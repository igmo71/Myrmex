namespace Myrmex.Shared.Wms.Topology;

public sealed record ListStorageLocationsRequest
{
    public Guid? WarehouseId { get; init; }

    public Guid? ZoneId { get; init; }

    public Guid? StorageLocationTypeId { get; init; }

    public Guid? StorageLocationStatusId { get; init; }

    public string? SearchText { get; init; }

    public bool? IncludeInactive { get; init; }

    public int? Skip { get; init; }

    public int? Take { get; init; }

    public string? SortBy { get; init; }

    public bool? SortDescending { get; init; }
}

