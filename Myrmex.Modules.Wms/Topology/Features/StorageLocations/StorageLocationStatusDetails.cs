using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;
using Myrmex.Shared.Wms.Topology;
using System.Linq.Expressions;

namespace Myrmex.Modules.Wms.Topology.Features.StorageLocations;

internal static class StorageLocationStatusDetailsMapping
{
    public static Expression<Func<StorageLocationStatus, StorageLocationStatusDetails>> Projection =>
        status => new StorageLocationStatusDetails(
            status.Id,
            status.Code,
            status.Name,
            status.Description,
            status.IsSystem,
            status.IsActive,
            status.SortOrder);
}
