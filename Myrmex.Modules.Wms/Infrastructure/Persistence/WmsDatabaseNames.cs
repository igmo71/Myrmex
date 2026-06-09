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

    public const string StorageLocationsTable = "storage_locations";
    public const string StorageLocationPrimaryKey = "PK_wms_storage_locations";

    public const string StorageLocationWarehouseForeignKey = "FK_wms_storage_locations_warehouses_warehouse_id";
    public const string StorageLocationZoneForeignKey = "FK_wms_storage_locations_zones_zone_id";
    public const string StorageLocationTypeForeignKey = "FK_wms_storage_locations_storage_location_types_storage_location_type_id";
    public const string StorageLocationStatusForeignKey = "FK_wms_storage_locations_storage_location_statuses_storage_location_status_id";

    public const string StorageLocationWarehouseIdIndex = "IX_wms_storage_locations_warehouse_id";
    public const string StorageLocationZoneIdIndex = "IX_wms_storage_locations_zone_id";
    public const string StorageLocationTypeIdIndex = "IX_wms_storage_locations_storage_location_type_id";
    public const string StorageLocationStatusIdIndex = "IX_wms_storage_locations_storage_location_status_id";
    public const string StorageLocationWarehouseIdCodeUniqueIndex = "UX_wms_storage_locations_warehouse_id_code";

    public const string StorageLocationTypesTable = "storage_location_types";
    public const string StorageLocationTypePrimaryKey = "PK_wms_storage_location_types";
    public const string StorageLocationTypeCodeUniqueIndex = "UX_wms_storage_location_types_code";

    public const string StorageLocationStatusesTable = "storage_location_statuses";
    public const string StorageLocationStatusPrimaryKey = "PK_wms_storage_location_statuses";
    public const string StorageLocationStatusCodeUniqueIndex = "UX_wms_storage_location_statuses_code";

    public const string StockKeepingUnitsTable = "stock_keeping_units";
    public const string StockKeepingUnitPrimaryKey = "PK_wms_stock_keeping_units";
    public const string StockKeepingUnitCodeUniqueIndex = "UX_wms_stock_keeping_units_code";

    public const string UnitsOfMeasureTable = "units_of_measure";
    public const string UnitOfMeasurePrimaryKey = "PK_wms_units_of_measure";
    public const string UnitOfMeasureCodeUniqueIndex = "UX_wms_units_of_measure_code";
}
