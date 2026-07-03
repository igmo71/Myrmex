using Myrmex.Modules.Wms.Topology.Domain.Zones;
using Myrmex.Shared.Wms.Topology;
using System.Linq.Expressions;

namespace Myrmex.Modules.Wms.Topology.Features.Zones;

internal static class ZoneDetailsMapping
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
