using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Myrmex.AppDispatching.EventDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Domain;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Events;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryCounts;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransactions;
using Myrmex.Shared.Wms.Inventory;

namespace Myrmex.Modules.Wms.Inventory.Features.InventoryCounts;

internal static class ApplyInventoryCountLine
{
    internal sealed record Command(
        Guid? InventoryCountId,
        Guid? LineId,
        string? ExpectedLineVersion,
        string? ActorId) : ICommand<ServiceResult<InventoryCountDetails>>;

    internal sealed class Handler(
        WmsDbContext dbContext,
        IDomainEventDispatcher domainEventDispatcher,
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

            if (line.Status != InventoryCountLineStatus.Counted ||
                line.CountedQuantity is null ||
                line.VarianceQuantity is null)
            {
                return ServiceResult<InventoryCountDetails>.Fail(
                    InventoryCountErrors.InvalidState(
                        "Only Counted inventory count lines can be applied.",
                        nameof(InventoryCountLine.Status)));
            }

            InventoryBalance? balance = await dbContext.InventoryBalances
                .SingleOrDefaultAsync(
                    x => x.StockKeepingUnitId == line.StockKeepingUnitId &&
                         x.StorageLocationId == line.StorageLocationId,
                    cancellationToken);

            if (!SnapshotMatches(line, balance))
            {
                return await PersistConflictAsync(count, line, command, cancellationToken);
            }

            DateTimeOffset appliedAtUtc = DateTimeOffset.UtcNow;
            if (line.VarianceQuantity == 0)
            {
                DomainValidationResult zeroResult = count.ApplyLine(
                    line.Id,
                    inventoryTransactionId: null,
                    actorId: command.ActorId,
                    appliedAtUtc: appliedAtUtc);
                if (!zeroResult.IsValid)
                {
                    return ServiceResult<InventoryCountDetails>.Invalid(zeroResult.Errors);
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

                logger.LogInformation(
                    "Inventory count line {LineId} in count {InventoryCountId} applied with zero variance by actor {ActorId}.",
                    line.Id,
                    count.Id,
                    command.ActorId);
                return await CreateInventoryCount.LoadDetailsAsync(
                    dbContext,
                    count.Id,
                    cancellationToken);
            }

            decimal balanceBefore = line.SystemQuantity;
            decimal balanceAfter = line.CountedQuantity.Value;

            if (balance is null)
            {
                DomainValidationResult createBalanceResult = InventoryBalance.Create(
                    line.StockKeepingUnitId,
                    line.StorageLocationId,
                    balanceAfter,
                    out balance);
                if (!createBalanceResult.IsValid)
                {
                    return ServiceResult<InventoryCountDetails>.Invalid(
                        createBalanceResult.Errors);
                }
                dbContext.InventoryBalances.Add(balance!);
            }
            else
            {
                DomainValidationResult updateResult =
                    balance.ApplyCountedQuantityAdjustment(balanceAfter);
                if (!updateResult.IsValid)
                {
                    return ServiceResult<InventoryCountDetails>.Invalid(updateResult.Errors);
                }
            }

            string reason = BuildReason(count, line);
            DomainValidationResult transactionResult =
                InventoryTransaction.CreateAdjustment(
                    line.StockKeepingUnitId,
                    line.StorageLocationId,
                    balanceBefore,
                    balanceAfter,
                    reason,
                    appliedAtUtc,
                    out InventoryTransaction? transaction);
            if (!transactionResult.IsValid)
            {
                return ServiceResult<InventoryCountDetails>.Invalid(transactionResult.Errors);
            }

            InventoryTransaction createdTransaction = transaction
                ?? throw new InvalidOperationException(
                    "InventoryTransaction.CreateAdjustment returned no transaction.");
            DomainValidationResult applyResult = count.ApplyLine(
                line.Id,
                createdTransaction.Id,
                command.ActorId,
                appliedAtUtc);
            if (!applyResult.IsValid)
            {
                return ServiceResult<InventoryCountDetails>.Invalid(applyResult.Errors);
            }

            dbContext.InventoryTransactions.Add(createdTransaction);
            ServiceResult saveResult = await SaveSuccessfulApplyAsync(cancellationToken);
            if (!saveResult.IsSuccess)
            {
                return ServiceResult<InventoryCountDetails>.Fail(saveResult.Error);
            }

            logger.LogInformation(
                "Inventory count line {LineId} in count {InventoryCountId} applied by actor {ActorId}; variance {Variance}; transaction {InventoryTransactionId}.",
                line.Id,
                count.Id,
                command.ActorId,
                line.VarianceQuantity,
                createdTransaction.Id);
            return await CreateInventoryCount.LoadDetailsAsync(
                dbContext,
                count.Id,
                cancellationToken);
        }

        private async Task<ServiceResult<InventoryCountDetails>> PersistConflictAsync(
            InventoryCount count,
            InventoryCountLine line,
            Command command,
            CancellationToken cancellationToken)
        {
            DomainValidationResult conflictResult = count.MarkLineConflict(line.Id);
            if (!conflictResult.IsValid)
            {
                return ServiceResult<InventoryCountDetails>.Invalid(conflictResult.Errors);
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

            logger.LogWarning(
                "Inventory count line {LineId} in count {InventoryCountId} marked Conflict by actor {ActorId} because the inventory snapshot changed.",
                line.Id,
                count.Id,
                command.ActorId);
            return ServiceResult<InventoryCountDetails>.Fail(
                InventoryCountErrors.BalanceSnapshotConflict());
        }

        private async Task<ServiceResult> SaveSuccessfulApplyAsync(
            CancellationToken cancellationToken)
        {
            List<AggregateRoot> roots = dbContext.ChangeTracker
                .Entries<AggregateRoot>()
                .Select(x => x.Entity)
                .Where(x => x.DomainEvents.Count > 0)
                .ToList();
            List<IDomainEvent> events = roots.SelectMany(x => x.DomainEvents).ToList();

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                await domainEventDispatcher.DispatchAsync(events, cancellationToken);
                foreach (AggregateRoot root in roots)
                {
                    root.ClearDomainEvents();
                }
                return ServiceResult.Success();
            }
            catch (DbUpdateConcurrencyException)
            {
                return ServiceResult.Fail(
                    InventoryCountErrors.BalanceSnapshotConflict());
            }
            catch (DbUpdateException exception)
                when (WmsPersistenceExceptionMapper.IsInventoryBalanceSkuLocationDuplicate(exception))
            {
                return ServiceResult.Fail(
                    InventoryCountErrors.BalanceSnapshotConflict());
            }
        }

        private static bool SnapshotMatches(
            InventoryCountLine line,
            InventoryBalance? balance) =>
            line.ExpectedBalanceVersion is null
                ? balance is null
                : balance is not null &&
                  balance.RowVersion.SequenceEqual(line.ExpectedBalanceVersion);

        private static string BuildReason(
            InventoryCount count,
            InventoryCountLine line)
        {
            string reason = $"Inventory count {count.Id}";
            if (!string.IsNullOrWhiteSpace(count.Reason))
            {
                reason += $": {count.Reason}";
            }
            if (!string.IsNullOrWhiteSpace(line.Comment))
            {
                reason += $" | {line.Comment}";
            }
            return reason.Length <= InventoryTransaction.ReasonMaxLength
                ? reason
                : reason[..InventoryTransaction.ReasonMaxLength];
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
