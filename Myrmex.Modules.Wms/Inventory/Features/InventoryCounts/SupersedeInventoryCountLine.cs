using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Myrmex.Core.Application;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryCounts;
using Myrmex.Shared.Wms.Inventory;

namespace Myrmex.Modules.Wms.Inventory.Features.InventoryCounts;

internal static class SupersedeInventoryCountLine
{
    internal sealed record Command(
        Guid? InventoryCountId,
        Guid? LineId,
        string? ExpectedLineVersion,
        string? ActorId) : ICommand<ServiceResult<InventoryCountDetails>>;

    internal sealed class Handler(
        WmsDbContext dbContext,
        ILogger<Handler> logger)
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

            InventoryCountLine? line = count.Lines.SingleOrDefault(
                x => x.Id == command.LineId!.Value);
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

            InventoryBalance? balance = await dbContext.InventoryBalances
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.StockKeepingUnitId == line.StockKeepingUnitId &&
                         x.StorageLocationId == line.StorageLocationId,
                    cancellationToken);
            DomainValidationResult result = count.SupersedeLine(
                line.Id,
                balance?.Quantity ?? 0,
                balance?.RowVersion,
                out InventoryCountLine? replacement);
            if (!result.IsValid)
            {
                return ServiceResult<InventoryCountDetails>.Fail(
                    InventoryCountErrors.InvalidState(
                        "Only an unsuperseded Conflict line can be superseded.",
                        nameof(InventoryCountLine.Status)));
            }

            dbContext.InventoryCountLines.Add(replacement!);
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
            catch (DbUpdateException exception)
                when (exception.ToString().Contains(
                    WmsDatabaseNames.InventoryCountLineCurrentPairUniqueIndex,
                    StringComparison.OrdinalIgnoreCase) ||
                      exception.ToString().Contains(
                          WmsDatabaseNames.InventoryCountLineSupersedesUniqueIndex,
                          StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult<InventoryCountDetails>.Fail(
                    InventoryCountErrors.InvalidState(
                        "The conflict line was already superseded by another operation.",
                        nameof(InventoryCountLine.Status)));
            }

            logger.LogInformation(
                "Inventory count line {LineId} in count {InventoryCountId} superseded by replacement {ReplacementLineId} for actor {ActorId}.",
                line.Id,
                count.Id,
                replacement!.Id,
                command.ActorId);
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
            if (!command.InventoryCountId.HasValue || command.InventoryCountId == Guid.Empty)
            {
                errors.Add(DomainValidationFailure.Required<InventoryCount>(
                    nameof(Command.InventoryCountId)));
            }
            if (!command.LineId.HasValue || command.LineId == Guid.Empty)
            {
                errors.Add(DomainValidationFailure.Required<InventoryCountLine>(
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
                errors.Add(DomainValidationFailure.Required<InventoryCount>(
                    nameof(Command.ActorId)));
            }
            else if (command.ActorId.Trim().Length > InventoryCount.ActorIdMaxLength)
            {
                errors.Add(DomainValidationFailure.TooLong<InventoryCount>(
                    nameof(Command.ActorId),
                    InventoryCount.ActorIdMaxLength));
            }
            return errors;
        }
    }
}
