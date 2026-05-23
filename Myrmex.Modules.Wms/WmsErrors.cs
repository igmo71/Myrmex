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
}