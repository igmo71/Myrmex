using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.SkuBarcodes;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Infrastructure.Persistence.SqlServer;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Modules.Wms.Topology.Domain.Zones;

namespace Myrmex.Modules.Wms.Infrastructure.Persistence;

internal static class WmsPersistenceExceptionMapper
{
    public static bool IsInventoryBalanceSkuLocationDuplicate(DbUpdateException exception)
    {
        return exception.IsUniqueConstraintViolation(
            WmsDatabaseNames.InventoryBalanceStockKeepingUnitIdStorageLocationIdUniqueIndex);
    }

    public static ServiceError? TryMap(DbUpdateException exception)
    {
        if (exception.IsUniqueConstraintViolation(WmsDatabaseNames.WarehouseCodeUniqueIndex))
        {
            return ServiceError.Conflict<Warehouse>("Code already exists", nameof(Warehouse.Code));
        }
        if (exception.IsUniqueConstraintViolation(WmsDatabaseNames.WarehouseExternalRefKeyUniqueIndex))
        {
            return ServiceError.Conflict<Warehouse>("External reference key already exists", nameof(Warehouse.ExternalRefKey));
        }
        if (exception.IsUniqueConstraintViolation(WmsDatabaseNames.ZoneWarehouseIdCodeUniqueIndex))
        {
            return ServiceError.Conflict<Zone>("Code already exists", nameof(Zone.Code));
        }
        if (exception.IsUniqueConstraintViolation(WmsDatabaseNames.StorageLocationWarehouseIdCodeUniqueIndex))
        {
            return ServiceError.Conflict<StorageLocation>("Code already exists", nameof(StorageLocation.Code));
        }
        if (exception.IsUniqueConstraintViolation(WmsDatabaseNames.StorageLocationTypeCodeUniqueIndex))
        {
            return ServiceError.Conflict<StorageLocationType>("Code already exists", nameof(StorageLocationType.Code));
        }
        if (exception.IsUniqueConstraintViolation(WmsDatabaseNames.StorageLocationStatusCodeUniqueIndex))
        {
            return ServiceError.Conflict<StorageLocationStatus>("Code already exists", nameof(StorageLocationStatus.Code));
        }
        if (exception.IsUniqueConstraintViolation(WmsDatabaseNames.StockKeepingUnitCodeUniqueIndex))
        {
            return ServiceError.Conflict<StockKeepingUnit>("Code already exists", nameof(StockKeepingUnit.Code));
        }
        if (exception.IsUniqueConstraintViolation(WmsDatabaseNames.StockKeepingUnitExternalRefKeyUniqueIndex))
        {
            return ServiceError.Conflict<StockKeepingUnit>("External reference key already exists", nameof(StockKeepingUnit.ExternalRefKey));
        }
        if (exception.IsUniqueConstraintViolation(WmsDatabaseNames.UnitOfMeasureCodeUniqueIndex))
        {
            return ServiceError.Conflict<UnitOfMeasure>("Code already exists", nameof(UnitOfMeasure.Code));
        }
        if (exception.IsUniqueConstraintViolation(WmsDatabaseNames.UnitOfMeasureExternalRefKeyUniqueIndex))
        {
            return ServiceError.Conflict<UnitOfMeasure>("External reference key already exists", nameof(UnitOfMeasure.ExternalRefKey));
        }
        if (exception.IsUniqueConstraintViolation(WmsDatabaseNames.SkuBarcodeValueUniqueIndex))
        {
            return ServiceError.Conflict<SkuBarcode>("Value already exists", nameof(SkuBarcode.Value));
        }
        if (IsInventoryBalanceSkuLocationDuplicate(exception))
        {
            return ServiceError.Conflict<InventoryBalance>("StockKeepingUnitId - StorageLocationId already exists",
                $"{nameof(InventoryBalance.StockKeepingUnitId)}-{nameof(InventoryBalance.StorageLocationId)}");
        }
        return null;
    }
}
