using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Myrmex.Integrations.Persistence.Configurations;
using Myrmex.Integrations.Persistence.SqlServer;
using System.Reflection;

namespace Myrmex.Tests.Integrations.OneC.Synchronization;

public sealed class SqlServerDuplicateSynchronizationRequestDetectorTests
{
    [Theory]
    [InlineData(2601)]
    [InlineData(2627)]
    public void IsIdempotencyDuplicate_WhenExpectedSqlDuplicateNamesIndex_ReturnsTrue(
        int errorNumber)
    {
        DbUpdateException exception = CreateDbUpdateException(
            errorNumber,
            "Cannot insert duplicate key row in object " +
            "'integration.synchronization_requests' with unique index '" +
            SynchronizationDatabaseNames
                .SynchronizationRequestIdempotencyUniqueIndex +
            "'.");

        Assert.True(
            new SqlServerDuplicateSynchronizationRequestDetector()
                .IsIdempotencyDuplicate(exception));
    }

    [Fact]
    public void IsIdempotencyDuplicate_WhenDuplicateNamesAnotherIndex_ReturnsFalse()
    {
        DbUpdateException exception = CreateDbUpdateException(
            2601,
            "Cannot insert duplicate key row in object " +
            "'integration.synchronization_requests' with unique index " +
            "'UX_other_index'.");

        Assert.False(
            new SqlServerDuplicateSynchronizationRequestDetector()
                .IsIdempotencyDuplicate(exception));
    }

    [Fact]
    public void IsIdempotencyDuplicate_WhenSqlErrorIsNotDuplicate_ReturnsFalse()
    {
        DbUpdateException exception = CreateDbUpdateException(
            547,
            SynchronizationDatabaseNames
                .SynchronizationRequestIdempotencyUniqueIndex);

        Assert.False(
            new SqlServerDuplicateSynchronizationRequestDetector()
                .IsIdempotencyDuplicate(exception));
    }

    [Fact]
    public void IsIdempotencyDuplicate_WhenNoSqlException_ReturnsFalse()
    {
        DbUpdateException exception = new(
            "Persistence failed.",
            new InvalidOperationException("not sql"));

        Assert.False(
            new SqlServerDuplicateSynchronizationRequestDetector()
                .IsIdempotencyDuplicate(exception));
    }

    private static DbUpdateException CreateDbUpdateException(
        int errorNumber,
        string message) =>
        new(
            "Persistence failed.",
            CreateSqlException(errorNumber, message));

    private static SqlException CreateSqlException(
        int errorNumber,
        string message)
    {
        SqlErrorCollection errors =
            (SqlErrorCollection)Activator.CreateInstance(
                typeof(SqlErrorCollection),
                nonPublic: true)!;
        SqlError error = CreateSqlError(errorNumber, message);

        typeof(SqlErrorCollection)
            .GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(errors, [error]);

        MethodInfo? twoArgumentFactory = typeof(SqlException).GetMethod(
            "CreateException",
            BindingFlags.Static | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(SqlErrorCollection), typeof(string)],
            modifiers: null);
        if (twoArgumentFactory is not null)
        {
            return (SqlException)twoArgumentFactory.Invoke(
                null,
                [errors, "16.0.0"])!;
        }

        MethodInfo threeArgumentFactory = typeof(SqlException).GetMethod(
            "CreateException",
            BindingFlags.Static | BindingFlags.NonPublic,
            binder: null,
            types:
            [
                typeof(SqlErrorCollection),
                typeof(string),
                typeof(Guid)
            ],
            modifiers: null)!;

        return (SqlException)threeArgumentFactory.Invoke(
            null,
            [errors, "16.0.0", Guid.NewGuid()])!;
    }

    private static SqlError CreateSqlError(
        int errorNumber,
        string message)
    {
        ConstructorInfo constructor = typeof(SqlError)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .OrderByDescending(candidate => candidate.GetParameters().Length)
            .First();
        object?[] arguments = constructor.GetParameters()
            .Select(parameter => CreateSqlErrorArgument(
                parameter,
                errorNumber,
                message))
            .ToArray();

        return (SqlError)constructor.Invoke(arguments);
    }

    private static object? CreateSqlErrorArgument(
        ParameterInfo parameter,
        int errorNumber,
        string message)
    {
        if (parameter.ParameterType == typeof(int))
        {
            if (parameter.Name?.Contains(
                "line",
                StringComparison.OrdinalIgnoreCase) == true)
            {
                return 1;
            }

            return parameter.Name?.Contains(
                "number",
                StringComparison.OrdinalIgnoreCase) == true
                ? errorNumber
                : 1;
        }

        if (parameter.ParameterType == typeof(byte))
        {
            return parameter.Name?.Contains(
                "class",
                StringComparison.OrdinalIgnoreCase) == true
                ? (byte)14
                : (byte)0;
        }

        if (parameter.ParameterType == typeof(string))
        {
            if (parameter.Name?.Contains(
                "message",
                StringComparison.OrdinalIgnoreCase) == true)
            {
                return message;
            }

            if (parameter.Name?.Contains(
                "server",
                StringComparison.OrdinalIgnoreCase) == true)
            {
                return "localhost";
            }

            return string.Empty;
        }

        if (parameter.ParameterType == typeof(uint))
        {
            return 0U;
        }

        if (parameter.ParameterType == typeof(Exception))
        {
            return null;
        }

        throw new NotSupportedException(
            $"Unsupported SqlError constructor parameter type " +
            $"{parameter.ParameterType}.");
    }
}
