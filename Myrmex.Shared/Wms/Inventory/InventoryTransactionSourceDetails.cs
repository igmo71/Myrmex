namespace Myrmex.Shared.Wms.Inventory;

public sealed record InventoryTransactionSourceDetails(
    string SourceType,
    string? DocumentNumber,
    DateTimeOffset? DocumentCreatedAtUtc);
