using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Myrmex.Integrations.OneC.Endpoints;
using Myrmex.Integrations.OneC.Imports;
using Myrmex.Integrations.OneC.Transport;
using Myrmex.Shared.Identity;
using Myrmex.Shared.Integrations.OneC;
using System.Runtime.CompilerServices;

namespace Myrmex.Tests.Integrations.Authorization;

public sealed class IntegrationAuthorizationEndpointTests
{
    private static readonly DateTimeOffset CheckedAtUtc =
        DateTimeOffset.Parse("2026-07-09T10:00:00Z");

    [Fact]
    public async Task OneCEndpoint_WhenAnonymous_Returns401WithoutSourceAccess()
    {
        RecordingOneCClient source = new();
        await using WebApplication app = CreateApp(source);
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpResponseMessage response = await SendConnectionTestAsync(
            app,
            cookie: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, source.CallCount);
    }

    [Fact]
    public async Task OneCEndpoint_WhenUnprivileged_Returns403WithoutSourceAccess()
    {
        RecordingOneCClient source = new();
        await using WebApplication app = CreateApp(source);
        await app.StartAsync(TestContext.Current.CancellationToken);

        string cookie = app.Services.CreateApiSessionCookie(roles: []);
        using HttpResponseMessage response = await SendConnectionTestAsync(
            app,
            cookie);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, source.CallCount);
    }

    [Theory]
    [InlineData(IdentityRoleNames.WmsOperator)]
    [InlineData(IdentityRoleNames.MyrmexAdmin)]
    public async Task OneCEndpoint_WhenEligibleRole_ReturnsSuccessAndTouchesSource(
        string role)
    {
        RecordingOneCClient source = new();
        await using WebApplication app = CreateApp(source);
        await app.StartAsync(TestContext.Current.CancellationToken);

        string cookie = app.Services.CreateApiSessionCookie([role]);
        using HttpResponseMessage response = await SendConnectionTestAsync(
            app,
            cookie);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, source.CallCount);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("", HttpStatusCode.Forbidden)]
    public async Task OneCImport_WhenDenied_DoesNotStartImport(
        string? role,
        HttpStatusCode expectedStatus)
    {
        RecordingOneCImportService importService = new();
        await using WebApplication app = CreateApp(
            new RecordingOneCClient(),
            importService);
        await app.StartAsync(TestContext.Current.CancellationToken);

        string? cookie = role is null
            ? null
            : app.Services.CreateApiSessionCookie(roles: []);
        using HttpResponseMessage response = await SendPostAsync(
            app,
            "/api/integrations/1c/warehouses/import",
            cookie);

        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal(0, importService.CallCount);
    }

    private static WebApplication CreateApp(
        IOneCODataClient source,
        IOneCImportService? importService = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(source);
        builder.Services.AddSingleton(
            importService ?? new RecordingOneCImportService());
        builder.Services.AddSingleton<TimeProvider>(
            new FixedTimeProvider(CheckedAtUtc));
        builder.Services.AddTestApiSessionAuthentication();

        WebApplication app = builder.Build();
        app.UseTestApiSessionAuthentication();
        app.MapOneCIntegration();
        return app;
    }

    private static Task<HttpResponseMessage> SendConnectionTestAsync(
        WebApplication app,
        string? cookie) =>
        SendPostAsync(app, "/api/integrations/1c/connection/test", cookie);

    private static async Task<HttpResponseMessage> SendPostAsync(
        WebApplication app,
        string path,
        string? cookie)
    {
        using HttpClient client = CreateClient(app);
        using HttpRequestMessage request = new(HttpMethod.Post, path);
        if (cookie is not null)
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookie);
        }

        return await client.SendAsync(
            request,
            TestContext.Current.CancellationToken);
    }

    private static HttpClient CreateClient(WebApplication app)
    {
        string address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.Single();
        return new HttpClient { BaseAddress = new Uri(address) };
    }

    private sealed class RecordingOneCClient : IOneCODataClient
    {
        public int CallCount { get; private set; }

        public void ValidateConfiguration()
        {
        }

        public Task TestConnectionAsync(CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Catalog_Склады>> ReadWarehousesAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Catalog_Склады>>([]);

        public Task<IReadOnlyList<Catalog_УпаковкиЕдиницыИзмерения>>
            ReadUnitsOfMeasureAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Catalog_УпаковкиЕдиницыИзмерения>>([]);

        public async IAsyncEnumerable<IReadOnlyList<Catalog_Номенклатура>>
            ReadNomenclaturePagesAsync(
                [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    private sealed class RecordingOneCImportService : IOneCImportService
    {
        public int CallCount { get; private set; }

        public Task<OneCImportResponse> ImportWarehousesAsync(
            CancellationToken cancellationToken) =>
            CompleteAsync("warehouses");

        public Task<OneCImportResponse> ImportUnitsOfMeasureAsync(
            CancellationToken cancellationToken) =>
            CompleteAsync("uoms");

        public Task<OneCImportResponse> ImportStockKeepingUnitsAsync(
            CancellationToken cancellationToken) =>
            CompleteAsync("skus");

        private Task<OneCImportResponse> CompleteAsync(string referenceType)
        {
            CallCount++;
            return Task.FromResult(new OneCImportResponse(
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
                Errors: []));
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
