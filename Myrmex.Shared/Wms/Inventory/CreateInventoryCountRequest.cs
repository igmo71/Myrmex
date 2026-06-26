namespace Myrmex.Shared.Wms.Inventory;

public sealed record CreateInventoryCountRequest(
    Guid? WarehouseId,
    string? Reason);
