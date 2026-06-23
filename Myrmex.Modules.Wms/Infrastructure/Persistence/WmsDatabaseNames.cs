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
    public const string StockKeepingUnitBaseUnitOfMeasureForeignKey = "FK_wms_stock_keeping_units_units_of_measure_base_unit_of_measure_id";
    public const string StockKeepingUnitBaseUnitOfMeasureIdIndex = "IX_wms_stock_keeping_units_base_unit_of_measure_id";

    public const string UnitsOfMeasureTable = "units_of_measure";
    public const string UnitOfMeasurePrimaryKey = "PK_wms_units_of_measure";
    public const string UnitOfMeasureCodeUniqueIndex = "UX_wms_units_of_measure_code";

    public const string SkuBarcodesTable = "sku_barcodes";
    public const string SkuBarcodePrimaryKey = "PK_wms_sku_barcodes";
    public const string SkuBarcodeStockKeepingUnitForeignKey = "FK_wms_sku_barcodes_stock_keeping_units_stock_keeping_unit_id";
    public const string SkuBarcodeValueUniqueIndex = "UX_wms_sku_barcodes_value";
    public const string SkuBarcodeStockKeepingUnitIdIndex = "IX_wms_sku_barcodes_stock_keeping_unit_id";

    public const string InventoryBalancesTable = "inventory_balances";
    public const string InventoryBalancePrimaryKey = "PK_wms_inventory_balances";
    public const string InventoryBalanceStockKeepingUnitForeignKey = "FK_wms_inventory_balances_stock_keeping_units_stock_keeping_unit_id";
    public const string InventoryBalanceStorageLocationForeignKey = "FK_wms_inventory_balances_storage_locations_storage_location_id";
    public const string InventoryBalanceStockKeepingUnitIdStorageLocationIdUniqueIndex = "UX_wms_inventory_balances_stock_keeping_unit_id_storage_location_id";
    public const string InventoryBalanceStorageLocationIdIndex = "IX_wms_inventory_balances_storage_location_id";

    public const int InventoryTransactionReasonMaxLength = 500;
    public const int InventoryTransactionTypeMaxLength = 32;

    public const string InventoryTransactionsTable = "inventory_transactions";
    public const string InventoryTransactionPrimaryKey = "PK_wms_inventory_transactions";
    public const string InventoryTransactionOccurredAtUtcIndex = "IX_wms_inventory_transactions_occurred_at_utc";

    public const string InventoryLedgerEntriesTable = "inventory_ledger_entries";
    public const string InventoryLedgerEntryPrimaryKey = "PK_wms_inventory_ledger_entries";
    public const string InventoryLedgerEntryInventoryTransactionForeignKey = "FK_wms_inventory_ledger_entries_inventory_transactions_inventory_transaction_id";
    public const string InventoryLedgerEntryStockKeepingUnitForeignKey = "FK_wms_inventory_ledger_entries_stock_keeping_units_stock_keeping_unit_id";
    public const string InventoryLedgerEntryStorageLocationForeignKey = "FK_wms_inventory_ledger_entries_storage_locations_storage_location_id";
    public const string InventoryLedgerEntryStockKeepingUnitIdIndex = "IX_wms_inventory_ledger_entries_stock_keeping_unit_id";
    public const string InventoryLedgerEntryStorageLocationIdIndex = "IX_wms_inventory_ledger_entries_storage_location_id";

    public const int InventoryTransferCodeMaxLength = 64;
    public const int InventoryTransferStatusMaxLength = 32;

    public const string InventoryTransfersTable = "inventory_transfers";
    public const string InventoryTransferPrimaryKey = "PK_wms_inventory_transfers";
    public const string InventoryTransferSourceWarehouseForeignKey = "FK_wms_inventory_transfers_warehouses_source_warehouse_id";
    public const string InventoryTransferDestinationWarehouseForeignKey = "FK_wms_inventory_transfers_warehouses_destination_warehouse_id";
    public const string InventoryTransferTransitStorageLocationForeignKey = "FK_wms_inventory_transfers_storage_locations_transit_storage_location_id";
    public const string InventoryTransferCodeUniqueIndex = "UX_wms_inventory_transfers_code";
    public const string InventoryTransferSourceWarehouseIdIndex = "IX_wms_inventory_transfers_source_warehouse_id";
    public const string InventoryTransferDestinationWarehouseIdIndex = "IX_wms_inventory_transfers_destination_warehouse_id";
    public const string InventoryTransferTransitStorageLocationIdIndex = "IX_wms_inventory_transfers_transit_storage_location_id";
    public const string InventoryTransferStatusIndex = "IX_wms_inventory_transfers_status";

    public const string InventoryTransferLinesTable = "inventory_transfer_lines";
    public const string InventoryTransferLinePrimaryKey = "PK_wms_inventory_transfer_lines";
    public const string InventoryTransferLineInventoryTransferForeignKey = "FK_wms_inventory_transfer_lines_inventory_transfers_inventory_transfer_id";
    public const string InventoryTransferLineStockKeepingUnitForeignKey = "FK_wms_inventory_transfer_lines_stock_keeping_units_stock_keeping_unit_id";
    public const string InventoryTransferLineSourceStorageLocationForeignKey = "FK_wms_inventory_transfer_lines_storage_locations_source_storage_location_id";
    public const string InventoryTransferLineDestinationStorageLocationForeignKey = "FK_wms_inventory_transfer_lines_storage_locations_destination_storage_location_id";
    public const string InventoryTransferLineInventoryTransferIdIndex = "IX_wms_inventory_transfer_lines_inventory_transfer_id";
    public const string InventoryTransferLineStockKeepingUnitIdIndex = "IX_wms_inventory_transfer_lines_stock_keeping_unit_id";
    public const string InventoryTransferLineSourceStorageLocationIdIndex = "IX_wms_inventory_transfer_lines_source_storage_location_id";
    public const string InventoryTransferLineDestinationStorageLocationIdIndex = "IX_wms_inventory_transfer_lines_destination_storage_location_id";

    public const string InventoryTransferMovementsTable = "inventory_transfer_movements";
    public const string InventoryTransferMovementPrimaryKey = "PK_wms_inventory_transfer_movements";
    public const string InventoryTransferMovementInventoryTransferForeignKey = "FK_wms_inventory_transfer_movements_inventory_transfers_inventory_transfer_id";
    public const string InventoryTransferMovementInventoryTransferLineForeignKey = "FK_wms_inventory_transfer_movements_inventory_transfer_lines_inventory_transfer_line_id";
    public const string InventoryTransferMovementInventoryTransactionForeignKey = "FK_wms_inventory_transfer_movements_inventory_transactions_inventory_transaction_id";
    public const string InventoryTransferMovementStockKeepingUnitForeignKey = "FK_wms_inventory_transfer_movements_stock_keeping_units_stock_keeping_unit_id";
    public const string InventoryTransferMovementFromStorageLocationForeignKey = "FK_wms_inventory_transfer_movements_storage_locations_from_storage_location_id";
    public const string InventoryTransferMovementToStorageLocationForeignKey = "FK_wms_inventory_transfer_movements_storage_locations_to_storage_location_id";
    public const string InventoryTransferMovementInventoryTransferIdIndex = "IX_wms_inventory_transfer_movements_inventory_transfer_id";
    public const string InventoryTransferMovementInventoryTransferLineIdIndex = "IX_wms_inventory_transfer_movements_inventory_transfer_line_id";
    public const string InventoryTransferMovementInventoryTransactionIdIndex = "IX_wms_inventory_transfer_movements_inventory_transaction_id";
    public const string InventoryTransferMovementStockKeepingUnitIdIndex = "IX_wms_inventory_transfer_movements_stock_keeping_unit_id";
    public const string InventoryTransferMovementFromStorageLocationIdIndex = "IX_wms_inventory_transfer_movements_from_storage_location_id";
    public const string InventoryTransferMovementToStorageLocationIdIndex = "IX_wms_inventory_transfer_movements_to_storage_location_id";
}
