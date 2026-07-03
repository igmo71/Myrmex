using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Shared.Wms.Topology;
using System.Linq.Expressions;

namespace Myrmex.Modules.Wms.Topology.Features.Warehouses;

internal static class WarehouseDetailsMapping
{
    public static WarehouseDetails From(Warehouse warehouse)
    {
        return new WarehouseDetails(
            warehouse.Id,
            warehouse.Code,
            warehouse.Name,
            warehouse.Description,
            warehouse.IsActive,
            warehouse.CreatedAtUtc,
            warehouse.UpdatedAtUtc);
    }

    public static Expression<Func<Warehouse, WarehouseDetails>> Projection =>
        warehouse => new WarehouseDetails(
            warehouse.Id,
            warehouse.Code,
            warehouse.Name,
            warehouse.Description,
            warehouse.IsActive,
            warehouse.CreatedAtUtc,
            warehouse.UpdatedAtUtc);
}
