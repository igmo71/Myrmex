using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;
using Myrmex.Shared.Wms.Topology;
using System.Linq.Expressions;

namespace Myrmex.Modules.Wms.Topology.Features.StorageLocations;

internal static class StorageLocationTypeDetailsMapping
{
    public static Expression<Func<StorageLocationType, StorageLocationTypeDetails>> Projection =>
        type => new StorageLocationTypeDetails(
            type.Id,
            type.Code,
            type.Name,
            type.Description,
            type.IsSystem,
            type.IsActive,
            type.SortOrder);
}
