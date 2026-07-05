using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Myrmex.AppDispatching;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms;
using Myrmex.Modules.Wms.DemoData.Features;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Shared.Wms.DemoData;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.DemoData.Testing;

internal static class DemoDataTestHost
{
    public const string ActorId = "demo-test-operator";

}

internal sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}

internal sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
{
    public string EnvironmentName { get; set; } = environmentName;
    public string ApplicationName { get; set; } = "Myrmex.Tests";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

internal sealed class DemoDataServiceFixture : IAsyncDisposable
{
    private readonly TestWmsDbContext _database;
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;

    private DemoDataServiceFixture(
        TestWmsDbContext database,
        ServiceProvider provider,
        IServiceScope scope)
    {
        _database = database;
        _provider = provider;
        _scope = scope;
    }

    public WmsDbContext DbContext => _database.DbContext;

    public static async Task<DemoDataServiceFixture> CreateAsync(
        IWmsDemoDataStageHook? stageHook = null)
    {
        TestWmsDbContext database = await TestWmsDbContext.CreateAsync();
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton(database.DbContext);
        services.AddSingleton<TimeProvider>(new FixedTimeProvider(
            DateTimeOffset.Parse("2026-07-04T09:00:00Z")));
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment(Environments.Development));
        services.AddSingleton<IWmsDemoDataStageHook>(
            stageHook ?? new NoOpWmsDemoDataStageHook());
        services.AddSingleton<WmsDemoDataOperationGate>();
        services.AddScoped<WmsDemoDataSeeder>();
        services.AddScoped<WmsDemoDataClearService>();
        services.AddMyrmexAppDispatching(typeof(WmsModule).Assembly);
        ServiceProvider provider = services.BuildServiceProvider();
        IServiceScope scope = provider.CreateScope();
        return new DemoDataServiceFixture(database, provider, scope);
    }

    public Task<ServiceResult<DemoDataOperationResponse>> SeedAsync() =>
        _scope.ServiceProvider.GetRequiredService<WmsDemoDataSeeder>()
            .SeedAsync(DemoDataTestHost.ActorId, TestContext.Current.CancellationToken);

    public Task<ServiceResult<DemoDataOperationResponse>> ClearAsync() =>
        _scope.ServiceProvider.GetRequiredService<WmsDemoDataClearService>()
            .ClearAsync(DemoDataTestHost.ActorId, TestContext.Current.CancellationToken);

    public async ValueTask DisposeAsync()
    {
        _scope.Dispose();
        await _provider.DisposeAsync();
        await _database.DisposeAsync();
    }
}

internal sealed class ThrowAfterStageHook(string operation, string stage)
    : IWmsDemoDataStageHook
{
    public Task StageCompletedAsync(
        string currentOperation,
        string currentStage,
        CancellationToken cancellationToken)
    {
        if (currentOperation == operation && currentStage == stage)
        {
            throw new InvalidOperationException("Injected demo data stage failure.");
        }

        return Task.CompletedTask;
    }
}
