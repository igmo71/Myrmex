using Myrmex.Modules.Wms.Topology.Domain.Zones;
using System.Linq.Expressions;

namespace Myrmex.Modules.Wms.Topology.Features.Zones;

internal sealed record ZoneDetails(
    Guid Id,
    Guid WarehouseId,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc)
{
    public static ZoneDetails From(Zone zone)
    {
        return new ZoneDetails(
            zone.Id,
            zone.WarehouseId,
            zone.Code,
            zone.Name,
            zone.Description,
            zone.IsActive,
            zone.CreatedAtUtc,
            zone.UpdatedAtUtc);
    }

    public static Expression<Func<Zone, ZoneDetails>> Projection =>
        zone => new ZoneDetails(
            zone.Id,
            zone.WarehouseId,
            zone.Code,
            zone.Name,
            zone.Description,
            zone.IsActive,
            zone.CreatedAtUtc,
            zone.UpdatedAtUtc);
}