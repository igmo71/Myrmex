using Microsoft.EntityFrameworkCore;
using Myrmex.AppDispatching.EventDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;

namespace Myrmex.Modules.Wms.Inventory.Features.InventoryBalances;

internal static class CreateInventoryBalance
{
    internal sealed record Command(
        Guid? StockKeepingUnitId,
        Guid? StorageLocationId,
        decimal Quantity) : ICommand<ServiceResult<InventoryBalanceDetails>>;

    internal sealed class Handler(
        WmsDbContext dbContext,
        IDomainEventDispatcher domainEventDispatcher)
        : ICommandHandler<Command, ServiceResult<InventoryBalanceDetails>>
    {
        public async Task<ServiceResult<InventoryBalanceDetails>> HandleAsync(Command command, CancellationToken cancellationToken = default)
        {
            DomainValidationResult domainValidationResult = InventoryBalance.Create(
                command.StockKeepingUnitId,
                command.StorageLocationId,
                command.Quantity,
                out InventoryBalance? validInventoryBalance);

            if (!domainValidationResult.IsValid)
            {
                return ServiceResult<InventoryBalanceDetails>.Invalid(domainValidationResult.Errors);
            }

            InventoryBalance? inventoryBalance = validInventoryBalance!;

            ServiceError? externalValidationError = await ValidateExternalDependenciesAsync(inventoryBalance, cancellationToken);

            if (externalValidationError is not null)
            {
                return ServiceResult<InventoryBalanceDetails>.Fail(externalValidationError);
            }

            bool duplicateExists = await dbContext.InventoryBalances
                .AnyAsync(
                    x => x.StockKeepingUnitId == inventoryBalance.StockKeepingUnitId &&
                         x.StorageLocationId == inventoryBalance.StorageLocationId,
                    cancellationToken);

            if (duplicateExists)
            {
                return ServiceResult<InventoryBalanceDetails>.Fail(ServiceError.Conflict<InventoryBalance>("StockKeepingUnitId, StorageLocationId already exists", "StockKeepingUnitId, StorageLocationId"));
            }

            dbContext.InventoryBalances.Add(inventoryBalance);

            ServiceResult saveResult = await dbContext
                .SaveChangesAsServiceResultAsync(domainEventDispatcher, cancellationToken);

            if (!saveResult.IsSuccess)
            {
                return ServiceResult<InventoryBalanceDetails>.Fail(saveResult.Error);
            }

            InventoryBalanceDetails? details = await dbContext.InventoryBalances
                .Where(x => x.Id == inventoryBalance.Id)
                .Select(InventoryBalanceDetails.Project)
                .SingleOrDefaultAsync(cancellationToken);

            return details is null
                ? ServiceResult<InventoryBalanceDetails>.Fail(ServiceError.Failure<InventoryBalance>("Failed to create InventoryBalance"))
                : ServiceResult<InventoryBalanceDetails>.Success(details);
        }

        private async Task<ServiceError?> ValidateExternalDependenciesAsync(InventoryBalance balance, CancellationToken cancellationToken)
        {
            var sku = await dbContext.StockKeepingUnits
                .AsNoTracking()
                .Include(s => s.BaseUnitOfMeasure)
                .FirstOrDefaultAsync(x => x.Id == balance.StockKeepingUnitId, cancellationToken);

            var location = await dbContext.StorageLocations
                .AsNoTracking()
                .Include(l => l.StorageLocationType)
                .Include(l => l.StorageLocationStatus)
                .FirstOrDefaultAsync(x => x.Id == balance.StorageLocationId, cancellationToken);

            return (sku, location) switch
            {
                (null, _) => ServiceError.NotFound<InventoryBalance>("StockKeepingUnit not found", "StockKeepingUnit"),
                ({ IsActive: false }, _) => ServiceError.Validation<InventoryBalance>("StockKeepingUnit is inactive", "StockKeepingUnit"),
                ({ BaseUnitOfMeasure.IsActive: false }, _) => ServiceError.Validation<InventoryBalance>("BaseUnitOfMeasure is inactive", "BaseUnitOfMeasure"),

                (_, null) => ServiceError.NotFound<InventoryBalance>("StorageLocation not found", "StorageLocation"),
                (_, { IsActive: false }) => ServiceError.Validation<InventoryBalance>("StorageLocation is inactive", "StorageLocation"),
                (_, { StorageLocationType.IsActive: false }) => ServiceError.Validation<InventoryBalance>("StorageLocationType is inactive", "StorageLocationType"),
                (_, { StorageLocationStatus.IsActive: false }) => ServiceError.Validation<InventoryBalance>("StorageLocationStatus is inactive", "StorageLocationStatus"),

                _ => null
            };
        }
    }
}
