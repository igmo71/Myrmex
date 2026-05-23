using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Myrmex.Modules.Wms.Infrastructure.Persistence;

internal static class SqlServerExceptionExtensions
{
    public static bool IsUniqueConstraintViolation(
        this DbUpdateException exception,
        string constraintOrIndexName)
    {
        return exception.InnerException is SqlException sqlException
            && sqlException.Number is 2601 or 2627
            && sqlException.Message.Contains(constraintOrIndexName, StringComparison.OrdinalIgnoreCase);
    }
}