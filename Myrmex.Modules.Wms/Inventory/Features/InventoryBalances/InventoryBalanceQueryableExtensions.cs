using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;
using Myrmex.Shared.Wms.Inventory;
using static Myrmex.Shared.Wms.Inventory.InventoryBalanceDetails;

namespace Myrmex.Modules.Wms.Inventory.Features.InventoryBalances;

internal static class InventoryBalanceQueryableExtensions
{
    public static IQueryable<InventoryBalanceDetails> ProjectDetails(this IQueryable<InventoryBalance> queryable)
    {
        return queryable.Select(balance => new InventoryBalanceDetails(
        balance.Id,
        balance.Quantity,
        balance.CreatedAtUtc,
        balance.UpdatedAtUtc,

        new StockKeepingUnitInfo(
            balance.StockKeepingUnitId,
            balance.StockKeepingUnit.Code,
            balance.StockKeepingUnit.Name,
            new UnitOfMeasureInfo(
                balance.StockKeepingUnit.BaseUnitOfMeasureId,
                balance.StockKeepingUnit.BaseUnitOfMeasure.Code,
                balance.StockKeepingUnit.BaseUnitOfMeasure.Symbol)),

        new StorageLocationInfo(
            balance.StorageLocationId,
            balance.StorageLocation.Code,
            balance.StorageLocation.Name,
            new WarehouseInfo(
                balance.StorageLocation.WarehouseId,
                balance.StorageLocation.Warehouse.Code,
                balance.StorageLocation.Warehouse.Name))));
    }

    public static IQueryable<InventoryBalance> ApplyFilters(this IQueryable<InventoryBalance> queryable, ListInventoryBalances.Query query)
    {
        if (query.StockKeepingUnitId is Guid stockKeepingUnitId)
        {
            queryable = queryable
                .Where(x => x.StockKeepingUnitId == stockKeepingUnitId);
        }

        if (query.StorageLocationId is Guid storageLocationId)
        {
            queryable = queryable
                .Where(x => x.StorageLocationId == storageLocationId);
        }

        if (query.WarehouseId is Guid warehouseId)
        {
            queryable = queryable
                .Where(x => x.StorageLocation.WarehouseId == warehouseId);
        }

        return queryable;
    }

    public static IQueryable<InventoryBalance> ApplySorting(this IQueryable<InventoryBalance> queryable, string? sortBy, bool sortDescending)
    {
        if (sortBy == InventoryBalanceSortBy.Quantity)
            return sortDescending
                ? queryable.OrderByDescending(x => x.Quantity).ThenBy(x => x.Id)
                : queryable.OrderBy(x => x.Quantity).ThenBy(x => x.Id);

        if (sortBy == InventoryBalanceSortBy.SkuCode)
            return sortDescending
                ? queryable.OrderByDescending(x => x.StockKeepingUnit.Code).ThenBy(x => x.Id)
                : queryable.OrderBy(x => x.StockKeepingUnit.Code).ThenBy(x => x.Id);

        if (sortBy == InventoryBalanceSortBy.SkuName)
            return sortDescending
                ? queryable.OrderByDescending(x => x.StockKeepingUnit.Name).ThenBy(x => x.Id)
                : queryable.OrderBy(x => x.StockKeepingUnit.Name).ThenBy(x => x.Id);

        if (sortBy == InventoryBalanceSortBy.SkuBaseUomSymbol)
            return sortDescending
                ? queryable.OrderByDescending(x => x.StockKeepingUnit.BaseUnitOfMeasure.Symbol ?? x.StockKeepingUnit.BaseUnitOfMeasure.Code).ThenBy(x => x.Id)
                : queryable.OrderBy(x => x.StockKeepingUnit.BaseUnitOfMeasure.Symbol ?? x.StockKeepingUnit.BaseUnitOfMeasure.Code).ThenBy(x => x.Id);

        if (sortBy == InventoryBalanceSortBy.StorageLocationCode)
            return sortDescending
                ? queryable.OrderByDescending(x => x.StorageLocation.Code).ThenBy(x => x.Id)
                : queryable.OrderBy(x => x.StorageLocation.Code).ThenBy(x => x.Id);

        if (sortBy == InventoryBalanceSortBy.WarehouseName)
            return sortDescending
                ? queryable.OrderByDescending(x => x.StorageLocation.Warehouse.Name).ThenBy(x => x.Id)
                : queryable.OrderBy(x => x.StorageLocation.Warehouse.Name).ThenBy(x => x.Id);

        if (sortBy == InventoryBalanceSortBy.WarehouseCode)
            return sortDescending
                ? queryable.OrderByDescending(x => x.StorageLocation.Warehouse.Code).ThenBy(x => x.Id)
                : queryable.OrderBy(x => x.StorageLocation.Warehouse.Code).ThenBy(x => x.Id);

        return sortDescending
            ? queryable.OrderByDescending(x => x.Id)
            : queryable.OrderBy(x => x.Id);
    }
}
