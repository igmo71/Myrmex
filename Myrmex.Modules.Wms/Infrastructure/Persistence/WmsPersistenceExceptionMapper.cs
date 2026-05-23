using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Results;

namespace Myrmex.Modules.Wms.Infrastructure.Persistence;

internal static class WmsPersistenceExceptionMapper
{
    public static ServiceError? TryMap(DbUpdateException exception)
    {
        if (exception.IsUniqueConstraintViolation(WmsDatabaseNames.WarehouseCodeUniqueIndex))
        {
            return ServiceErrors.Conflict("Warehouse.CodeAlreadyExists", "Warehouse with the same code already exists.", "code");
        }

        return null;
    }
}