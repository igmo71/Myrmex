namespace Myrmex.Shared.Wms.Topology;

public sealed record ZoneDetails(
    Guid Id,
    Guid WarehouseId,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

