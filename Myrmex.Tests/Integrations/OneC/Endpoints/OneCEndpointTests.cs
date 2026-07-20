using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Myrmex.Integrations.OneC.Common.Imports;
using Myrmex.Integrations.OneC.Common.Transport;
using Myrmex.Integrations.OneC.Connection;
using Myrmex.Integrations.OneC.Endpoints;
using Myrmex.Integrations.OneC.StockKeepingUnits;
using Myrmex.Integrations.OneC.UnitsOfMeasure;
using Myrmex.Integrations.OneC.Warehouses;
using Myrmex.Shared.Integrations.OneC;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;

namespace Myrmex.Tests.Integrations.OneC.Endpoints;

public sealed class OneCEndpointTests
{
    private static readonly DateTimeOffset CheckedAtUtc =
        DateTimeOffset.Parse("2026-06-27T12:00:00Z");

    [Fact]
    public async Task TestConnection_WhenAuthenticatedAndReady_ReturnsProbeSummary()
    {
        ProbeCounter probes = new();
        await using WebApplication app = CreateApp(probes, authenticated: true);
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient httpClient = CreateClient(app);
        using HttpResponseMessage response = await httpClient.PostAsync(
            "/api/integrations/1c/connection/test",
            content: null,
            TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        OneCConnectionTestResponse? payload = await response.Content
            .ReadFromJsonAsync<OneCConnectionTestResponse>(TestContext.Current.CancellationToken);
        Assert.True(payload?.IsReady);
        Assert.Equal(CheckedAtUtc, payload?.CheckedAtUtc);
        Assert.Equal(["warehouses", "uoms", "skus"], payload?.CheckedReferenceTypes);
        Assert.Equal(3, probes.CallCount);
    }

    [Fact]
    public async Task TestConnection_WhenUnauthenticated_Returns401WithoutSourceAccess()
    {
        ProbeCounter probes = new();
        await using WebApplication app = CreateApp(probes, authenticated: false);
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient httpClient = CreateClient(app);
        using HttpResponseMessage response = await httpClient.PostAsync(
            "/api/integrations/1c/connection/test",
            null,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, probes.CallCount);
    }

    [Theory]
    [InlineData((int)OneCTransportFailureReason.InvalidConfiguration, HttpStatusCode.BadRequest, "OneC.ConfigurationInvalid")]
    [InlineData((int)OneCTransportFailureReason.AuthenticationFailed, HttpStatusCode.BadGateway, "OneC.AuthenticationFailed")]
    [InlineData((int)OneCTransportFailureReason.SourceUnavailable, HttpStatusCode.BadGateway, "OneC.SourceUnavailable")]
    [InlineData((int)OneCTransportFailureReason.EntitySetUnavailable, HttpStatusCode.BadGateway, "OneC.EntitySetUnavailable")]
    [InlineData((int)OneCTransportFailureReason.MalformedResponse, HttpStatusCode.BadGateway, "OneC.MalformedResponse")]
    [InlineData((int)OneCTransportFailureReason.Timeout, HttpStatusCode.GatewayTimeout, "OneC.Timeout")]
    public async Task TestConnection_WhenTransportFails_ReturnsSafeProblemDetails(
        int reason,
        HttpStatusCode status,
        string expectedCode)
    {
        OneCTransportException exception = new(
            (OneCTransportFailureReason)reason,
            "Safe failure.");
        await using WebApplication app = CreateApp(
            new ProbeCounter(),
            authenticated: true,
            connectionException: exception);
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient httpClient = CreateClient(app);
        using HttpResponseMessage response = await httpClient.PostAsync(
            "/api/integrations/1c/connection/test",
            null,
            TestContext.Current.CancellationToken);

        Assert.Equal(status, response.StatusCode);
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);
        Assert.Equal(expectedCode, problem?.Extensions["code"]?.ToString());
        Assert.Equal("Safe failure.", problem?.Detail);
    }

    [Theory]
    [InlineData("/api/integrations/1c/warehouses/import", "warehouses", true)]
    [InlineData("/api/integrations/1c/uoms/import", "uoms", false)]
    [InlineData("/api/integrations/1c/skus/import", "skus", true)]
    public async Task ImportRoutes_WhenStarted_ReturnCompleteOrIncompleteResponse(
        string route,
        string referenceType,
        bool isComplete)
    {
        OneCImportResponse expected = new(
            referenceType,
            isComplete,
            Processed: 3,
            Created: 1,
            Updated: 0,
            Unchanged: 1,
            Skipped: 1,
            Failed: 0,
            StartedAtUtc: CheckedAtUtc,
            CompletedAtUtc: CheckedAtUtc,
            OperationError: isComplete
                ? null
                : new OneCImportOperationError("SourceUnavailable", "Unavailable."),
            Errors: []);
        StubImportOperations imports = new(expected);
        await using WebApplication app = CreateApp(
            new ProbeCounter(),
            authenticated: true,
            imports: imports);
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient httpClient = CreateClient(app);
        using HttpResponseMessage response = await httpClient.PostAsync(
            route,
            content: null,
            TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        OneCImportResponse? payload = await response.Content.ReadFromJsonAsync<OneCImportResponse>(
            TestContext.Current.CancellationToken);
        Assert.Equal(referenceType, payload?.ReferenceType);
        Assert.Equal(isComplete, payload?.IsComplete);
        Assert.Equal(1, payload?.Unchanged);
        Assert.Equal(!isComplete, payload?.OperationError is not null);
        Assert.Equal(1, imports.CallCount);
    }

    [Fact]
    public async Task ImportRoute_WhenConfigurationFailsBeforeStart_Returns400ProblemDetails()
    {
        StubImportOperations imports = new(new OneCTransportException(
            OneCTransportFailureReason.InvalidConfiguration,
            "Configuration is invalid."));
        await using WebApplication app = CreateApp(
            new ProbeCounter(),
            authenticated: true,
            imports: imports);
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient httpClient = CreateClient(app);
        using HttpResponseMessage response = await httpClient.PostAsync(
            "/api/integrations/1c/warehouses/import",
            null,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);
        Assert.Equal("OneC.ConfigurationInvalid", problem?.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task ImportRoute_WhenUnauthenticated_Returns401WithoutStartingImport()
    {
        StubImportOperations imports = new(CreateImportResponse("warehouses"));
        await using WebApplication app = CreateApp(
            new ProbeCounter(),
            authenticated: false,
            imports: imports);
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient httpClient = CreateClient(app);
        using HttpResponseMessage response = await httpClient.PostAsync(
            "/api/integrations/1c/warehouses/import",
            null,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, imports.CallCount);
    }

    [Fact]
    public async Task ImportSkuRoute_SerializesStableBaseUnitRecordReason()
    {
        OneCImportResponse expected = new(
            "skus",
            IsComplete: true,
            Processed: 1,
            Created: 0,
            Updated: 0,
            Unchanged: 0,
            Skipped: 0,
            Failed: 1,
            StartedAtUtc: CheckedAtUtc,
            CompletedAtUtc: CheckedAtUtc,
            OperationError: null,
            Errors:
            [
                new OneCImportRecordError(
                    Guid.NewGuid(),
                    "SKU-1",
                    "BaseUnitOfMeasureExternalRefKeyMissing",
                    "Base unit is required.")
            ]);
        StubImportOperations imports = new(expected);
        await using WebApplication app = CreateApp(
            new ProbeCounter(),
            authenticated: true,
            imports: imports);
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient httpClient = CreateClient(app);
        using HttpResponseMessage response = await httpClient.PostAsync(
            "/api/integrations/1c/skus/import",
            null,
            TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        OneCImportResponse? payload = await response.Content.ReadFromJsonAsync<OneCImportResponse>(
            TestContext.Current.CancellationToken);
        Assert.Equal("BaseUnitOfMeasureExternalRefKeyMissing", Assert.Single(payload!.Errors).Reason);
    }

    [Fact]
    public async Task ImportRoutes_WhenSkuImportIsAlreadyRunning_Returns409WithoutBlockingWarehouseImport()
    {
        StubImportOperations imports = new(skuConflict: true);
        await using WebApplication app = CreateApp(
            new ProbeCounter(),
            authenticated: true,
            imports: imports);
        await app.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = CreateClient(app);

        using HttpResponseMessage conflictResponse = await httpClient.PostAsync(
            "/api/integrations/1c/skus/import",
            content: null,
            TestContext.Current.CancellationToken);
        using HttpResponseMessage warehouseResponse = await httpClient.PostAsync(
            "/api/integrations/1c/warehouses/import",
            content: null,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
        ProblemDetails? problem = await conflictResponse.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);
        Assert.Equal("OneCImport.AlreadyInProgress", problem?.Extensions["code"]?.ToString());
        warehouseResponse.EnsureSuccessStatusCode();
        Assert.Equal(1, imports.WarehouseCallCount);
    }

    private static WebApplication CreateApp(
        ProbeCounter probes,
        bool authenticated,
        StubImportOperations? imports = null,
        OneCTransportException? connectionException = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        StubTransport transport = new(
            connectionException?.Reason is OneCTransportFailureReason.Disabled or
                OneCTransportFailureReason.InvalidConfiguration
                ? connectionException
                : null);
        StubWarehouseSource warehouseSource = new(probes, connectionException);
        StubUnitOfMeasureSource unitSource = new(probes);
        StubStockKeepingUnitSource skuSource = new(probes);
        OneCConnectionTest connectionTest = new(
            transport,
            warehouseSource,
            unitSource,
            skuSource,
            NullLogger<OneCConnectionTest>.Instance);
        StubImportOperations importOperations =
            imports ?? new StubImportOperations(CreateImportResponse("warehouses"));

        builder.Services.AddSingleton(connectionTest);
        builder.Services.AddSingleton<IWarehouseOneCImport>(importOperations);
        builder.Services.AddSingleton<IUnitOfMeasureOneCImport>(importOperations);
        builder.Services.AddSingleton<IStockKeepingUnitOneCImport>(importOperations);
        builder.Services.AddSingleton<TimeProvider>(new FixedTimeProvider(CheckedAtUtc));
        builder.Services.AddTestAuthentication(authenticated);

        WebApplication app = builder.Build();
        app.UseTestAuthentication();
        app.MapOneCIntegration();
        return app;
    }

    private static HttpClient CreateClient(WebApplication app)
    {
        string address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.Single();
        return new HttpClient { BaseAddress = new Uri(address) };
    }

    private sealed class ProbeCounter
    {
        private int _callCount;

        public int CallCount => Volatile.Read(ref _callCount);

        public void Increment() => Interlocked.Increment(ref _callCount);
    }

    private sealed class StubTransport(Exception? configurationException = null)
        : IOneCODataTransport
    {
        public void ValidateConfiguration()
        {
            if (configurationException is not null)
            {
                throw configurationException;
            }
        }

        public Task<IReadOnlyList<T>> ReadCollectionAsync<T>(
            string entitySet,
            IEnumerable<KeyValuePair<string, string>> parameters,
            CancellationToken cancellationToken)
            where T : class => throw new NotSupportedException();
    }

    private sealed class StubWarehouseSource(
        ProbeCounter probes,
        Exception? probeException = null) : IWarehouseOneCSource
    {
        public Task<IReadOnlyList<WarehouseSourceRecord>> ReadAllAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<WarehouseSourceRecord>>([]);

        public Task<WarehouseSourceRecord?> ReadCurrentAsync(
            Guid externalRefKey,
            CancellationToken cancellationToken) => Task.FromResult<WarehouseSourceRecord?>(null);

        public Task ProbeAsync(CancellationToken cancellationToken)
        {
            probes.Increment();
            return probeException is null
                ? Task.CompletedTask
                : Task.FromException(probeException);
        }
    }

    private sealed class StubUnitOfMeasureSource(ProbeCounter probes) : IUnitOfMeasureOneCSource
    {
        public Task<IReadOnlyList<UnitOfMeasureSourceRecord>> ReadAllAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<UnitOfMeasureSourceRecord>>([]);

        public Task<UnitOfMeasureSourceRecord?> ReadCurrentAsync(
            Guid externalRefKey,
            CancellationToken cancellationToken) =>
            Task.FromResult<UnitOfMeasureSourceRecord?>(null);

        public Task ProbeAsync(CancellationToken cancellationToken)
        {
            probes.Increment();
            return Task.CompletedTask;
        }
    }

    private sealed class StubStockKeepingUnitSource(ProbeCounter probes)
        : IStockKeepingUnitOneCSource
    {
        public async IAsyncEnumerable<IReadOnlyList<StockKeepingUnitSourceRecord>> ReadPagesAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }

        public Task<StockKeepingUnitSourceRecord?> ReadCurrentAsync(
            Guid externalRefKey,
            CancellationToken cancellationToken) =>
            Task.FromResult<StockKeepingUnitSourceRecord?>(null);

        public Task ProbeAsync(CancellationToken cancellationToken)
        {
            probes.Increment();
            return Task.CompletedTask;
        }
    }

    private sealed class StubImportOperations :
        IWarehouseOneCImport,
        IUnitOfMeasureOneCImport,
        IStockKeepingUnitOneCImport
    {
        private readonly OneCImportResponse? _response;
        private readonly Exception? _exception;
        private readonly bool _skuConflict;

        public StubImportOperations(OneCImportResponse response) => _response = response;
        public StubImportOperations(Exception exception) => _exception = exception;
        public StubImportOperations(bool skuConflict) => _skuConflict = skuConflict;

        public int CallCount { get; private set; }
        public int WarehouseCallCount { get; private set; }

        Task<OneCImportResponse> IWarehouseOneCImport.ImportAsync(
            CancellationToken cancellationToken)
        {
            WarehouseCallCount++;
            return Complete("warehouses");
        }

        Task<OneCImportResponse> IUnitOfMeasureOneCImport.ImportAsync(
            CancellationToken cancellationToken) => Complete("uoms");

        Task<OneCImportResponse> IStockKeepingUnitOneCImport.ImportAsync(
            CancellationToken cancellationToken) =>
            _skuConflict
                ? Task.FromException<OneCImportResponse>(
                    new OneCImportAlreadyInProgressException("skus"))
                : Complete("skus");

        private Task<OneCImportResponse> Complete(string referenceType)
        {
            CallCount++;
            if (_exception is not null)
            {
                return Task.FromException<OneCImportResponse>(_exception);
            }

            return Task.FromResult(
                _response?.ReferenceType == referenceType
                    ? _response
                    : CreateImportResponse(referenceType));
        }
    }

    private static OneCImportResponse CreateImportResponse(string referenceType) => new(
        referenceType,
        IsComplete: true,
        Processed: 0,
        Created: 0,
        Updated: 0,
        Unchanged: 0,
        Skipped: 0,
        Failed: 0,
        StartedAtUtc: CheckedAtUtc,
        CompletedAtUtc: CheckedAtUtc,
        OperationError: null,
        Errors: []);

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
