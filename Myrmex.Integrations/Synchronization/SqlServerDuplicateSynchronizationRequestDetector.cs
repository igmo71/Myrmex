using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Myrmex.Integrations.Synchronization;

internal sealed class SqlServerDuplicateSynchronizationRequestDetector
{
    private const int CannotInsertDuplicateKeyRow = 2601;
    private const int ViolationOfUniqueConstraint = 2627;

    public bool IsIdempotencyDuplicate(DbUpdateException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        SqlException? sqlException = FindSqlException(exception);
        return sqlException is not null &&
            HasDuplicateKeyError(sqlException) &&
            IdentifiesIdempotencyIndex(sqlException);
    }

    private static SqlException? FindSqlException(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException sqlException)
            {
                return sqlException;
            }
        }

        return null;
    }

    private static bool HasDuplicateKeyError(SqlException exception)
    {
        foreach (SqlError error in exception.Errors)
        {
            if (error.Number is CannotInsertDuplicateKeyRow or ViolationOfUniqueConstraint)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IdentifiesIdempotencyIndex(SqlException exception) =>
        exception.Message.Contains(
            IntegrationSynchronizationDatabaseNames
                .SynchronizationRequestIdempotencyUniqueIndex,
            StringComparison.Ordinal);
}
