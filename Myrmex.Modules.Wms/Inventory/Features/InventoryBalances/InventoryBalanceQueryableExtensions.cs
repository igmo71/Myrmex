using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;
using System.Linq.Expressions;

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

        return sortDescending
            ? query.OrderByDescending(GetSortExpression(sortBy)).ThenBy(x => x.Id)
            : query.OrderBy(GetSortExpression(sortBy)).ThenBy(x => x.Id);
    }

    private static Expression<Func<InventoryBalance, object>> GetSortExpression(string sortBy)
    {
        return sortBy switch
        {
            nameof(InventoryBalanceDetails.Quantity) => x => x.Quantity,
            nameof(InventoryBalanceDetails.CreatedAtUtc) => x => x.CreatedAtUtc,

            _ when sortBy == InventoryBalanceDetails.SortPath.SkuCode => x => x.StockKeepingUnit.Code,
            _ when sortBy == InventoryBalanceDetails.SortPath.SkuName => x => x.StockKeepingUnit.Name,
            _ when sortBy == InventoryBalanceDetails.SortPath.LocationCode => x => x.StorageLocation.Code,
            _ when sortBy == InventoryBalanceDetails.SortPath.WarehouseName => x => x.StorageLocation.Warehouse.Name,

            _ => x => x.Id
        };
    }
}
