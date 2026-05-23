using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;
using System.Linq.Expressions;

namespace Myrmex.Modules.Wms.Topology.Features.StorageLocations;

internal sealed record StorageLocationTypeDetails(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsSystem,
    bool IsActive,
    int SortOrder)
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