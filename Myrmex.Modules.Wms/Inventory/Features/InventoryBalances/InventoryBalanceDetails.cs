using Microsoft.EntityFrameworkCore;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;

namespace Myrmex.Modules.Wms.Inventory.Features.InventoryBalances;

internal sealed record InventoryBalanceDetails(
    Guid Id,
    Guid StockKeepingUnitId,
    string StockKeepingUnitCode,
    string StockKeepingUnitName,
    Guid StorageLocationId,
    string StorageLocationCode,
    string StorageLocationName,
    Guid WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    Guid BaseUnitOfMeasureId,
    string BaseUnitOfMeasureCode,
    string? BaseUnitOfMeasureSymbol,
    decimal Quantity,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc)
{
    internal static IQueryable<InventoryBalanceDetails> QueryFrom(
        WmsDbContext dbContext,
        IQueryable<InventoryBalance> inventoryBalances)
    {
        return inventoryBalances
            .AsNoTracking()
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
