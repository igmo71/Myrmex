namespace Myrmex.Shared.Wms.Inventory;

public sealed record InventoryTransferListItem(
    Guid Id,
    string Code,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    decimal RequestedQuantity,
    InventoryTransferListItem.WarehouseInfo SourceWarehouse,
    InventoryTransferListItem.WarehouseInfo DestinationWarehouse,
    InventoryTransferListItem.StorageLocationInfo? TransitStorageLocation)
{
    public sealed record WarehouseInfo(Guid Id, string Code, string Name);

    public sealed record StorageLocationInfo(Guid Id, string Code, string Name);
}
