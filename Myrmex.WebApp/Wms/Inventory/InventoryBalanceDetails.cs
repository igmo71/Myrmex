namespace Myrmex.WebApp.Wms.Inventory;

public sealed record InventoryBalanceDetails(
    Guid Id,
    decimal Quantity,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    InventoryBalanceDetails.StockKeepingUnitInfo Sku,
    InventoryBalanceDetails.StorageLocationInfo Location)
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
