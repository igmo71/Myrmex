using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;

namespace Myrmex.Modules.Wms.Inventory.Features.InventoryBalances;

internal static class InventoryBalanceQueryableExtensions
{
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
        if (sortBy == ListInventoryBalances.SortBy.Quantity)
            return sortDescending
                ? queryable.OrderByDescending(x => x.Quantity).ThenBy(x => x.Id)
                : queryable.OrderBy(x => x.Quantity).ThenBy(x => x.Id);

        if (sortBy == ListInventoryBalances.SortBy.SkuCode)
            return sortDescending
                ? queryable.OrderByDescending(x => x.StockKeepingUnit.Code).ThenBy(x => x.Id)
                : queryable.OrderBy(x => x.StockKeepingUnit.Code).ThenBy(x => x.Id);

        if (sortBy == ListInventoryBalances.SortBy.SkuName)
            return sortDescending
                ? queryable.OrderByDescending(x => x.StockKeepingUnit.Name).ThenBy(x => x.Id)
                : queryable.OrderBy(x => x.StockKeepingUnit.Name).ThenBy(x => x.Id);

        if (sortBy == ListInventoryBalances.SortBy.SkuBaseUomSymbol)
            return sortDescending
                ? queryable.OrderByDescending(x => x.StockKeepingUnit.BaseUnitOfMeasure.Symbol ?? x.StockKeepingUnit.BaseUnitOfMeasure.Code).ThenBy(x => x.Id)
                : queryable.OrderBy(x => x.StockKeepingUnit.BaseUnitOfMeasure.Symbol ?? x.StockKeepingUnit.BaseUnitOfMeasure.Code).ThenBy(x => x.Id);

        if (sortBy == ListInventoryBalances.SortBy.LocationCode)
            return sortDescending
                ? queryable.OrderByDescending(x => x.StorageLocation.Code).ThenBy(x => x.Id)
                : queryable.OrderBy(x => x.StorageLocation.Code).ThenBy(x => x.Id);

        if (sortBy == ListInventoryBalances.SortBy.WarehouseName)
            return sortDescending
                ? queryable.OrderByDescending(x => x.StorageLocation.Warehouse.Name).ThenBy(x => x.Id)
                : queryable.OrderBy(x => x.StorageLocation.Warehouse.Name).ThenBy(x => x.Id);

        return sortDescending
            ? queryable.OrderByDescending(x => x.Id)
            : queryable.OrderBy(x => x.Id);
    }
}
