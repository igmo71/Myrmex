namespace Myrmex.Shared.Wms.Inventory;

public sealed record PlaceInventoryTransferLineRequest(
    Guid? LineId,
    decimal Quantity);
