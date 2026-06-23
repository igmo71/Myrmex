namespace Myrmex.Shared.Wms.Inventory;

public sealed record MoveInventoryTransferLineRequest(
    Guid? LineId,
    decimal Quantity);
