namespace Myrmex.Shared.Wms.Topology;

public sealed record WarehouseDetails(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    Guid? DefaultReceivingLocationId,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

