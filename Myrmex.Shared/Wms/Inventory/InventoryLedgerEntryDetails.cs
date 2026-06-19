namespace Myrmex.Shared.Wms.Inventory;

public sealed record InventoryLedgerEntryDetails(
    Guid EntryId,
    Guid TransactionId,
    string TransactionType,
    string Reason,
    DateTimeOffset OccurredAtUtc,
    decimal BalanceBefore,
    decimal QuantityDelta,
    decimal BalanceAfter,
    InventoryLedgerEntryDetails.StockKeepingUnitInfo Sku,
    InventoryLedgerEntryDetails.StorageLocationInfo StorageLocation)
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
