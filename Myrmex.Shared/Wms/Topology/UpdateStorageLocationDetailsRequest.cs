namespace Myrmex.Shared.Wms.Topology;

public sealed record UpdateStorageLocationDetailsRequest(
    string? Name,
    string? Description,
    bool IsPickable);

