namespace Myrmex.Shared.Wms.Inventory;

public sealed record InventoryCountDetails(
    Guid Id,
    string CountVersion,
    string Status,
    string? Reason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    string CreatedByActorId,
    string? CompletedByActorId,
    string? CancelledByActorId,
    InventoryCountDetails.WarehouseInfo Warehouse,
    IReadOnlyList<InventoryCountLineDetails> Lines)
{
    public sealed record WarehouseInfo(Guid Id, string Code, string Name);
}
