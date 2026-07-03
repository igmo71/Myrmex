namespace Myrmex.Shared.Wms.Topology;

public sealed record CreateZoneRequest(
    string? Code,
    string? Name,
    string? Description);

