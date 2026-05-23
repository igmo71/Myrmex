using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Modules.Wms.Topology.Features.Warehouses;

internal static class WarehouseMappings
{
    public static WarehouseDetails ToDetails(this Warehouse warehouse)
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
}