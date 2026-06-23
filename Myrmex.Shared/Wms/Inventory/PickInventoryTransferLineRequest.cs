namespace Myrmex.Shared.Wms.Inventory;

public sealed record PickInventoryTransferLineRequest(
    Guid? LineId,
    decimal Quantity);
