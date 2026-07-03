namespace Myrmex.Shared.Wms.Topology;

public sealed record CreateStorageLocationRequest(
    Guid StorageLocationTypeId,
    Guid StorageLocationStatusId,
    string? Code,
    string? Name,
    string? Description,
    bool IsPickable);

