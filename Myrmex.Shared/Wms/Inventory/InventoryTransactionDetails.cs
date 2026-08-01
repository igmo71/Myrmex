namespace Myrmex.Shared.Wms.Inventory;

public sealed record InventoryTransactionDetails(
    Guid Id,
    string TransactionType,
    string Reason,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<InventoryTransactionEntryDetails> Entries,
    InventoryTransactionSourceDetails? Source = null);
