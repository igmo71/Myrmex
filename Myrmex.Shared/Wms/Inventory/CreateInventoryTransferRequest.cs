namespace Myrmex.Shared.Wms.Inventory;

public sealed record CreateInventoryTransferRequest(
    Guid? SourceWarehouseId,
    Guid? DestinationWarehouseId,
    Guid? TransitStorageLocationId,
    IReadOnlyList<CreateInventoryTransferLineRequest> Lines);
