namespace Myrmex.WebApp.Wms.Inventory;

public sealed record InventoryBalanceDetails(
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
}
