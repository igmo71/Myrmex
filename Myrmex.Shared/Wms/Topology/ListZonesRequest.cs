namespace Myrmex.Shared.Wms.Topology;

public sealed record ListZonesRequest
{
    public Guid? WarehouseId { get; init; }

    public int? Skip { get; init; }

    public int? Take { get; init; }

    public string? SearchText { get; init; }

    public string? SortBy { get; init; }

    public bool? SortDescending { get; init; }

    public bool? IncludeInactive { get; init; }
}

