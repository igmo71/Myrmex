using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Myrmex.Modules.Wms.Infrastructure.Persistence;

namespace Myrmex.Tests.Wms.Topology.Testing;

internal sealed class TestWmsDbContext : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private TestWmsDbContext(
        SqliteConnection connection,
        WmsDbContext dbContext)
    {
        _connection = connection;
        DbContext = dbContext;
    }

    public WmsDbContext DbContext { get; }

    public static async Task<TestWmsDbContext> CreateAsync()
    {
        SqliteConnection connection = new("Data Source=:memory:");

        await connection.OpenAsync(TestContext.Current.CancellationToken);

        DbContextOptions<WmsDbContext> options = new DbContextOptionsBuilder<WmsDbContext>()
            .UseSqlite(connection)
            .Options;

        WmsDbContext dbContext = new(options);

        await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        return new TestWmsDbContext(connection, dbContext);
    }

    public async ValueTask DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }
}