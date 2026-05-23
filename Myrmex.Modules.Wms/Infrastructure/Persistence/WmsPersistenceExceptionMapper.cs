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
            return ServiceErrors.Conflict(
                "Warehouse.CodeAlreadyExists", "Warehouse with the same code already exists.", "code");
        }
        if (exception.IsUniqueConstraintViolation(WmsDatabaseNames.ZoneWarehouseIdCodeUniqueIndex))
        {
            return ServiceErrors.Conflict(
                "Zone.CodeAlreadyExists", "Zone code already exists in this warehouse.", "code");
        }

        return null;
    }
}