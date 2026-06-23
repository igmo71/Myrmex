namespace Myrmex.Shared.Wms.Inventory;

public sealed record ListInventoryTransfersRequest
{
    public int? Skip { get; init; }
    public int? Take { get; init; }
    public string? SortBy { get; init; }
    public bool? SortDescending { get; init; }
    public Guid? WarehouseId { get; init; }
    public string? Status { get; init; }
    public DateTimeOffset? CreatedFromUtc { get; init; }
    public DateTimeOffset? CreatedToUtc { get; init; }
    public string? TransferCode { get; init; }
    public Guid? SourceStorageLocationId { get; init; }
    public Guid? DestinationStorageLocationId { get; init; }
    public Guid? StockKeepingUnitId { get; init; }
    public bool? HasTransitLocation { get; init; }
}
