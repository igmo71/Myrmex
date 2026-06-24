namespace Myrmex.Shared.Wms.Inventory;

public sealed record MoveInventoryBalanceRequest(
    Guid? StockKeepingUnitId,
    Guid? SourceStorageLocationId,
    Guid? DestinationStorageLocationId,
    decimal Quantity,
    string? Reason,
    string? ExpectedSourceBalanceVersion);
