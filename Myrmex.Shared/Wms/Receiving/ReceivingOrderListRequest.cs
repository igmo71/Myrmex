namespace Myrmex.Shared.Wms.Receiving;

public sealed record ReceivingOrderListRequest(
    int? Skip,
    int? Take,
    string? SearchText,
    Guid? WarehouseId,
    string? Status,
    string? SortBy,
    bool? SortDescending);
