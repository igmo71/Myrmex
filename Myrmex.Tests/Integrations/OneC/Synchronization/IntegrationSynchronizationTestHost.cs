using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Myrmex.Integrations.Persistence;
using Myrmex.Integrations.Persistence.Configurations;

namespace Myrmex.Tests.Integrations.OneC.Synchronization;

internal static class IntegrationSynchronizationTestHost
{
    public static IntegrationDbContext CreateModelDbContext()
    {
        DbContextOptions<IntegrationDbContext> options =
            new DbContextOptionsBuilder<IntegrationDbContext>()
                .UseSqlServer(
                    "Server=localhost;Database=MyrmexIntegrationModelTests;" +
                    "Trusted_Connection=True;TrustServerCertificate=True")
                .Options;

        return new IntegrationDbContext(options);
    }
}

internal sealed class IntegrationSynchronizationSqlTestHost : IAsyncDisposable
{
    private const string ConnectionStringEnvironmentVariable =
        "MYRMEX_INTEGRATION_TEST_CONNECTION";

    private static readonly SemaphoreSlim DatabaseGate = new(1, 1);
    private static bool _migrationStateChecked;

    private readonly string _connectionString;
    private bool _disposed;

    private IntegrationSynchronizationSqlTestHost(string connectionString)
    {
        _connectionString = connectionString;
    }

    public string ConnectionString => _connectionString;

    public IntegrationDbContext CreateDbContext()
    {
        DbContextOptions<IntegrationDbContext> options =
            new DbContextOptionsBuilder<IntegrationDbContext>()
                .UseSqlServer(_connectionString)
                .Options;

        return new IntegrationDbContext(options);
    }

    public static async Task<IntegrationSynchronizationSqlTestHost> CreateAsync()
    {
        await DatabaseGate.WaitAsync(TestContext.Current.CancellationToken);

        try
        {
            string connectionString = GetConnectionString();

            ValidateTestDatabase(connectionString);

            IntegrationSynchronizationSqlTestHost host = new(connectionString);
            await using IntegrationDbContext dbContext = host.CreateDbContext();
            await EnsureMigrationStateAsync(dbContext);
            await host.ClearSynchronizationRequestsAsync();

            return host;
        }
        catch
        {
            DatabaseGate.Release();
            throw;
        }
    }

    public async Task ClearSynchronizationRequestsAsync()
    {
        await using IntegrationDbContext dbContext = CreateDbContext();
        await dbContext.Database.ExecuteSqlRawAsync(
            $"DELETE FROM [{SynchronizationDatabaseNames.Schema}]." +
            $"[{SynchronizationDatabaseNames.SynchronizationRequestsTable}]",
            TestContext.Current.CancellationToken);
    }

    private static async Task EnsureMigrationStateAsync(
        IntegrationDbContext dbContext)
    {
        if (_migrationStateChecked)
        {
            return;
        }

        string[] pendingMigrations =
            (await dbContext.Database.GetPendingMigrationsAsync(
                TestContext.Current.CancellationToken))
            .ToArray();

        if (pendingMigrations.Length > 0)
        {
            throw new InvalidOperationException(
                "The integration test database has pending migrations: " +
                string.Join(", ", pendingMigrations));
        }

        _migrationStateChecked = true;
    }

    private static void ValidateTestDatabase(string connectionString)
    {
        SqlConnectionStringBuilder builder = new(connectionString);

        if (string.IsNullOrWhiteSpace(builder.InitialCatalog) ||
            !builder.InitialCatalog.EndsWith(
                "_test",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Integration synchronization SQL tests require a dedicated " +
                "database whose name ends with '_test'.");
        }
    }

    private static string GetConnectionString()
    {
        string? environmentValue =
            Environment.GetEnvironmentVariable(
                ConnectionStringEnvironmentVariable);

        if (!string.IsNullOrWhiteSpace(environmentValue))
        {
            return environmentValue;
        }

        IConfiguration configuration = new ConfigurationBuilder()
            .AddUserSecrets<IntegrationSynchronizationSqlTestHost>()
            .Build();

        return configuration.GetConnectionString(
                "MyrmexIntegrationTestDatabase")
            ?? throw new InvalidOperationException(
                "Integration test database connection string is not configured.");
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            await ClearSynchronizationRequestsAsync();
        }
        finally
        {
            _disposed = true;
            DatabaseGate.Release();
        }
    }
}
