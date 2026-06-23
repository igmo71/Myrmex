namespace Myrmex.Shared.Wms.Inventory;

public sealed record CreateInventoryTransferLineRequest(
    Guid? StockKeepingUnitId,
    Guid? SourceStorageLocationId,
    Guid? DestinationStorageLocationId,
    decimal RequestedQuantity);
