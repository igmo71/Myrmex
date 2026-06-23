namespace Myrmex.Shared.Wms.Inventory;

public sealed record InventoryTransferDetails(
    Guid Id,
    string Code,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    InventoryTransferDetails.WarehouseInfo SourceWarehouse,
    InventoryTransferDetails.WarehouseInfo DestinationWarehouse,
    InventoryTransferDetails.StorageLocationInfo? TransitStorageLocation,
    IReadOnlyList<InventoryTransferLineDetails> Lines,
    IReadOnlyList<InventoryTransferMovementDetails> Movements)
{
    public sealed record WarehouseInfo(Guid Id, string Code, string Name);

    public sealed record StorageLocationInfo(Guid Id, string Code, string Name);
}
