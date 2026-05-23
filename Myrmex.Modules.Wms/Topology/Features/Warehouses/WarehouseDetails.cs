namespace Myrmex.Modules.Wms.Topology.Features.Warehouses;

internal sealed record WarehouseDetails(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc)
{ }