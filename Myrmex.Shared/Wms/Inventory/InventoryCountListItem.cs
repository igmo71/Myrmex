namespace Myrmex.Shared.Wms.Inventory;

public sealed record InventoryCountListItem(
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
    int LineCount,
    int AppliedLineCount,
    int UnresolvedLineCount,
    int ConflictLineCount,
    InventoryCountListItem.WarehouseInfo Warehouse)
{
    public sealed record WarehouseInfo(Guid Id, string Code, string Name);
}
