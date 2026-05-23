using Myrmex.Core.Domain.Validation;

namespace Myrmex.Modules.Wms.Topology.Domain.StorageLocations;

internal static class StorageLocationValidationErrors
{
    public static DomainValidationFailure WarehouseIdRequired =>
        new("StorageLocation.WarehouseIdRequired", "Warehouse id is required.", "warehouseId");

    public static DomainValidationFailure ZoneIdRequired =>
        new("StorageLocation.ZoneIdRequired", "Zone id is required.", "zoneId");

    public static DomainValidationFailure TypeIdRequired =>
        new("StorageLocation.TypeIdRequired", "Storage location type id is required.", "storageLocationTypeId");

    public static DomainValidationFailure StatusIdRequired =>
        new("StorageLocation.StatusIdRequired", "Storage location status id is required.", "storageLocationStatusId");

    public static DomainValidationFailure CodeRequired =>
        new("StorageLocation.CodeRequired", "Storage location code is required.", "code");

    public static DomainValidationFailure CodeTooLong(int maxLength) =>
        new("StorageLocation.CodeTooLong", $"Storage location code must not exceed {maxLength} characters.", "code");

    public static DomainValidationFailure NameRequired =>
        new("StorageLocation.NameRequired", "Storage location name is required.", "name");

    public static DomainValidationFailure NameTooLong(int maxLength) =>
        new("StorageLocation.NameTooLong", $"Storage location name must not exceed {maxLength} characters.", "name");

    public static DomainValidationFailure DescriptionTooLong(int maxLength) =>
        new("StorageLocation.DescriptionTooLong", $"Storage location description must not exceed {maxLength} characters.", "description");
}