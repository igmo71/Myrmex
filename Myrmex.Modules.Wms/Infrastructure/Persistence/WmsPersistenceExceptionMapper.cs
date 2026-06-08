using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence.SqlServer;

namespace Myrmex.Modules.Wms.Infrastructure.Persistence;

internal static class WmsPersistenceExceptionMapper
{
    public static ServiceError? TryMap(DbUpdateException exception)
    {
        if (exception.IsUniqueConstraintViolation(WmsDatabaseNames.WarehouseCodeUniqueIndex))
        {
            return WmsErrors.Warehouse.CodeAlreadyExists;
        }
        if (exception.IsUniqueConstraintViolation(WmsDatabaseNames.ZoneWarehouseIdCodeUniqueIndex))
        {
            return WmsErrors.Zone.CodeAlreadyExists;
        }
        if (exception.IsUniqueConstraintViolation(WmsDatabaseNames.StorageLocationWarehouseIdCodeUniqueIndex))
        {
            return WmsErrors.StorageLocation.CodeAlreadyExists;
        }
        if (exception.IsUniqueConstraintViolation(WmsDatabaseNames.StorageLocationTypeCodeUniqueIndex))
        {
            return WmsErrors.StorageLocationType.CodeAlreadyExists;
        }
        if (exception.IsUniqueConstraintViolation(WmsDatabaseNames.StorageLocationStatusCodeUniqueIndex))
        {
            return WmsErrors.StorageLocationStatus.CodeAlreadyExists;
        }
        if (exception.IsUniqueConstraintViolation(WmsDatabaseNames.StockKeepingUnitCodeUniqueIndex))
        {
            return WmsErrors.StockKeepingUnit.CodeAlreadyExists;
        }
        return null;
    }
}
