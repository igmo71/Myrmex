namespace Myrmex.Shared.Wms.Topology;

public sealed record LookupStorageLocationsRequest
{
    public string? SearchText { get; init; }

    public int? Take { get; init; }

    public bool SelectableOnly { get; init; } = true;
}
