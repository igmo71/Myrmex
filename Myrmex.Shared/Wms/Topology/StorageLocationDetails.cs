namespace Myrmex.Shared.Wms.Topology;

public sealed record StorageLocationDetails(
    Guid Id,
    Guid WarehouseId,
    Guid ZoneId,
    Guid StorageLocationTypeId,
    Guid StorageLocationStatusId,
    string Code,
    string Name,
    string? Description,
    bool IsPickable,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

