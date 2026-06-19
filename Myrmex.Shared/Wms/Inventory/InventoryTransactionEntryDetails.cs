namespace Myrmex.Shared.Wms.Inventory;

public sealed record InventoryTransactionEntryDetails(
    Guid EntryId,
    decimal BalanceBefore,
    decimal QuantityDelta,
    decimal BalanceAfter,
    InventoryTransactionEntryDetails.StockKeepingUnitInfo Sku,
    InventoryTransactionEntryDetails.StorageLocationInfo StorageLocation)
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
