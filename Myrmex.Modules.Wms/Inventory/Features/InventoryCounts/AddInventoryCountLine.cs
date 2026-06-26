using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Myrmex.Core.Application;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryCounts;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;
using Myrmex.Shared.Wms.Inventory;

namespace Myrmex.Modules.Wms.Inventory.Features.InventoryCounts;

internal static class AddInventoryCountLine
{
    private const string InternalTransitCode = "INTERNAL_TRANSIT";
    private const string ExternalTransitCode = "EXTERNAL_TRANSIT";

    internal sealed record Command(
        Guid? InventoryCountId,
        Guid? StockKeepingUnitId,
        Guid? StorageLocationId,
        string? ExpectedCountVersion,
        string? ActorId) : ICommand<ServiceResult<InventoryCountDetails>>;

    internal sealed class Handler(
        WmsDbContext dbContext,
        ILogger<Handler>? logger = null)
        : ICommandHandler<Command, ServiceResult<InventoryCountDetails>>
    {
        public async Task<ServiceResult<InventoryCountDetails>> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            List<DomainValidationFailure> errors = Validate(command, out byte[]? expectedVersion);

            if (errors.Count > 0)
            {
                return ServiceResult<InventoryCountDetails>.Invalid(errors);
            }

            InventoryCount? count = await dbContext.InventoryCounts
                .Include(x => x.Lines)
                .SingleOrDefaultAsync(x => x.Id == command.InventoryCountId!.Value, cancellationToken);

            if (count is null)
            {
                return ServiceResult<InventoryCountDetails>.Fail(
                    ServiceError.NotFound<InventoryCount>(
                        "InventoryCount not found",
                        nameof(Command.InventoryCountId)));
            }

            if (!count.RowVersion.SequenceEqual(expectedVersion!))
            {
                return ServiceResult<InventoryCountDetails>.Fail(
                    InventoryCountErrors.CountConcurrency(nameof(Command.ExpectedCountVersion)));
            }

            StockKeepingUnit? sku = await dbContext.StockKeepingUnits
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == command.StockKeepingUnitId!.Value, cancellationToken);

            if (sku is null)
            {
                return ServiceResult<InventoryCountDetails>.Fail(
                    ServiceError.NotFound<StockKeepingUnit>(
                        "StockKeepingUnit not found",
                        nameof(Command.StockKeepingUnitId)));
            }

            if (!sku.IsActive)
            {
                return ServiceResult<InventoryCountDetails>.Fail(
                    ServiceError.Validation<StockKeepingUnit>(
                        "StockKeepingUnit is inactive",
                        nameof(Command.StockKeepingUnitId)));
            }

            StorageLocation? location = await dbContext.StorageLocations
                .AsNoTracking()
                .Include(x => x.StorageLocationType)
                .Include(x => x.StorageLocationStatus)
                .SingleOrDefaultAsync(x => x.Id == command.StorageLocationId!.Value, cancellationToken);

            ServiceError? locationError = ValidateLocation(count, location);

            if (locationError is not null)
            {
                return ServiceResult<InventoryCountDetails>.Fail(locationError);
            }

            InventoryBalance? balance = await dbContext.InventoryBalances
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.StockKeepingUnitId == command.StockKeepingUnitId!.Value &&
                         x.StorageLocationId == command.StorageLocationId!.Value,
                    cancellationToken);

            DomainValidationResult addResult = count.AddLine(
                command.StockKeepingUnitId,
                command.StorageLocationId,
                balance?.Quantity ?? 0,
                balance?.RowVersion,
                out InventoryCountLine? line);

            if (!addResult.IsValid)
            {
                if (count.Lines.Any(x =>
                        x.IsCurrent &&
                        x.StockKeepingUnitId == command.StockKeepingUnitId &&
                        x.StorageLocationId == command.StorageLocationId))
                {
                    return ServiceResult<InventoryCountDetails>.Fail(InventoryCountErrors.DuplicateLine());
                }

                return ServiceResult<InventoryCountDetails>.Invalid(addResult.Errors);
            }

            InventoryCountLine? addedLine = line
                ?? throw new InvalidOperationException("InventoryCount.AddLine returned a valid result without a line.");

            dbContext.InventoryCountLines.Add(addedLine!);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return ServiceResult<InventoryCountDetails>.Fail(
                    InventoryCountErrors.CountConcurrency(nameof(Command.ExpectedCountVersion)));
            }
            catch (DbUpdateException exception)
                when (exception.ToString().Contains(
                    WmsDatabaseNames.InventoryCountLineCurrentPairUniqueIndex,
                    StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult<InventoryCountDetails>.Fail(InventoryCountErrors.DuplicateLine());
            }

            logger?.LogInformation(
                "Inventory count action {Action} completed with outcome {Outcome}. Actor {ActorId}; count {InventoryCountId}; line {LineId}; warehouse {WarehouseId}; SKU {StockKeepingUnitId}; location {StorageLocationId}.",
                "AddLine",
                "Success",
                command.ActorId,
                count.Id,
                addedLine.Id,
                count.WarehouseId,
                addedLine.StockKeepingUnitId,
                addedLine.StorageLocationId);

            return await CreateInventoryCount.LoadDetailsAsync(
                dbContext,
                count.Id,
                cancellationToken);
        }

        private static List<DomainValidationFailure> Validate(
            Command command,
            out byte[]? expectedVersion)
        {
            List<DomainValidationFailure> errors = [];

            if (!command.InventoryCountId.HasValue || command.InventoryCountId.Value == Guid.Empty)
            {
                errors.Add(DomainValidationFailure.Required<InventoryCount>(nameof(Command.InventoryCountId)));
            }

            if (!command.StockKeepingUnitId.HasValue || command.StockKeepingUnitId.Value == Guid.Empty)
            {
                errors.Add(DomainValidationFailure.Required<InventoryCountLine>(nameof(Command.StockKeepingUnitId)));
            }

            if (!command.StorageLocationId.HasValue || command.StorageLocationId.Value == Guid.Empty)
            {
                errors.Add(DomainValidationFailure.Required<InventoryCountLine>(nameof(Command.StorageLocationId)));
            }

            DomainValidationFailure? versionError = InventoryCountVersion.Parse(
                command.ExpectedCountVersion,
                nameof(Command.ExpectedCountVersion),
                out expectedVersion);

            if (versionError is not null)
            {
                errors.Add(versionError);
            }

            if (string.IsNullOrWhiteSpace(command.ActorId))
            {
                errors.Add(DomainValidationFailure.Required<InventoryCount>(nameof(Command.ActorId)));
            }

            return errors;
        }

        private static ServiceError? ValidateLocation(
            InventoryCount count,
            StorageLocation? location)
        {
            if (location is null)
            {
                return ServiceError.NotFound<StorageLocation>(
                    "StorageLocation not found",
                    nameof(Command.StorageLocationId));
            }

            if (!location.IsActive)
            {
                return ServiceError.Validation<StorageLocation>(
                    "StorageLocation is inactive",
                    nameof(Command.StorageLocationId));
            }

            if (!location.StorageLocationType.IsActive)
            {
                return ServiceError.Validation<StorageLocationType>(
                    "StorageLocationType is inactive",
                    nameof(Command.StorageLocationId));
            }

            if (!location.StorageLocationStatus.IsActive)
            {
                return ServiceError.Validation<StorageLocationStatus>(
                    "StorageLocationStatus is inactive",
                    nameof(Command.StorageLocationId));
            }

            if (location.WarehouseId != count.WarehouseId)
            {
                return ServiceError.Validation<StorageLocation>(
                    "StorageLocation does not belong to the count warehouse",
                    nameof(Command.StorageLocationId));
            }

            if (location.StorageLocationType.Code is InternalTransitCode or ExternalTransitCode)
            {
                return ServiceError.Validation<StorageLocation>(
                    "Transit storage locations are not eligible for inventory counting",
                    nameof(Command.StorageLocationId));
            }

            return null;
        }
    }
}
