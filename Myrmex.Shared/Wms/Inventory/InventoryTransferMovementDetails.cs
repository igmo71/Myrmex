namespace Myrmex.Shared.Wms.Inventory;

public sealed record InventoryTransferMovementDetails(
    Guid Id,
    Guid LineId,
    Guid InventoryTransactionId,
    DateTimeOffset OccurredAtUtc,
    decimal Quantity,
    InventoryTransferMovementDetails.StockKeepingUnitInfo Sku,
    InventoryTransferMovementDetails.StorageLocationInfo FromStorageLocation,
    InventoryTransferMovementDetails.StorageLocationInfo ToStorageLocation)
{
    public sealed record StockKeepingUnitInfo(
        Guid Id,
        string Code,
        string Name,
        UnitOfMeasureInfo BaseUom);

    public sealed record UnitOfMeasureInfo(Guid Id, string Code, string? Symbol);

    public sealed record StorageLocationInfo(Guid Id, string Code, string Name);
}
