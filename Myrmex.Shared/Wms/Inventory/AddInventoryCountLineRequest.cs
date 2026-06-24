namespace Myrmex.Shared.Wms.Inventory;

public sealed record AddInventoryCountLineRequest(
    Guid? StockKeepingUnitId,
    Guid? StorageLocationId,
    string? ExpectedCountVersion);
