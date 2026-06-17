namespace Myrmex.Shared.Wms.Inventory;

public sealed record CreateInventoryBalanceRequest(
    Guid? StockKeepingUnitId,
    Guid? StorageLocationId,
    decimal Quantity);
