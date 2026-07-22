namespace Myrmex.Shared.Wms.Receiving;

public sealed record CreateReceivingOrderRequest(
    string? Number,
    Guid? WarehouseId,
    Guid? ReceivingLocationId,
    IReadOnlyList<CreateReceivingOrderLineRequest> Lines);

public sealed record CreateReceivingOrderLineRequest(
    Guid? StockKeepingUnitId,
    decimal PlannedQuantity);
