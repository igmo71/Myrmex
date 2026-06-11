using Myrmex.Core.Results;

namespace Myrmex.Modules.Wms;

internal static class WmsErrors
{
    internal static class Warehouse
    {
        public static ServiceError NotFound => ServiceErrors.NotFound("Warehouse.NotFound", "Warehouse was not found.");
        public static ServiceError CodeAlreadyExists => ServiceErrors.Conflict("Warehouse.CodeAlreadyExists", "Warehouse with the same code already exists.", "code");
        public static ServiceError CreateFailed => ServiceErrors.Failure("Warehouse.CreateFailed", "Warehouse creation failed unexpectedly.");
        public static ServiceError NotFoundById => ServiceErrors.NotFound("Warehouse.NotFound", "Warehouse was not found.", "warehouseId");
    }

    internal static class Zone
    {
        public static ServiceError NotFound => ServiceErrors.NotFound("Zone.NotFound", "Zone was not found.");
        public static ServiceError CodeAlreadyExists => ServiceErrors.Conflict("Zone.CodeAlreadyExists", "Zone with the same code already exists in this warehouse.", "code");
        public static ServiceError CreateFailed => ServiceErrors.Failure("Zone.CreateFailed", "Zone creation failed unexpectedly.");
        public static ServiceError NotFoundById => ServiceErrors.NotFound("Zone.NotFound", "Zone was not found.", "zoneId");
    }

    internal static class StorageLocation
    {
        public static ServiceError NotFound => ServiceErrors.NotFound("StorageLocation.NotFound", "Storage location was not found.");
        public static ServiceError CodeAlreadyExists => ServiceErrors.Conflict("StorageLocation.CodeAlreadyExists", "Storage location with the same code already exists in this warehouse.", "code");
        public static ServiceError CreateFailed => ServiceErrors.Failure("StorageLocation.CreateFailed", "Storage location creation failed unexpectedly.");
        public static ServiceError ZoneWarehouseMismatch => ServiceErrors.Failure("StorageLocation.ZoneWarehouseMismatch", "Zone does not belong to the specified warehouse.");
        public static ServiceError TypeNotFound => ServiceErrors.NotFound("StorageLocationType.NotFound", "Storage location type was not found.", "storageLocationTypeId");
        public static ServiceError StatusNotFound => ServiceErrors.NotFound("StorageLocationStatus.NotFound", "Storage location status was not found.", "storageLocationStatusId");
    }

    internal static class StorageLocationType
    {
        public static ServiceError NotFound => ServiceErrors.NotFound("StorageLocationType.NotFound", "Storage location type was not found.");
        public static ServiceError CodeAlreadyExists => ServiceErrors.Conflict("StorageLocationType.CodeAlreadyExists", "Storage location type with the same code already exists.", "code");
        public static ServiceError CreateFailed => ServiceErrors.Failure("StorageLocationType.CreateFailed", "Storage location type creation failed unexpectedly.");
    }

    internal static class StorageLocationStatus
    {
        public static ServiceError NotFound => ServiceErrors.NotFound("StorageLocationStatus.NotFound", "Storage location status was not found.");
        public static ServiceError CodeAlreadyExists => ServiceErrors.Conflict("StorageLocationStatus.CodeAlreadyExists", "Storage location status with the same code already exists.", "code");
        public static ServiceError CreateFailed => ServiceErrors.Failure("StorageLocationStatus.CreateFailed", "Storage location status creation failed unexpectedly.");
    }

    internal static class StockKeepingUnit
    {
        public static ServiceError NotFound => ServiceErrors.NotFound("StockKeepingUnit.NotFound", "Stock keeping unit was not found.");
        public static ServiceError NotFoundById => ServiceErrors.NotFound("StockKeepingUnit.NotFound", "Stock keeping unit was not found.", "stockKeepingUnitId");
        public static ServiceError CodeAlreadyExists => ServiceErrors.Conflict("StockKeepingUnit.CodeAlreadyExists", "Stock keeping unit with the same code already exists.", "code");
        public static ServiceError ValidationFailed => ServiceErrors.Failure("StockKeepingUnit.ValidationFailed", "Stock keeping unit validation failed.");
        public static ServiceError CreateFailed => ServiceErrors.Failure("StockKeepingUnit.CreateFailed", "Stock keeping unit creation failed unexpectedly.");
        public static ServiceError UpdateFailed => ServiceErrors.Failure("StockKeepingUnit.UpdateFailed", "Stock keeping unit update failed unexpectedly.");
        public static ServiceError DeactivateFailed => ServiceErrors.Failure("StockKeepingUnit.DeactivateFailed", "Stock keeping unit deactivation failed unexpectedly.");
        public static ServiceError ReactivateFailed => ServiceErrors.Failure("StockKeepingUnit.ReactivateFailed", "Stock keeping unit reactivation failed unexpectedly.");
        public static ServiceError BaseUnitOfMeasureNotFound => ServiceErrors.NotFound("UnitOfMeasure.NotFound", "Base unit of measure was not found.", "baseUnitOfMeasureId");
        public static ServiceError BaseUnitOfMeasureInactive => ServiceErrors.Validation("StockKeepingUnit.BaseUnitOfMeasureInactive", "SKU base unit of measure must be active.", "baseUnitOfMeasureId");
        public static ServiceError BaseUnitOfMeasureRequired => ServiceErrors.Validation("StockKeepingUnit.BaseUnitOfMeasureRequired", "SKU base unit of measure is required.", "baseUnitOfMeasureId");
    }

    internal static class UnitOfMeasure
    {
        public static ServiceError NotFound => ServiceErrors.NotFound("UnitOfMeasure.NotFound", "Unit of measure was not found.");
        public static ServiceError NotFoundById => ServiceErrors.NotFound("UnitOfMeasure.NotFound", "Unit of measure was not found.", "unitOfMeasureId");
        public static ServiceError CodeAlreadyExists => ServiceErrors.Conflict("UnitOfMeasure.CodeAlreadyExists", "Unit of measure with the same code already exists.", "code");
        public static ServiceError CreateFailed => ServiceErrors.Failure("UnitOfMeasure.CreateFailed", "Unit of measure creation failed unexpectedly.");
    }

    internal static class SkuBarcode
    {
        public static ServiceError NotFound => ServiceErrors.NotFound("SkuBarcode.NotFound", "SKU barcode was not found.");
        public static ServiceError NotFoundById => ServiceErrors.NotFound("SkuBarcode.NotFound", "SKU barcode was not found.", "skuBarcodeId");
        public static ServiceError ValueAlreadyExists => ServiceErrors.Conflict("SkuBarcode.ValueAlreadyExists", "SKU barcode with the same value already exists.", "value");
        public static ServiceError CreateFailed => ServiceErrors.Failure("SkuBarcode.CreateFailed", "SKU barcode creation failed unexpectedly.");
        public static ServiceError UpdateFailed => ServiceErrors.Failure("SkuBarcode.UpdateFailed", "SKU barcode update failed unexpectedly.");
        public static ServiceError DeactivateFailed => ServiceErrors.Failure("SkuBarcode.DeactivateFailed", "SKU barcode deactivation failed unexpectedly.");
        public static ServiceError ReactivateFailed => ServiceErrors.Failure("SkuBarcode.ReactivateFailed", "SKU barcode reactivation failed unexpectedly.");
        public static ServiceError UnsupportedPrimaryChange => ServiceErrors.Conflict("SkuBarcode.UnsupportedPrimaryChange", "Inactive SKU barcodes cannot be made primary.", "isPrimary");
    }

    internal static class InventoryBalance
    {
        public static ServiceError NotFound => ServiceErrors.NotFound("InventoryBalance.NotFound", "Inventory balance was not found.");
        public static ServiceError DuplicateStockKeepingUnitStorageLocation => ServiceErrors.Conflict("InventoryBalance.DuplicateStockKeepingUnitStorageLocation", "Inventory balance for the same SKU and storage location already exists.", "stockKeepingUnitId");
        public static ServiceError CreateFailed => ServiceErrors.Failure("InventoryBalance.CreateFailed", "Inventory balance creation failed unexpectedly.");
        public static ServiceError UpdateFailed => ServiceErrors.Failure("InventoryBalance.UpdateFailed", "Inventory balance update failed unexpectedly.");
        public static ServiceError StockKeepingUnitNotFound => ServiceErrors.NotFound("InventoryBalance.StockKeepingUnitNotFound", "SKU was not found for inventory balance creation.", "stockKeepingUnitId");
        public static ServiceError InvalidStockKeepingUnit => ServiceErrors.Validation("InventoryBalance.InvalidStockKeepingUnit", "SKU must be active and have an active base unit of measure.", "stockKeepingUnitId");
        public static ServiceError StorageLocationNotFound => ServiceErrors.NotFound("InventoryBalance.StorageLocationNotFound", "Storage location was not found for inventory balance creation.", "storageLocationId");
        public static ServiceError InvalidStorageLocation => ServiceErrors.Validation("InventoryBalance.InvalidStorageLocation", "Storage location must be active for inventory balance creation.", "storageLocationId");
        public static ServiceError InactiveStorageLocationType => ServiceErrors.Validation("InventoryBalance.InactiveStorageLocationType", "Storage location type must be active for inventory balance creation.", "storageLocationTypeId");
        public static ServiceError InactiveStorageLocationStatus => ServiceErrors.Validation("InventoryBalance.InactiveStorageLocationStatus", "Storage location status must be active for inventory balance creation.", "storageLocationStatusId");
    }
}
