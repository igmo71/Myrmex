using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Myrmex.Modules.Wms.Infrastructure.Persistence;

namespace Myrmex.Tests.Wms.Topology.Testing;

internal sealed class TestWmsDbContext : IAsyncDisposable
{
    private const string ConnectionStringEnvironmentVariable =
        "MYRMEX_WMS_TEST_CONNECTION";

    private static readonly SemaphoreSlim DatabaseGate = new(1, 1);
    private static bool _migrationStateChecked;

    private readonly IDbContextTransaction _transaction;
    private bool _disposed;

    private TestWmsDbContext(
        WmsDbContext dbContext,
        IDbContextTransaction transaction)
    {
        DbContext = dbContext;
        _transaction = transaction;
    }

    public WmsDbContext DbContext { get; }

    public WmsDbContext CreateDbContext()
    {
        DbContextOptions<WmsDbContext> options =
            new DbContextOptionsBuilder<WmsDbContext>()
                .UseSqlServer(DbContext.Database.GetDbConnection())
                .Options;

        WmsDbContext dbContext = new(options);
        dbContext.Database.UseTransaction(_transaction.GetDbTransaction());

        return dbContext;
    }

    public static async Task<TestWmsDbContext> CreateAsync()
    {
        await DatabaseGate.WaitAsync(TestContext.Current.CancellationToken);

        try
        {
            string connectionString =
                Environment.GetEnvironmentVariable(
                    ConnectionStringEnvironmentVariable)
                ?? throw new InvalidOperationException(
                    $"Environment variable " +
                    $"'{ConnectionStringEnvironmentVariable}' is not configured.");

            ValidateTestDatabase(connectionString);

            DbContextOptions<WmsDbContext> options =
                new DbContextOptionsBuilder<WmsDbContext>()
                    .UseSqlServer(connectionString)
                    .Options;

            WmsDbContext dbContext = new(options);

            await EnsureMigrationStateAsync(dbContext);

            IDbContextTransaction transaction =
                await dbContext.Database.BeginTransactionAsync(
                    TestContext.Current.CancellationToken);

            return new TestWmsDbContext(dbContext, transaction);
        }
        catch
        {
            DatabaseGate.Release();
            throw;
        }
    }

    private static async Task EnsureMigrationStateAsync(
        WmsDbContext dbContext)
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
                "The WMS test database has pending migrations: " +
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
                "WMS database tests require a dedicated database " +
                "whose name ends with '_test'.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            await DbContext.DisposeAsync();
        }
        finally
        {
            _disposed = true;
            DatabaseGate.Release();
        }
    }
}
