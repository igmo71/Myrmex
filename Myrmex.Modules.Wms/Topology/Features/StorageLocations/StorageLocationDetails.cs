using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;
using Myrmex.Shared.Wms.Topology;
using System.Linq.Expressions;

namespace Myrmex.Modules.Wms.Topology.Features.StorageLocations;

internal static class StorageLocationDetailsMapping
{
    public static StorageLocationDetails From(StorageLocation storageLocation)
    {
        return new StorageLocationDetails(
            storageLocation.Id,
            storageLocation.WarehouseId,
            storageLocation.ZoneId,
            storageLocation.StorageLocationTypeId,
            storageLocation.StorageLocationStatusId,
            storageLocation.Code,
            storageLocation.Name,
            storageLocation.Description,
            storageLocation.IsPickable,
            storageLocation.IsActive,
            storageLocation.CreatedAtUtc,
            storageLocation.UpdatedAtUtc);
    }

    public static Expression<Func<StorageLocation, StorageLocationDetails>> Projection =>
        storageLocation => new StorageLocationDetails(
            storageLocation.Id,
            storageLocation.WarehouseId,
            storageLocation.ZoneId,
            storageLocation.StorageLocationTypeId,
            storageLocation.StorageLocationStatusId,
            storageLocation.Code,
            storageLocation.Name,
            storageLocation.Description,
            storageLocation.IsPickable,
            storageLocation.IsActive,
            storageLocation.CreatedAtUtc,
            storageLocation.UpdatedAtUtc);
}
