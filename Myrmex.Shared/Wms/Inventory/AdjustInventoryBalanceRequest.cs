namespace Myrmex.Shared.Wms.Inventory;

public sealed record AdjustInventoryBalanceRequest(
    Guid? StockKeepingUnitId,
    Guid? StorageLocationId,
    decimal CountedQuantity,
    string? Reason,
    string? ExpectedBalanceVersion);
