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
    public static ServiceError? TryMap(DbUpdateException exception)
    {
        if (exception.IsUniqueConstraintViolation(WmsDatabaseNames.WarehouseCodeUniqueIndex))
        {
            return ServiceError.Conflict<Warehouse>("code", "Already Exists");
        }
        if (exception.IsUniqueConstraintViolation(WmsDatabaseNames.ZoneWarehouseIdCodeUniqueIndex))
        {
            return ServiceError.Conflict<Zone>("code", "Already Exists");
        }
        if (exception.IsUniqueConstraintViolation(WmsDatabaseNames.StorageLocationWarehouseIdCodeUniqueIndex))
        {
            return ServiceError.Conflict<StorageLocation>("code", "Already Exists");
        }
        if (exception.IsUniqueConstraintViolation(WmsDatabaseNames.StorageLocationTypeCodeUniqueIndex))
        {
            return ServiceError.Conflict<StorageLocationType>("code", "Already Exists");
        }
        if (exception.IsUniqueConstraintViolation(WmsDatabaseNames.StorageLocationStatusCodeUniqueIndex))
        {
            return ServiceError.Conflict<StorageLocationStatus>("code", "Already Exists");
        }
        if (exception.IsUniqueConstraintViolation(WmsDatabaseNames.StockKeepingUnitCodeUniqueIndex))
        {
            return ServiceError.Conflict<StockKeepingUnit>("code", "Already Exists");
        }
        if (exception.IsUniqueConstraintViolation(WmsDatabaseNames.UnitOfMeasureCodeUniqueIndex))
        {
            return ServiceError.Conflict<UnitOfMeasure>("code", "Already Exists");
        }
        if (exception.IsUniqueConstraintViolation(WmsDatabaseNames.SkuBarcodeValueUniqueIndex))
        {
            return ServiceError.Conflict<SkuBarcode>("value", "Already Exists");
        }
        if (exception.IsUniqueConstraintViolation(WmsDatabaseNames.InventoryBalanceStockKeepingUnitIdStorageLocationIdUniqueIndex))
        {
            return ServiceError.Conflict<InventoryBalance>("stockKeepingUnitId, storageLocationId", "Already Exists");
        }
        return null;
    }
}
