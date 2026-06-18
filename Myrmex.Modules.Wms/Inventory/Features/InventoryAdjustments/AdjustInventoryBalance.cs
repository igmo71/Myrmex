using Microsoft.EntityFrameworkCore;
using Myrmex.AppDispatching.EventDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Domain.Validation;
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
            List<DomainValidationFailure> validationErrors = ValidateCommand(command);

            if (validationErrors.Count > 0)
            {
                return ServiceResult<InventoryBalanceDetails>.Invalid(validationErrors);
            }

            byte[] expectedVersion = Convert.FromBase64String(command.ExpectedBalanceVersion!);

            InventoryBalance? inventoryBalance = await dbContext.InventoryBalances
                .FirstOrDefaultAsync(
                    x => x.StockKeepingUnitId == command.StockKeepingUnitId!.Value &&
                         x.StorageLocationId == command.StorageLocationId!.Value,
                    cancellationToken);

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

            ServiceResult saveResult;

            try
            {
                saveResult = await dbContext
                    .SaveChangesAsServiceResultAsync(domainEventDispatcher, cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return ConcurrencyConflict();
            }

            if (!saveResult.IsSuccess)
            {
                return ServiceResult<InventoryBalanceDetails>.Fail(saveResult.Error);
            }

            return await GetDetailsAsync(inventoryBalance.Id, cancellationToken);
        }

        private static List<DomainValidationFailure> ValidateCommand(Command command)
        {
            List<DomainValidationFailure> errors = [];

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

            if (command.ExpectedBalanceVersion is null)
            {
                errors.Add(DomainValidationFailure.Required<InventoryBalance>(nameof(Command.ExpectedBalanceVersion)));
            }
            else
            {
                try
                {
                    Convert.FromBase64String(command.ExpectedBalanceVersion);
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
        return ServiceResult<InventoryBalanceDetails>.Fail(new ServiceError(
            ServiceErrorType.Conflict,
            "InventoryBalance.ConcurrencyConflict",
            "Inventory balance was changed by another operation. Refresh and review the current balance before adjusting again.",
            nameof(Command.ExpectedBalanceVersion)));
    }
}
