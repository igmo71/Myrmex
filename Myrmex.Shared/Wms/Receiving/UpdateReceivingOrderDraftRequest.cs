namespace Myrmex.Shared.Wms.Receiving;

public sealed record UpdateReceivingOrderDraftRequest(
    string? Number,
    Guid? WarehouseId,
    Guid? ReceivingLocationId,
    string? ExpectedOrderVersion,
    IReadOnlyList<UpdateReceivingOrderLineRequest> Lines);

public sealed record UpdateReceivingOrderLineRequest(
    Guid? LineId,
    Guid? StockKeepingUnitId,
    decimal PlannedQuantity);
