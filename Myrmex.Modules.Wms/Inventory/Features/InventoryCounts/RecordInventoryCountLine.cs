using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Myrmex.Core.Application;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryCounts;
using Myrmex.Shared.Wms.Inventory;

namespace Myrmex.Modules.Wms.Inventory.Features.InventoryCounts;

internal static class RecordInventoryCountLine
{
    internal sealed record Command(
        Guid? InventoryCountId,
        Guid? LineId,
        decimal CountedQuantity,
        string? Comment,
        string? ExpectedLineVersion,
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
                .SingleOrDefaultAsync(
                    x => x.Id == command.InventoryCountId!.Value,
                    cancellationToken);

            if (count is null)
            {
                return ServiceResult<InventoryCountDetails>.Fail(
                    ServiceError.NotFound<InventoryCount>(
                        "InventoryCount not found",
                        nameof(Command.InventoryCountId)));
            }

            InventoryCountLine? line = count.Lines
                .SingleOrDefault(x => x.Id == command.LineId!.Value);

            if (line is null)
            {
                return ServiceResult<InventoryCountDetails>.Fail(
                    ServiceError.NotFound<InventoryCountLine>(
                        "InventoryCountLine not found",
                        nameof(Command.LineId)));
            }

            if (!line.RowVersion.SequenceEqual(expectedVersion!))
            {
                return ServiceResult<InventoryCountDetails>.Fail(
                    InventoryCountErrors.LineConcurrency(
                        nameof(Command.ExpectedLineVersion)));
            }

            DomainValidationResult recordResult = count.RecordLineCount(
                line.Id,
                command.CountedQuantity,
                command.Comment,
                command.ActorId,
                DateTimeOffset.UtcNow);

            if (!recordResult.IsValid)
            {
                if (recordResult.Errors.Any(x =>
                        x.Code.StartsWith("IncorrectState-", StringComparison.Ordinal)))
                {
                    return ServiceResult<InventoryCountDetails>.Fail(
                        InventoryCountErrors.InvalidState(
                            "Only Pending or Counted lines in an active inventory count can be recorded.",
                            nameof(InventoryCountLine.Status)));
                }

                return ServiceResult<InventoryCountDetails>.Invalid(recordResult.Errors);
            }

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return ServiceResult<InventoryCountDetails>.Fail(
                    InventoryCountErrors.LineConcurrency(
                        nameof(Command.ExpectedLineVersion)));
            }

            logger?.LogInformation(
                "Inventory count action {Action} completed with outcome {Outcome}. Actor {ActorId}; count {InventoryCountId}; line {LineId}; warehouse {WarehouseId}; SKU {StockKeepingUnitId}; location {StorageLocationId}.",
                "RecordCount",
                "Success",
                command.ActorId,
                count.Id,
                line.Id,
                count.WarehouseId,
                line.StockKeepingUnitId,
                line.StorageLocationId);

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
                errors.Add(
                    DomainValidationFailure.Required<InventoryCount>(
                        nameof(Command.InventoryCountId)));
            }

            if (!command.LineId.HasValue || command.LineId.Value == Guid.Empty)
            {
                errors.Add(
                    DomainValidationFailure.Required<InventoryCountLine>(
                        nameof(Command.LineId)));
            }

            DomainValidationFailure? versionError = InventoryCountVersion.Parse(
                command.ExpectedLineVersion,
                nameof(Command.ExpectedLineVersion),
                out expectedVersion);

            if (versionError is not null)
            {
                errors.Add(versionError);
            }

            if (string.IsNullOrWhiteSpace(command.ActorId))
            {
                errors.Add(
                    DomainValidationFailure.Required<InventoryCount>(
                        nameof(Command.ActorId)));
            }

            return errors;
        }
    }
}
