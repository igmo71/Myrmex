namespace Myrmex.Modules.Wms.Infrastructure.Persistence;

internal static class WmsDatabaseNames
{
    public const string WarehousesTable = "warehouses";
    public const string WarehousePrimaryKey = "PK_wms_warehouses";
    public const string WarehouseCodeUniqueIndex = "UX_wms_warehouses_code";

    public const string ZonesTable = "zones";
    public const string ZonePrimaryKey = "PK_wms_zones";
    public const string ZoneWarehouseForeignKey = "FK_wms_zones_warehouses_warehouse_id";
    public const string ZoneWarehouseIdIndex = "IX_wms_zones_warehouse_id";
    public const string ZoneWarehouseIdCodeUniqueIndex = "UX_wms_zones_warehouse_id_code";
}

