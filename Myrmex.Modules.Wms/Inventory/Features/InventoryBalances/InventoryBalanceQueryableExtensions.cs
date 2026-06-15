using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;

namespace Myrmex.Modules.Wms.Inventory.Features.InventoryBalances;

internal static class InventoryBalanceQueryableExtensions
{
    public static IQueryable<InventoryBalance> ApplySorting(
            this IQueryable<InventoryBalance> query,
            string? sortBy,
            bool sortDescending)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            return sortDescending ? query.OrderByDescending(x => x.Id) : query.OrderBy(x => x.Id);
        }

        return sortBy switch
        {
            nameof(InventoryBalanceDetails.Quantity) => sortDescending
                ? query.OrderByDescending(x => x.Quantity).ThenBy(x => x.Id)
                : query.OrderBy(x => x.Quantity).ThenBy(x => x.Id),

            nameof(InventoryBalanceDetails.CreatedAtUtc) => sortDescending
                ? query.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id)
                : query.OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id),

            _ when sortBy == InventoryBalanceDetails.SortPath.SkuCode => sortDescending
                ? query.OrderByDescending(x => x.StockKeepingUnit.Code).ThenBy(x => x.Id)
                : query.OrderBy(x => x.StockKeepingUnit.Code).ThenBy(x => x.Id),

            _ when sortBy == InventoryBalanceDetails.SortPath.SkuName => sortDescending
                ? query.OrderByDescending(x => x.StockKeepingUnit.Name).ThenBy(x => x.Id)
                : query.OrderBy(x => x.StockKeepingUnit.Name).ThenBy(x => x.Id),

            _ when sortBy == InventoryBalanceDetails.SortPath.SkuNameBaseUomSymbol => sortDescending
                ? query.OrderByDescending(x => x.StockKeepingUnit.BaseUnitOfMeasure.Symbol).ThenBy(x => x.Id)
                : query.OrderBy(x => x.StockKeepingUnit.BaseUnitOfMeasure.Symbol).ThenBy(x => x.Id),

            _ when sortBy == InventoryBalanceDetails.SortPath.LocationCode => sortDescending
                ? query.OrderByDescending(x => x.StorageLocation.Code).ThenBy(x => x.Id)
                : query.OrderBy(x => x.StorageLocation.Code).ThenBy(x => x.Id),

            _ when sortBy == InventoryBalanceDetails.SortPath.WarehouseName => sortDescending
                ? query.OrderByDescending(x => x.StorageLocation.Warehouse.Name).ThenBy(x => x.Id)
                : query.OrderBy(x => x.StorageLocation.Warehouse.Name).ThenBy(x => x.Id),

            _ => sortDescending
                ? query.OrderByDescending(x => x.Id)
                : query.OrderBy(x => x.Id)
        };
    }
}