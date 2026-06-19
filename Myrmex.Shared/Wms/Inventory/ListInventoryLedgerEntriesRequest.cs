namespace Myrmex.Shared.Wms.Inventory;

public sealed record ListInventoryLedgerEntriesRequest
{
    public int? Skip { get; init; }
    public int? Take { get; init; }
    public string? SortBy { get; init; }
    public bool? SortDescending { get; init; }
    public Guid? StockKeepingUnitId { get; init; }
    public Guid? WarehouseId { get; init; }
    public Guid? StorageLocationId { get; init; }
    public string? TransactionType { get; init; }
    public DateTimeOffset? OccurredFromUtc { get; init; }
    public DateTimeOffset? OccurredToUtc { get; init; }
}
