namespace Myrmex.Shared.Wms.Inventory;

public sealed record ListInventoryTransfersRequest
{
    public int? Skip { get; init; }
    public int? Take { get; init; }
    public string? SortBy { get; init; }
    public bool? SortDescending { get; init; }
    public Guid? WarehouseId { get; init; }
    public string? Status { get; init; }
}
