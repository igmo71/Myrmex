namespace Myrmex.Shared.Wms.Topology;

public sealed record LookupWarehousesRequest
{
    public string? SearchText { get; init; }

    public int? Take { get; init; }

    public bool? SelectableOnly { get; init; } = true;
}

