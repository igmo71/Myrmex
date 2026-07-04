using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Myrmex.AppDispatching;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms;
using Myrmex.Modules.Wms.DemoData.Configuration;
using Myrmex.Modules.Wms.DemoData.Endpoints;
using Myrmex.Modules.Wms.DemoData.Features;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Shared.Wms.DemoData;
using Myrmex.Tests.Wms.Topology.Testing;
using System.Security.Claims;

namespace Myrmex.Tests.Wms.DemoData.Testing;

internal static class DemoDataTestHost
{
    public const string ActorId = "demo-test-operator";

    public static async Task<RunningDemoDataApp> StartAsync(
        RecordingCommandDispatcher dispatcher,
        WmsDemoDataOptions? options = null,
        string? environmentName = null,
        bool authenticated = true)
    {
        environmentName ??= Environments.Development;

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.WebHost.UseEnvironment(environmentName);
        builder.Services.AddProblemDetails();
        builder.Services.AddSingleton<ICommandDispatcher>(dispatcher);
        builder.Services.AddSingleton(Options.Create(options ?? new WmsDemoDataOptions
        {
            Enabled = true,
            AllowClear = true,
            ClearConfirmation = "CLEAR-MYRMEX-DEMO"
        }));

        WebApplication app = builder.Build();
        app.UseExceptionHandler();
        if (authenticated)
        {
            app.Use(async (context, next) =>
            {
                context.User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim("sub", ActorId)],
                    authenticationType: "Test"));
                await next(context);
            });
        }

        app.MapDemoDataAdminEndpoints();
        await app.StartAsync(TestContext.Current.CancellationToken);
        return new RunningDemoDataApp(app);
    }

    internal sealed class RunningDemoDataApp(WebApplication app) : IAsyncDisposable
    {
        public HttpClient CreateClient()
        {
            string address = app.Services
                .GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!
                .Addresses.Single();
            return new HttpClient { BaseAddress = new Uri(address) };
        }

        public async ValueTask DisposeAsync()
        {
            await app.StopAsync(TestContext.Current.CancellationToken);
            await app.DisposeAsync();
        }
    }
}

internal sealed class RecordingCommandDispatcher(Func<object, IServiceResult> resultFactory)
    : ICommandDispatcher
{
    public object? Command { get; private set; }
    public CancellationToken CancellationToken { get; private set; }
    public int CallCount { get; private set; }

    public Task<TResult> DispatchAsync<TCommand, TResult>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResult>
        where TResult : IServiceResult
    {
        Command = command;
        CancellationToken = cancellationToken;
        CallCount++;
        return Task.FromResult((TResult)resultFactory(command!));
    }
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
