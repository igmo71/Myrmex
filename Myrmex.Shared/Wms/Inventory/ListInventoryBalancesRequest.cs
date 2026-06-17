namespace Myrmex.Shared.Wms.Inventory;

public sealed record ListInventoryBalancesRequest
{
    public int? Skip { get; init; }
    public int? Take { get; init; }
    public string? SortBy { get; init; }
    public bool? SortDescending { get; init; }
    public Guid? StockKeepingUnitId { get; init; }
    public Guid? StorageLocationId { get; init; }
    public Guid? WarehouseId { get; init; }
}
