using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Myrmex.AppDispatching.EventDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Domain;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Events;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Domain;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransactions;
using Myrmex.Modules.Wms.Receiving.Domain.ReceivingOrders;
using Myrmex.Shared.Wms.Receiving;

namespace Myrmex.Modules.Wms.Receiving.Features.ReceivingOrders;

internal static class CompleteReceivingOrder
{
    internal sealed record Command(
        Guid? ReceivingOrderId,
        string? ExpectedOrderVersion,
        string? ActorId) : ICommand<ServiceResult<ReceivingOrderDetails>>;

    internal sealed class Handler(
        WmsDbContext dbContext,
        IDomainEventDispatcher domainEventDispatcher,
        ILogger<Handler> logger)
        : ICommandHandler<Command, ServiceResult<ReceivingOrderDetails>>
    {
        public async Task<ServiceResult<ReceivingOrderDetails>> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            DomainValidationFailure? idError = ValidateId(command);
            if (idError is not null)
            {
                return ServiceResult<ReceivingOrderDetails>.Invalid([idError]);
            }

            ReceivingOrder? order = await dbContext.ReceivingOrders
                .Include(x => x.Lines)
                .SingleOrDefaultAsync(x => x.Id == command.ReceivingOrderId!.Value, cancellationToken);
            if (order is null)
            {
                return ServiceResult<ReceivingOrderDetails>.Fail(
                    ServiceError.NotFound<ReceivingOrder>("ReceivingOrder not found", nameof(Command.ReceivingOrderId)));
            }

            if (order.Status == ReceivingOrderStatus.Completed)
            {
                return order.HasCompletePersistedCompletedInvariant
                    ? await CreateReceivingOrder.LoadDetailsAsync(dbContext, order.Id, cancellationToken)
                    : ServiceResult<ReceivingOrderDetails>.Fail(
                        ReceivingOrderErrors.InvalidPersistedState(
                            "Completed receiving order does not satisfy its persisted completion invariant."));
            }

            if (order.Status != ReceivingOrderStatus.InProgress)
            {
                return ServiceResult<ReceivingOrderDetails>.Fail(
                    ReceivingOrderErrors.InvalidState(
                        "Completion requires an InProgress order whose lines are fully received."));
            }

            DomainValidationFailure? versionError = ReceivingOrderVersion.Parse(
                command.ExpectedOrderVersion,
                nameof(Command.ExpectedOrderVersion),
                out byte[]? version);
            if (versionError is not null)
            {
                return ServiceResult<ReceivingOrderDetails>.Invalid([versionError]);
            }

            if (!order.RowVersion.SequenceEqual(version!))
            {
                return ServiceResult<ReceivingOrderDetails>.Fail(
                    ReceivingOrderErrors.ConcurrencyConflict(nameof(Command.ExpectedOrderVersion)));
            }

            if (!order.IsFullyReceived)
            {
                return ServiceResult<ReceivingOrderDetails>.Fail(
                    ReceivingOrderErrors.InvalidState(
                        "Completion requires an InProgress order whose lines are fully received."));
            }

            ReceivingOrderLine[] linesInDeterministicOrder = [.. order.Lines
                .OrderBy(line => line.CreatedAtUtc)
                .ThenBy(line => line.Id)];
            Guid[] skuIds = [.. linesInDeterministicOrder
                .Select(line => line.StockKeepingUnitId)];
            Dictionary<Guid, InventoryBalance> existingBalances = await dbContext.InventoryBalances
                .Where(x => x.StorageLocationId == order.ReceivingLocationId && skuIds.Contains(x.StockKeepingUnitId))
                .ToDictionaryAsync(x => x.StockKeepingUnitId, cancellationToken);

            Guid[] missingBalanceSkuIdsInLineOrder = [.. linesInDeterministicOrder
                .Where(line => !existingBalances.ContainsKey(line.StockKeepingUnitId))
                .Select(line => line.StockKeepingUnitId)];
            HashSet<Guid> missingBalanceSkuIds = [.. missingBalanceSkuIdsInLineOrder];

            // This set-based validation covers the complete line set before any balance is
            // changed. Its SKU/base-UOM checks and shared StorageLocationEligibility rule
            // are the reference eligibility semantics required for every missing balance.
            ServiceError? eligibilityError = await ReceivingOrderEligibility.ValidateAsync(
                dbContext,
                order.WarehouseId,
                order.ReceivingLocationId,
                skuIds,
                nameof(ReceivingOrder.WarehouseId),
                nameof(ReceivingOrder.ReceivingLocationId),
                index => $"Lines[{index}].StockKeepingUnitId",
                cancellationToken);
            if (eligibilityError is not null)
            {
                return ServiceResult<ReceivingOrderDetails>.Fail(eligibilityError);
            }

            List<InventoryTransaction.ReceivingChange> changes = [];
            foreach (ReceivingOrderLine line in linesInDeterministicOrder)
            {
                bool requiresBalanceCreation = missingBalanceSkuIds.Contains(line.StockKeepingUnitId);
                InventoryBalance? balance = requiresBalanceCreation
                    ? null
                    : existingBalances[line.StockKeepingUnitId];
                decimal before = balance?.Quantity ?? 0;
                decimal after;
                try
                {
                    after = before + line.ReceivedQuantity;
                }
                catch (OverflowException)
                {
                    return InvalidQuantity(nameof(InventoryBalance.Quantity));
                }

                if (!WmsQuantityPersistence.IsExactlyRepresentable(line.ReceivedQuantity) ||
                    !WmsQuantityPersistence.IsExactlyRepresentable(before) ||
                    !WmsQuantityPersistence.IsExactlyRepresentable(after))
                {
                    return InvalidQuantity(nameof(InventoryBalance.Quantity));
                }

                if (requiresBalanceCreation)
                {
                    DomainValidationResult createBalanceResult = InventoryBalance.Create(
                        line.StockKeepingUnitId,
                        order.ReceivingLocationId,
                        after,
                        out balance);
                    if (!createBalanceResult.IsValid || balance is null)
                    {
                        return ServiceResult<ReceivingOrderDetails>.Invalid(createBalanceResult.Errors);
                    }
                    dbContext.InventoryBalances.Add(balance);
                }
                else
                {
                    DomainValidationResult updateBalanceResult = balance.UpdateQuantity(after);
                    if (!updateBalanceResult.IsValid)
                    {
                        return ServiceResult<ReceivingOrderDetails>.Invalid(updateBalanceResult.Errors);
                    }
                }

                changes.Add(new(
                    line.StockKeepingUnitId,
                    line.ReceivedQuantity,
                    before,
                    after));
            }

            DateTimeOffset completedAtUtc = DateTimeOffset.UtcNow;
            string reason = $"ReceivingOrder {order.Id:D} Number {order.Number}";
            DomainValidationResult transactionResult = InventoryTransaction.CreateReceiving(
                order.ReceivingLocationId,
                changes,
                reason,
                completedAtUtc,
                out InventoryTransaction? transaction);
            if (!transactionResult.IsValid || transaction is null)
            {
                return ServiceResult<ReceivingOrderDetails>.Invalid(transactionResult.Errors);
            }

            DomainValidationResult completionResult = order.Complete(transaction.Id, completedAtUtc);
            if (!completionResult.IsValid)
            {
                return ServiceResult<ReceivingOrderDetails>.Invalid(completionResult.Errors);
            }

            dbContext.InventoryTransactions.Add(transaction);
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
            }
            catch (DbUpdateConcurrencyException)
            {
                return await ObserveFailedCompletionAsync(order.Id, cancellationToken);
            }
            catch (DbUpdateException exception)
                when (WmsPersistenceExceptionMapper.IsInventoryBalanceSkuLocationDuplicate(exception))
            {
                return await ObserveFailedCompletionAsync(order.Id, cancellationToken);
            }

            logger.LogInformation(
                "Receiving order action {Action} completed with outcome {Outcome}. Actor {ActorId}; order {ReceivingOrderId}; transaction {InventoryTransactionId}; lines {LineCount}.",
                "Complete", "Success", command.ActorId, order.Id, transaction.Id, order.Lines.Count);
            return await CreateReceivingOrder.LoadDetailsAsync(dbContext, order.Id, cancellationToken);
        }

        private async Task<ServiceResult<ReceivingOrderDetails>> ObserveFailedCompletionAsync(
            Guid orderId,
            CancellationToken cancellationToken)
        {
            dbContext.ChangeTracker.Clear();
            ReceivingOrder? current = await dbContext.ReceivingOrders
                .AsNoTracking()
                .Include(x => x.Lines)
                .SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken);
            if (current is null)
            {
                return ServiceResult<ReceivingOrderDetails>.Fail(
                    ReceivingOrderErrors.InventoryPostingConflict());
            }

            if (current.Status == ReceivingOrderStatus.Completed)
            {
                return current.HasCompletePersistedCompletedInvariant
                    ? await CreateReceivingOrder.LoadDetailsAsync(dbContext, orderId, cancellationToken)
                    : ServiceResult<ReceivingOrderDetails>.Fail(
                        ReceivingOrderErrors.InvalidPersistedState(
                            "Concurrent completion produced an invalid persisted Completed state."));
            }

            return ServiceResult<ReceivingOrderDetails>.Fail(
                ReceivingOrderErrors.InventoryPostingConflict());
        }

        private static ServiceResult<ReceivingOrderDetails> InvalidQuantity(string property) =>
            ServiceResult<ReceivingOrderDetails>.Invalid([
                WmsQuantityPersistence.Validate<InventoryBalance>(
                    WmsQuantityPersistence.MaximumValue + 1,
                    property)!]);
    }

    private static DomainValidationFailure? ValidateId(Command command)
    {
        if (!command.ReceivingOrderId.HasValue || command.ReceivingOrderId.Value == Guid.Empty)
        {
            return DomainValidationFailure.Required<ReceivingOrder>(
                nameof(Command.ReceivingOrderId));
        }

        return null;
    }
}
