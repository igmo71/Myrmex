using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;
using System.Linq.Expressions;

namespace Myrmex.Modules.Wms.Inventory.Features.InventoryBalances;

internal sealed record InventoryBalanceDetails(
    Guid Id,
    decimal Quantity,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    InventoryBalanceDetails.SkuInfo Sku,
    InventoryBalanceDetails.LocationInfo Location)
{
    public sealed record SkuInfo(
        Guid Id,
        string Code,
        string Name,
        UomInfo BaseUom);

    public sealed record UomInfo(
        Guid Id,
        string Code,
        string? Symbol);

    public sealed record LocationInfo(
        Guid Id,
        string Code,
        string Name,
        WarehouseInfo Warehouse);

    public sealed record WarehouseInfo(
        Guid Id,
        string Code,
        string Name);

    public static Expression<Func<InventoryBalance, InventoryBalanceDetails>> Project =>
        balance => new InventoryBalanceDetails(
            balance.Id,
            balance.Quantity,
            balance.CreatedAtUtc,
            balance.UpdatedAtUtc,

            new SkuInfo(
                balance.StockKeepingUnitId,
                balance.StockKeepingUnit.Code,
                balance.StockKeepingUnit.Name,
                new UomInfo(
                    balance.StockKeepingUnit.BaseUnitOfMeasureId,
                    balance.StockKeepingUnit.BaseUnitOfMeasure.Code,
                    balance.StockKeepingUnit.BaseUnitOfMeasure.Symbol
                )
            ),

            new LocationInfo(
                balance.StorageLocationId,
                balance.StorageLocation.Code,
                balance.StorageLocation.Name,
                new WarehouseInfo(
                    balance.StorageLocation.WarehouseId,
                    balance.StorageLocation.Warehouse.Code,
                    balance.StorageLocation.Warehouse.Name
                )
            )
        );

    internal static class SortPath
    {
        public static readonly string SkuCode = $"{nameof(Sku)}.{nameof(SkuInfo.Code)}";       // "Sku.Code"
        public static readonly string SkuName = $"{nameof(Sku)}.{nameof(SkuInfo.Name)}";       // "Sku.Name"
        public static readonly string LocationCode = $"{nameof(Location)}.{nameof(LocationInfo.Code)}"; // "Location.Code"
        public static readonly string WarehouseName = $"{nameof(Location)}.{nameof(LocationInfo.Warehouse)}.{nameof(WarehouseInfo.Name)}"; // "Location.Warehouse.Name"
    }
}