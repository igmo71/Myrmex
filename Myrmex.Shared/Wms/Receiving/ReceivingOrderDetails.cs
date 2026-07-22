namespace Myrmex.Shared.Wms.Receiving;

public sealed record ReceivingOrderDetails(
    Guid Id,
    string OrderVersion,
    string Number,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    Guid? InventoryTransactionId,
    ReceivingOrderDetails.WarehouseInfo Warehouse,
    ReceivingOrderDetails.StorageLocationInfo ReceivingLocation,
    int LineCount,
    decimal TotalPlannedQuantity,
    decimal TotalReceivedQuantity,
    decimal TotalRemainingQuantity,
    IReadOnlyList<ReceivingOrderLineDetails> Lines)
{
    public sealed record WarehouseInfo(Guid Id, string Code, string Name);

    public sealed record StorageLocationInfo(Guid Id, string Code, string Name);
}
