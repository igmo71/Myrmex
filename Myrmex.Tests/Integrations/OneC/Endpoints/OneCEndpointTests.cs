using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Myrmex.Integrations.OneC.Endpoints;
using Myrmex.Integrations.OneC.Imports;
using Myrmex.Integrations.OneC.Transport;
using Myrmex.Shared.Integrations.OneC;

namespace Myrmex.Tests.Integrations.OneC.Endpoints;

public sealed class OneCEndpointTests
{
    private static readonly DateTimeOffset CheckedAtUtc = DateTimeOffset.Parse("2026-06-27T12:00:00Z");

    [Fact]
    public async Task TestConnection_WhenAuthenticatedAndReady_ReturnsProbeSummary()
    {
        var client = new StubOneCODataClient();
        await using WebApplication app = CreateApp(client, authenticated: true);
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
        Assert.Equal(1, client.CallCount);
    }

    [Fact]
    public async Task TestConnection_WhenUnauthenticated_Returns401WithoutSourceAccess()
    {
        var client = new StubOneCODataClient();
        await using WebApplication app = CreateApp(client, authenticated: false);
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient httpClient = CreateClient(app);
        using HttpResponseMessage response = await httpClient.PostAsync(
            "/api/integrations/1c/connection/test", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, client.CallCount);
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
        var client = new StubOneCODataClient(new OneCTransportException(
            (OneCTransportFailureReason)reason,
            "Safe failure."));
        await using WebApplication app = CreateApp(client, authenticated: true);
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient httpClient = CreateClient(app);
        using HttpResponseMessage response = await httpClient.PostAsync(
            "/api/integrations/1c/connection/test", null, TestContext.Current.CancellationToken);

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
            Processed: 2,
            Created: 1,
            Updated: 0,
            Skipped: 1,
            Failed: 0,
            StartedAtUtc: CheckedAtUtc,
            CompletedAtUtc: CheckedAtUtc,
            OperationError: isComplete ? null : new OneCImportOperationError("SourceUnavailable", "Unavailable."),
            Errors: []);
        StubImportService importService = new(expected);
        await using WebApplication app = CreateApp(
            new StubOneCODataClient(),
            authenticated: true,
            importService: importService);
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
        Assert.Equal(!isComplete, payload?.OperationError is not null);
        Assert.Equal(1, importService.CallCount);
    }

    [Fact]
    public async Task ImportRoute_WhenConfigurationFailsBeforeStart_Returns400ProblemDetails()
    {
        StubImportService importService = new(new OneCTransportException(
            OneCTransportFailureReason.InvalidConfiguration,
            "Configuration is invalid."));
        await using WebApplication app = CreateApp(
            new StubOneCODataClient(),
            authenticated: true,
            importService: importService);
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
        StubImportService importService = new(CreateImportResponse("warehouses"));
        await using WebApplication app = CreateApp(
            new StubOneCODataClient(),
            authenticated: false,
            importService: importService);
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient httpClient = CreateClient(app);
        using HttpResponseMessage response = await httpClient.PostAsync(
            "/api/integrations/1c/warehouses/import",
            null,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, importService.CallCount);
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
        StubImportService importService = new(expected);
        await using WebApplication app = CreateApp(
            new StubOneCODataClient(),
            authenticated: true,
            importService: importService);
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
        SelectiveConflictImportService importService = new();
        await using WebApplication app = CreateApp(
            new StubOneCODataClient(),
            authenticated: true,
            importService: importService);
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
        Assert.Equal(1, importService.WarehouseCallCount);
    }

    private static WebApplication CreateApp(
        IOneCODataClient client,
        bool authenticated,
        IOneCImportService? importService = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(client);
        builder.Services.AddSingleton(importService ?? new StubImportService(CreateImportResponse("warehouses")));
        builder.Services.AddSingleton<TimeProvider>(new FixedTimeProvider(CheckedAtUtc));
        builder.Services.AddTestAuthentication(authenticated, actorId: "operator");

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

    private sealed class StubOneCODataClient(Exception? exception = null) : IOneCODataClient
    {
        public int CallCount { get; private set; }

        public void ValidateConfiguration()
        {
            if (exception is not null)
            {
                throw exception;
            }
        }

        public Task TestConnectionAsync(CancellationToken cancellationToken)
        {
            CallCount++;
            return exception is null ? Task.CompletedTask : Task.FromException(exception);
        }

        public Task<IReadOnlyList<Catalog_Склады>> ReadWarehousesAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Catalog_Склады>>([]);

        public Task<IReadOnlyList<Catalog_УпаковкиЕдиницыИзмерения>> ReadUnitsOfMeasureAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Catalog_УпаковкиЕдиницыИзмерения>>([]);

        public async IAsyncEnumerable<IReadOnlyList<Catalog_Номенклатура>> ReadNomenclaturePagesAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    private sealed class StubImportService : IOneCImportService
    {
        private readonly OneCImportResponse? _response;
        private readonly Exception? _exception;

        public StubImportService(OneCImportResponse response) => _response = response;
        public StubImportService(Exception exception) => _exception = exception;

        public int CallCount { get; private set; }

        public Task<OneCImportResponse> ImportWarehousesAsync(CancellationToken cancellationToken) => Complete();
        public Task<OneCImportResponse> ImportUnitsOfMeasureAsync(CancellationToken cancellationToken) => Complete();
        public Task<OneCImportResponse> ImportStockKeepingUnitsAsync(CancellationToken cancellationToken) => Complete();

        private Task<OneCImportResponse> Complete()
        {
            CallCount++;
            return _exception is null
                ? Task.FromResult(_response!)
                : Task.FromException<OneCImportResponse>(_exception);
        }
    }

    private sealed class SelectiveConflictImportService : IOneCImportService
    {
        public int WarehouseCallCount { get; private set; }

        public Task<OneCImportResponse> ImportWarehousesAsync(CancellationToken cancellationToken)
        {
            WarehouseCallCount++;
            return Task.FromResult(CreateImportResponse("warehouses"));
        }

        public Task<OneCImportResponse> ImportUnitsOfMeasureAsync(CancellationToken cancellationToken) =>
            Task.FromResult(CreateImportResponse("uoms"));

        public Task<OneCImportResponse> ImportStockKeepingUnitsAsync(CancellationToken cancellationToken) =>
            Task.FromException<OneCImportResponse>(new OneCImportAlreadyInProgressException("skus"));
    }

    private static OneCImportResponse CreateImportResponse(string referenceType) => new(
        referenceType,
        IsComplete: true,
        Processed: 0,
        Created: 0,
        Updated: 0,
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
