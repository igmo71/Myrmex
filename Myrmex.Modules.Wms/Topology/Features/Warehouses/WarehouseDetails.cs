using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using System.Linq.Expressions;

namespace Myrmex.Modules.Wms.Topology.Features.Warehouses;

internal sealed record WarehouseDetails(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc)
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