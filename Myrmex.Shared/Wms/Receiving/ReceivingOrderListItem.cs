namespace Myrmex.Shared.Wms.Receiving;

public sealed record ReceivingOrderListItem(
    Guid Id,
    string OrderVersion,
    string Number,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    Guid? InventoryTransactionId,
    ReceivingOrderListItem.WarehouseInfo Warehouse,
    ReceivingOrderListItem.StorageLocationInfo ReceivingLocation,
    int LineCount,
    decimal TotalPlannedQuantity,
    decimal TotalReceivedQuantity,
    decimal TotalRemainingQuantity)
{
    public sealed record WarehouseInfo(Guid Id, string Code, string Name);

    public sealed record StorageLocationInfo(Guid Id, string Code, string Name);
}
