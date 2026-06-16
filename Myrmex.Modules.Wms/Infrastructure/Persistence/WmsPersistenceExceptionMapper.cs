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
            return ServiceError.Conflict<Warehouse>("Code already exists", "Code");
        }
        if (exception.IsUniqueConstraintViolation(WmsDatabaseNames.ZoneWarehouseIdCodeUniqueIndex))
        {
            return ServiceError.Conflict<Zone>("Code already exists", "Code");
        }
        if (exception.IsUniqueConstraintViolation(WmsDatabaseNames.StorageLocationWarehouseIdCodeUniqueIndex))
        {
            return ServiceError.Conflict<StorageLocation>("Code already exists", "Code");
        }
        if (exception.IsUniqueConstraintViolation(WmsDatabaseNames.StorageLocationTypeCodeUniqueIndex))
        {
            return ServiceError.Conflict<StorageLocationType>("Code already exists", "Code");
        }
        if (exception.IsUniqueConstraintViolation(WmsDatabaseNames.StorageLocationStatusCodeUniqueIndex))
        {
            return ServiceError.Conflict<StorageLocationStatus>("Code already exists", "Code");
        }
        if (exception.IsUniqueConstraintViolation(WmsDatabaseNames.StockKeepingUnitCodeUniqueIndex))
        {
            return ServiceError.Conflict<StockKeepingUnit>("Code already exists", "Code");
        }
        if (exception.IsUniqueConstraintViolation(WmsDatabaseNames.UnitOfMeasureCodeUniqueIndex))
        {
            return ServiceError.Conflict<UnitOfMeasure>("Code already exists", "Code");
        }
        if (exception.IsUniqueConstraintViolation(WmsDatabaseNames.SkuBarcodeValueUniqueIndex))
        {
            return ServiceError.Conflict<SkuBarcode>("Value already exists", "Value");
        }
        if (exception.IsUniqueConstraintViolation(WmsDatabaseNames.InventoryBalanceStockKeepingUnitIdStorageLocationIdUniqueIndex))
        {
            return ServiceError.Conflict<InventoryBalance>("StockKeepingUnitId, StorageLocationId already exists", "StockKeepingUnitId, StorageLocationId");
        }
        return null;
    }
}
