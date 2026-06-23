using Microsoft.EntityFrameworkCore;
using Myrmex.AppDispatching.EventDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransactions;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransfers;
using Myrmex.Shared.Wms.Inventory;

namespace Myrmex.Modules.Wms.Inventory.Features.InventoryTransfers;

internal static class PlaceInventoryTransferLine
{
    internal sealed record Command(
        Guid? TransferId,
        Guid? LineId,
        decimal Quantity) : ICommand<ServiceResult<InventoryTransferDetails>>;

    internal sealed class Handler(
        WmsDbContext dbContext,
        IDomainEventDispatcher domainEventDispatcher)
        : ICommandHandler<Command, ServiceResult<InventoryTransferDetails>>
    {
        public async Task<ServiceResult<InventoryTransferDetails>> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            List<DomainValidationFailure> commandErrors = ValidateCommand(command);

            if (commandErrors.Count > 0)
            {
                return ServiceResult<InventoryTransferDetails>.Invalid(commandErrors);
            }

            InventoryTransfer? transfer = await dbContext.InventoryTransfers
                .Include(x => x.Lines)
                .Include(x => x.Movements)
                .FirstOrDefaultAsync(x => x.Id == command.TransferId!.Value, cancellationToken);

            if (transfer is null)
            {
                return ServiceResult<InventoryTransferDetails>.Fail(
                    ServiceError.NotFound<InventoryTransfer>("InventoryTransfer not found", nameof(Command.TransferId)));
            }

            InventoryTransferLine? line = transfer.Lines
                .SingleOrDefault(x => x.Id == command.LineId!.Value);

            if (line is null)
            {
                return ServiceResult<InventoryTransferDetails>.Fail(
                    ServiceError.NotFound<InventoryTransferLine>("InventoryTransferLine not found", nameof(Command.LineId)));
            }

            ServiceError? movementError = ValidateMovement(transfer, line, command.Quantity);

            if (movementError is not null)
            {
                return ServiceResult<InventoryTransferDetails>.Fail(movementError);
            }

            Guid transitStorageLocationId = transfer.TransitStorageLocationId!.Value;

            InventoryBalance? transitBalance = await dbContext.InventoryBalances
                .FirstOrDefaultAsync(
                    x => x.StockKeepingUnitId == line.StockKeepingUnitId &&
                         x.StorageLocationId == transitStorageLocationId,
                    cancellationToken);

            if (transitBalance is null || transitBalance.Quantity < command.Quantity)
            {
                return ServiceResult<InventoryTransferDetails>.Fail(InsufficientTransitBalanceConflict());
            }

            InventoryBalance? destinationBalance = await dbContext.InventoryBalances
                .FirstOrDefaultAsync(
                    x => x.StockKeepingUnitId == line.StockKeepingUnitId &&
                         x.StorageLocationId == line.DestinationStorageLocationId,
                    cancellationToken);

            decimal transitBalanceBefore = transitBalance.Quantity;
            decimal transitBalanceAfter = transitBalanceBefore - command.Quantity;
            decimal destinationBalanceBefore = destinationBalance?.Quantity ?? 0;
            decimal destinationBalanceAfter = destinationBalanceBefore + command.Quantity;

            DomainValidationResult transitBalanceValidation = transitBalance.UpdateQuantity(transitBalanceAfter);

            if (!transitBalanceValidation.IsValid)
            {
                return ServiceResult<InventoryTransferDetails>.Invalid(transitBalanceValidation.Errors);
            }

            if (destinationBalance is null)
            {
                DomainValidationResult destinationBalanceCreateValidation = InventoryBalance.Create(
                    line.StockKeepingUnitId,
                    line.DestinationStorageLocationId,
                    destinationBalanceAfter,
                    out destinationBalance);

                if (!destinationBalanceCreateValidation.IsValid)
                {
                    return ServiceResult<InventoryTransferDetails>.Invalid(destinationBalanceCreateValidation.Errors);
                }

                dbContext.InventoryBalances.Add(destinationBalance!);
            }
            else
            {
                DomainValidationResult destinationBalanceValidation = destinationBalance.UpdateQuantity(destinationBalanceAfter);

                if (!destinationBalanceValidation.IsValid)
                {
                    return ServiceResult<InventoryTransferDetails>.Invalid(destinationBalanceValidation.Errors);
                }
            }

            DateTimeOffset occurredAtUtc = DateTimeOffset.UtcNow;

            DomainValidationResult transactionValidation = InventoryTransaction.CreateTransfer(
                line.StockKeepingUnitId,
                transitStorageLocationId,
                line.DestinationStorageLocationId,
                transitBalanceBefore,
                transitBalanceAfter,
                destinationBalanceBefore,
                destinationBalanceAfter,
                $"Internal transfer {transfer.Code}",
                occurredAtUtc,
                out InventoryTransaction? inventoryTransaction);

            if (!transactionValidation.IsValid)
            {
                return ServiceResult<InventoryTransferDetails>.Invalid(transactionValidation.Errors);
            }

            InventoryTransaction createdTransaction = inventoryTransaction
                ?? throw new InvalidOperationException("InventoryTransaction.CreateTransfer returned a valid result without a transaction.");

            DomainValidationResult movementValidation = InventoryTransferMovement.Create(
                line.Id,
                createdTransaction.Id,
                transitStorageLocationId,
                line.DestinationStorageLocationId,
                command.Quantity,
                occurredAtUtc,
                out InventoryTransferMovement? movement);

            if (!movementValidation.IsValid)
            {
                return ServiceResult<InventoryTransferDetails>.Invalid(movementValidation.Errors);
            }

            DomainValidationResult addMovementValidation = transfer.AddMovement(movement!);

            if (!addMovementValidation.IsValid)
            {
                return ServiceResult<InventoryTransferDetails>.Fail(CompletedTransferConflict());
            }

            dbContext.InventoryTransferMovements.Add(movement!);
            dbContext.InventoryTransactions.Add(createdTransaction);

            ServiceResult saveResult;

            try
            {
                saveResult = await dbContext.SaveChangesAsServiceResultAsync(
                    domainEventDispatcher,
                    cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                saveResult = ServiceResult.Fail(MovementConcurrencyConflict());
            }

            if (!saveResult.IsSuccess)
            {
                return ServiceResult<InventoryTransferDetails>.Fail(saveResult.Error);
            }

            return await GetDetailsAsync(transfer.Id, cancellationToken);
        }

        private static List<DomainValidationFailure> ValidateCommand(Command command)
        {
            List<DomainValidationFailure> errors = [];

            if (!command.TransferId.HasValue || command.TransferId.Value == Guid.Empty)
            {
                errors.Add(DomainValidationFailure.Required<InventoryTransfer>(nameof(Command.TransferId)));
            }

            if (!command.LineId.HasValue || command.LineId.Value == Guid.Empty)
            {
                errors.Add(DomainValidationFailure.Required<InventoryTransferLine>(nameof(Command.LineId)));
            }

            if (command.Quantity <= 0)
            {
                errors.Add(DomainValidationFailure.IncorrectState<InventoryTransferMovement>(nameof(Command.Quantity)));
            }

            return errors;
        }

        private static ServiceError? ValidateMovement(
            InventoryTransfer transfer,
            InventoryTransferLine line,
            decimal quantity)
        {
            if (transfer.Status == InventoryTransferStatus.Completed)
            {
                return CompletedTransferConflict();
            }

            if (!transfer.UsesTransit)
            {
                return PlaceRequiresTransitConflict();
            }

            decimal inTransitQuantity = line.GetInTransitQuantity(transfer.Movements);

            if (quantity > inTransitQuantity)
            {
                return OverPlaceConflict();
            }

            return null;
        }

        private async Task<ServiceResult<InventoryTransferDetails>> GetDetailsAsync(
            Guid transferId,
            CancellationToken cancellationToken)
        {
            InventoryTransferDetailsData? detailsData = await dbContext.InventoryTransfers
                .AsNoTracking()
                .Where(x => x.Id == transferId)
                .ProjectDetailsData()
                .SingleOrDefaultAsync(cancellationToken);

            return detailsData is null
                ? ServiceResult<InventoryTransferDetails>.Fail(ServiceError.Failure<InventoryTransfer>("InventoryTransfer was saved but could not be loaded"))
                : ServiceResult<InventoryTransferDetails>.Success(detailsData.ToDetails());
        }
    }

    internal static ServiceError CompletedTransferConflict()
    {
        return new ServiceError(
            ServiceErrorType.Conflict,
            "InventoryTransfer.Completed",
            "Completed inventory transfers cannot be placed.",
            nameof(Command.TransferId));
    }

    internal static ServiceError PlaceRequiresTransitConflict()
    {
        return new ServiceError(
            ServiceErrorType.Conflict,
            "InventoryTransfer.PlaceRequiresTransitTransfer",
            "Place is only available for transfers with a transit storage location.",
            nameof(Command.TransferId));
    }

    internal static ServiceError OverPlaceConflict()
    {
        return new ServiceError(
            ServiceErrorType.Conflict,
            "InventoryTransfer.OverPlace",
            "Place quantity exceeds the current in-transit quantity.",
            nameof(Command.Quantity));
    }

    internal static ServiceError InsufficientTransitBalanceConflict()
    {
        return new ServiceError(
            ServiceErrorType.Conflict,
            "InventoryTransfer.InsufficientTransitBalance",
            "Transit storage location does not have enough available inventory for the place.",
            nameof(Command.Quantity));
    }

    internal static ServiceError MovementConcurrencyConflict()
    {
        return new ServiceError(
            ServiceErrorType.Conflict,
            "InventoryTransfer.MovementConcurrencyConflict",
            "Inventory balances changed while the transfer place was being saved. Refresh and try again.",
            nameof(Command.TransferId));
    }
}
