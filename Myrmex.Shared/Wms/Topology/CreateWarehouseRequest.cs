namespace Myrmex.Shared.Wms.Topology;

public sealed record CreateWarehouseRequest(
    string? Code,
    string? Name,
    string? Description);

