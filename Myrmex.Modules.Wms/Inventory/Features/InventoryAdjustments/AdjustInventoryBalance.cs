using Microsoft.EntityFrameworkCore;
using Myrmex.AppDispatching.EventDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Domain;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Events;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransactions;
using Myrmex.Modules.Wms.Inventory.Features.InventoryBalances;
using Myrmex.Shared.Wms.Inventory;

namespace Myrmex.Modules.Wms.Inventory.Features.InventoryAdjustments;

internal static class AdjustInventoryBalance
{
    internal sealed record Command(
        Guid? StockKeepingUnitId,
        Guid? StorageLocationId,
        decimal CountedQuantity,
        string? Reason,
        string? ExpectedBalanceVersion) : ICommand<ServiceResult<InventoryBalanceDetails>>;

    internal sealed class Handler(
        WmsDbContext dbContext,
        IDomainEventDispatcher domainEventDispatcher)
        : ICommandHandler<Command, ServiceResult<InventoryBalanceDetails>>
    {
        public async Task<ServiceResult<InventoryBalanceDetails>> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            List<DomainValidationFailure> validationErrors = ValidateCommand(
                command,
                out byte[]? expectedVersion);

            if (validationErrors.Count > 0)
            {
                return ServiceResult<InventoryBalanceDetails>.Invalid(validationErrors);
            }

            InventoryBalance? inventoryBalance = await dbContext.InventoryBalances
                .FirstOrDefaultAsync(
                    x => x.StockKeepingUnitId == command.StockKeepingUnitId!.Value &&
                         x.StorageLocationId == command.StorageLocationId!.Value,
                    cancellationToken);

            if (expectedVersion is null)
            {
                if (inventoryBalance is not null)
                {
                    return ConcurrencyConflict();
                }

                return await InitializeMissingBalanceAsync(command, cancellationToken);
            }

            if (inventoryBalance is null)
            {
                return ConcurrencyConflict();
            }

            if (!inventoryBalance.RowVersion.SequenceEqual(expectedVersion))
            {
                return ConcurrencyConflict();
            }

            if (inventoryBalance.Quantity == command.CountedQuantity)
            {
                return await GetDetailsAsync(inventoryBalance.Id, cancellationToken);
            }

            decimal balanceBefore = inventoryBalance.Quantity;

            DomainValidationResult balanceValidationResult =
                inventoryBalance.ApplyCountedQuantityAdjustment(command.CountedQuantity);

            if (!balanceValidationResult.IsValid)
            {
                return ServiceResult<InventoryBalanceDetails>.Invalid(balanceValidationResult.Errors);
            }

            DomainValidationResult transactionValidationResult =
                InventoryTransaction.CreateAdjustment(
                    inventoryBalance.StockKeepingUnitId,
                    inventoryBalance.StorageLocationId,
                    balanceBefore,
                    inventoryBalance.Quantity,
                    command.Reason,
                    DateTimeOffset.UtcNow,
                    out InventoryTransaction? inventoryTransaction);

            if (!transactionValidationResult.IsValid)
            {
                return ServiceResult<InventoryBalanceDetails>.Invalid(transactionValidationResult.Errors);
            }

            dbContext.InventoryTransactions.Add(inventoryTransaction!);

            ServiceResult saveResult = await SaveAdjustmentChangesAsync(cancellationToken);

            if (!saveResult.IsSuccess)
            {
                return ServiceResult<InventoryBalanceDetails>.Fail(saveResult.Error);
            }

            return await GetDetailsAsync(inventoryBalance.Id, cancellationToken);
        }

        private async Task<ServiceResult<InventoryBalanceDetails>> InitializeMissingBalanceAsync(
            Command command,
            CancellationToken cancellationToken)
        {
            DomainValidationResult balanceValidationResult = InventoryBalance.Create(
                command.StockKeepingUnitId,
                command.StorageLocationId,
                command.CountedQuantity,
                out InventoryBalance? inventoryBalance);

            if (!balanceValidationResult.IsValid)
            {
                return ServiceResult<InventoryBalanceDetails>.Invalid(balanceValidationResult.Errors);
            }

            ServiceError? eligibilityError = await InventoryBalanceCreateEligibility.ValidateAsync(
                dbContext,
                inventoryBalance!,
                cancellationToken);

            if (eligibilityError is not null)
            {
                return ServiceResult<InventoryBalanceDetails>.Fail(eligibilityError);
            }

            dbContext.InventoryBalances.Add(inventoryBalance!);

            if (command.CountedQuantity > 0)
            {
                DomainValidationResult transactionValidationResult =
                    InventoryTransaction.CreateAdjustment(
                        inventoryBalance.StockKeepingUnitId,
                        inventoryBalance.StorageLocationId,
                        balanceBefore: 0,
                        balanceAfter: inventoryBalance.Quantity,
                        command.Reason,
                        DateTimeOffset.UtcNow,
                        out InventoryTransaction? inventoryTransaction);

                if (!transactionValidationResult.IsValid)
                {
                    return ServiceResult<InventoryBalanceDetails>.Invalid(transactionValidationResult.Errors);
                }

                dbContext.InventoryTransactions.Add(inventoryTransaction!);
            }

            ServiceResult saveResult = await SaveAdjustmentChangesAsync(cancellationToken);

            if (!saveResult.IsSuccess)
            {
                return ServiceResult<InventoryBalanceDetails>.Fail(saveResult.Error);
            }

            return await GetDetailsAsync(inventoryBalance.Id, cancellationToken);
        }

        private async Task<ServiceResult> SaveAdjustmentChangesAsync(CancellationToken cancellationToken)
        {
            List<AggregateRoot> aggregateRoots = dbContext.ChangeTracker
                .Entries<AggregateRoot>()
                .Select(x => x.Entity)
                .Where(x => x.DomainEvents.Count > 0)
                .ToList();

            List<IDomainEvent> domainEvents = aggregateRoots
                .SelectMany(x => x.DomainEvents)
                .ToList();

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);

                await domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);

                foreach (AggregateRoot aggregateRoot in aggregateRoots)
                {
                    aggregateRoot.ClearDomainEvents();
                }

                return ServiceResult.Success();
            }
            catch (DbUpdateConcurrencyException)
            {
                return ServiceResult.Fail(ConcurrencyConflictError());
            }
            catch (DbUpdateException exception)
                when (WmsPersistenceExceptionMapper.IsInventoryBalanceSkuLocationDuplicate(exception))
            {
                return ServiceResult.Fail(ConcurrencyConflictError());
            }
            catch (DbUpdateException exception)
            {
                ServiceError? error = WmsPersistenceExceptionMapper.TryMap(exception);

                if (error is not null)
                {
                    return ServiceResult.Fail(error);
                }

                throw;
            }
        }

        private static List<DomainValidationFailure> ValidateCommand(
            Command command,
            out byte[]? expectedVersion)
        {
            List<DomainValidationFailure> errors = [];
            expectedVersion = null;

            if (!command.StockKeepingUnitId.HasValue || command.StockKeepingUnitId.Value == Guid.Empty)
            {
                errors.Add(DomainValidationFailure.Required<InventoryBalance>(nameof(Command.StockKeepingUnitId)));
            }

            if (!command.StorageLocationId.HasValue || command.StorageLocationId.Value == Guid.Empty)
            {
                errors.Add(DomainValidationFailure.Required<InventoryBalance>(nameof(Command.StorageLocationId)));
            }

            if (command.CountedQuantity < 0)
            {
                errors.Add(DomainValidationFailure.MustBeNonNegative<InventoryBalance>(nameof(Command.CountedQuantity)));
            }

            string reason = command.Reason?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(reason))
            {
                errors.Add(DomainValidationFailure.Required<InventoryTransaction>(nameof(Command.Reason)));
            }
            else if (reason.Length > InventoryTransaction.ReasonMaxLength)
            {
                errors.Add(DomainValidationFailure.TooLong<InventoryTransaction>(nameof(Command.Reason), InventoryTransaction.ReasonMaxLength));
            }

            if (command.ExpectedBalanceVersion is not null)
            {
                try
                {
                    byte[] parsedVersion = Convert.FromBase64String(command.ExpectedBalanceVersion);

                    if (parsedVersion.Length != 8)
                    {
                        errors.Add(DomainValidationFailure.IncorrectState<InventoryBalance>(nameof(Command.ExpectedBalanceVersion)));
                    }
                    else
                    {
                        expectedVersion = parsedVersion;
                    }
                }
                catch (FormatException)
                {
                    errors.Add(DomainValidationFailure.IncorrectState<InventoryBalance>(nameof(Command.ExpectedBalanceVersion)));
                }
            }

            return errors;
        }

        private async Task<ServiceResult<InventoryBalanceDetails>> GetDetailsAsync(
            Guid inventoryBalanceId,
            CancellationToken cancellationToken)
        {
            InventoryBalanceDetailsData? detailsData = await dbContext.InventoryBalances
                .AsNoTracking()
                .Where(x => x.Id == inventoryBalanceId)
                .ProjectDetailsData()
                .SingleOrDefaultAsync(cancellationToken);

            return detailsData is null
                ? ServiceResult<InventoryBalanceDetails>.Fail(ServiceError.Failure<InventoryBalance>("Failed to adjust InventoryBalance"))
                : ServiceResult<InventoryBalanceDetails>.Success(detailsData.ToDetails());
        }
    }

    internal static ServiceResult<InventoryBalanceDetails> ConcurrencyConflict()
    {
        return ServiceResult<InventoryBalanceDetails>.Fail(ConcurrencyConflictError());
    }

    internal static ServiceError ConcurrencyConflictError()
    {
        return new ServiceError(
            ServiceErrorType.Conflict,
            "InventoryBalance.ConcurrencyConflict",
            "Inventory balance was changed by another operation. Refresh and review the current balance before adjusting again.",
            nameof(Command.ExpectedBalanceVersion));
    }
}
