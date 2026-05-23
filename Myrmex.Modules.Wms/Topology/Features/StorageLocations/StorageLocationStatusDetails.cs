using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;
using System.Linq.Expressions;

namespace Myrmex.Modules.Wms.Topology.Features.StorageLocations;

internal sealed record StorageLocationStatusDetails(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsSystem,
    bool IsActive,
    int SortOrder)
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