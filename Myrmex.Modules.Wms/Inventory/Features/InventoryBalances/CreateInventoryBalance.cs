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
        public async Task<ServiceResult<InventoryBalanceDetails>> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            DomainValidationResult validationResult = InventoryBalance.Create(
                command.StockKeepingUnitId,
                command.StorageLocationId,
                command.Quantity,
                out InventoryBalance? inventoryBalance);

            if (!validationResult.IsValid)
            {
                return ServiceResult<InventoryBalanceDetails>.Invalid(validationResult.Errors);
            }

            if (inventoryBalance is null)
            {
                return ServiceResult<InventoryBalanceDetails>.Fail(WmsErrors.InventoryBalance.CreateFailed);
            }

            var stockKeepingUnit = await dbContext.StockKeepingUnits
                .AsNoTracking()
                .Where(x => x.Id == inventoryBalance.StockKeepingUnitId)
                .Select(x => new { x.Id, x.IsActive, x.BaseUnitOfMeasureId })
                .FirstOrDefaultAsync(cancellationToken);

            if (stockKeepingUnit is null)
            {
                return ServiceResult<InventoryBalanceDetails>.Fail(WmsErrors.InventoryBalance.StockKeepingUnitNotFound);
            }

            if (!stockKeepingUnit.IsActive || stockKeepingUnit.BaseUnitOfMeasureId == Guid.Empty)
            {
                return ServiceResult<InventoryBalanceDetails>.Fail(WmsErrors.InventoryBalance.InvalidStockKeepingUnit);
            }

            bool baseUnitOfMeasureIsActive = await dbContext.UnitsOfMeasure
                .AnyAsync(
                    x => x.Id == stockKeepingUnit.BaseUnitOfMeasureId && x.IsActive,
                    cancellationToken);

            if (!baseUnitOfMeasureIsActive)
            {
                return ServiceResult<InventoryBalanceDetails>.Fail(WmsErrors.InventoryBalance.InvalidStockKeepingUnit);
            }

            var storageLocation = await dbContext.StorageLocations
                .AsNoTracking()
                .Where(x => x.Id == inventoryBalance.StorageLocationId)
                .Select(x => new
                {
                    x.Id,
                    x.StorageLocationTypeId,
                    x.StorageLocationStatusId,
                    x.IsActive
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (storageLocation is null)
            {
                return ServiceResult<InventoryBalanceDetails>.Fail(WmsErrors.InventoryBalance.StorageLocationNotFound);
            }

            if (!storageLocation.IsActive)
            {
                return ServiceResult<InventoryBalanceDetails>.Fail(WmsErrors.InventoryBalance.InvalidStorageLocation);
            }

            bool storageLocationTypeIsActive = await dbContext.StorageLocationTypes
                .AnyAsync(
                    x => x.Id == storageLocation.StorageLocationTypeId && x.IsActive,
                    cancellationToken);

            if (!storageLocationTypeIsActive)
            {
                return ServiceResult<InventoryBalanceDetails>.Fail(WmsErrors.InventoryBalance.InactiveStorageLocationType);
            }

            bool storageLocationStatusIsActive = await dbContext.StorageLocationStatuses
                .AnyAsync(
                    x => x.Id == storageLocation.StorageLocationStatusId && x.IsActive,
                    cancellationToken);

            if (!storageLocationStatusIsActive)
            {
                return ServiceResult<InventoryBalanceDetails>.Fail(WmsErrors.InventoryBalance.InactiveStorageLocationStatus);
            }

            bool duplicateExists = await dbContext.InventoryBalances
                .AnyAsync(
                    x => x.StockKeepingUnitId == inventoryBalance.StockKeepingUnitId &&
                         x.StorageLocationId == inventoryBalance.StorageLocationId,
                    cancellationToken);

            if (duplicateExists)
            {
                return ServiceResult<InventoryBalanceDetails>.Fail(WmsErrors.InventoryBalance.DuplicateStockKeepingUnitStorageLocation);
            }

            dbContext.InventoryBalances.Add(inventoryBalance);

            ServiceResult saveResult = await dbContext
                .SaveChangesAsServiceResultAsync(domainEventDispatcher, cancellationToken);

            if (!saveResult.IsSuccess)
            {
                return ServiceResult<InventoryBalanceDetails>.Fail(saveResult.Error);
            }

            InventoryBalanceDetails? details = await ProjectDetails(inventoryBalance.Id)
                .SingleOrDefaultAsync(cancellationToken);

            return details is null
                ? ServiceResult<InventoryBalanceDetails>.Fail(WmsErrors.InventoryBalance.CreateFailed)
                : ServiceResult<InventoryBalanceDetails>.Success(details);
        }

        private IQueryable<InventoryBalanceDetails> ProjectDetails(Guid inventoryBalanceId)
        {
            return dbContext.InventoryBalances
                .AsNoTracking()
                .Where(inventoryBalance => inventoryBalance.Id == inventoryBalanceId)
                .Join(
                    dbContext.StockKeepingUnits.AsNoTracking(),
                    inventoryBalance => inventoryBalance.StockKeepingUnitId,
                    stockKeepingUnit => stockKeepingUnit.Id,
                    (inventoryBalance, stockKeepingUnit) => new
                    {
                        InventoryBalance = inventoryBalance,
                        StockKeepingUnit = stockKeepingUnit
                    })
                .Join(
                    dbContext.StorageLocations.AsNoTracking(),
                    x => x.InventoryBalance.StorageLocationId,
                    storageLocation => storageLocation.Id,
                    (x, storageLocation) => new
                    {
                        x.InventoryBalance,
                        x.StockKeepingUnit,
                        StorageLocation = storageLocation
                    })
                .Join(
                    dbContext.Warehouses.AsNoTracking(),
                    x => x.StorageLocation.WarehouseId,
                    warehouse => warehouse.Id,
                    (x, warehouse) => new
                    {
                        x.InventoryBalance,
                        x.StockKeepingUnit,
                        x.StorageLocation,
                        Warehouse = warehouse
                    })
                .Join(
                    dbContext.UnitsOfMeasure.AsNoTracking(),
                    x => x.StockKeepingUnit.BaseUnitOfMeasureId,
                    baseUnitOfMeasure => baseUnitOfMeasure.Id,
                    (x, baseUnitOfMeasure) => new InventoryBalanceDetails(
                        x.InventoryBalance.Id,
                        x.InventoryBalance.StockKeepingUnitId,
                        x.StockKeepingUnit.Code,
                        x.StockKeepingUnit.Name,
                        x.InventoryBalance.StorageLocationId,
                        x.StorageLocation.Code,
                        x.StorageLocation.Name,
                        x.Warehouse.Id,
                        x.Warehouse.Code,
                        x.Warehouse.Name,
                        baseUnitOfMeasure.Id,
                        baseUnitOfMeasure.Code,
                        baseUnitOfMeasure.Symbol,
                        x.InventoryBalance.Quantity,
                        x.InventoryBalance.CreatedAtUtc,
                        x.InventoryBalance.UpdatedAtUtc));
        }
    }
}
