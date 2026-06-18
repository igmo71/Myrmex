using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;
using Myrmex.Shared.Wms.Inventory;
using static Myrmex.Shared.Wms.Inventory.InventoryBalanceDetails;

namespace Myrmex.Modules.Wms.Inventory.Features.InventoryBalances;

internal static class InventoryBalanceQueryableExtensions
{
    public static IQueryable<InventoryBalanceDetailsData> ProjectDetailsData(this IQueryable<InventoryBalance> queryable)
    {
        return queryable.Select(balance => new InventoryBalanceDetailsData(
        balance.Id,
        balance.Quantity,
        balance.CreatedAtUtc,
        balance.UpdatedAtUtc,
        balance.RowVersion,

        new InventoryBalanceDetailsData.StockKeepingUnitInfo(
            balance.StockKeepingUnitId,
            balance.StockKeepingUnit.Code,
            balance.StockKeepingUnit.Name,
            new InventoryBalanceDetailsData.UnitOfMeasureInfo(
                balance.StockKeepingUnit.BaseUnitOfMeasureId,
                balance.StockKeepingUnit.BaseUnitOfMeasure.Code,
                balance.StockKeepingUnit.BaseUnitOfMeasure.Symbol)),

        new InventoryBalanceDetailsData.StorageLocationInfo(
            balance.StorageLocationId,
            balance.StorageLocation.Code,
            balance.StorageLocation.Name,
            new InventoryBalanceDetailsData.WarehouseInfo(
                balance.StorageLocation.WarehouseId,
                balance.StorageLocation.Warehouse.Code,
                balance.StorageLocation.Warehouse.Name))));
    }

    public static InventoryBalanceDetails ToDetails(this InventoryBalanceDetailsData data)
    {
        return new InventoryBalanceDetails(
            data.Id,
            data.Quantity,
            data.CreatedAtUtc,
            data.UpdatedAtUtc,
            Convert.ToBase64String(data.RowVersion),
            new StockKeepingUnitInfo(
                data.Sku.Id,
                data.Sku.Code,
                data.Sku.Name,
                new UnitOfMeasureInfo(
                    data.Sku.BaseUom.Id,
                    data.Sku.BaseUom.Code,
                    data.Sku.BaseUom.Symbol)),
            new StorageLocationInfo(
                data.StorageLocation.Id,
                data.StorageLocation.Code,
                data.StorageLocation.Name,
                new WarehouseInfo(
                    data.StorageLocation.Warehouse.Id,
                    data.StorageLocation.Warehouse.Code,
                    data.StorageLocation.Warehouse.Name)));
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

internal sealed record InventoryBalanceDetailsData(
    Guid Id,
    decimal Quantity,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    byte[] RowVersion,
    InventoryBalanceDetailsData.StockKeepingUnitInfo Sku,
    InventoryBalanceDetailsData.StorageLocationInfo StorageLocation)
{
    public sealed record StockKeepingUnitInfo(
        Guid Id,
        string Code,
        string Name,
        UnitOfMeasureInfo BaseUom);

    public sealed record UnitOfMeasureInfo(
        Guid Id,
        string Code,
        string? Symbol);

    public sealed record StorageLocationInfo(
        Guid Id,
        string Code,
        string Name,
        WarehouseInfo Warehouse);

    public sealed record WarehouseInfo(
        Guid Id,
        string Code,
        string Name);
}
