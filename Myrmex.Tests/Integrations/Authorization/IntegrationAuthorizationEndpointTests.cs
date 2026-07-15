using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Myrmex.AspNetCore.Security;
using Myrmex.Integrations.OneC.Configuration;
using Myrmex.Integrations.OneC.Endpoints;
using Myrmex.Integrations.OneC.Imports;
using Myrmex.Integrations.OneC.Notifications;
using Myrmex.Integrations.OneC.Security;
using Myrmex.Integrations.OneC.Transport;
using Myrmex.Integrations.Persistence;
using Myrmex.Integrations.Synchronization;
using Myrmex.Integrations.Synchronization.Processing;
using Myrmex.Shared.Identity;
using Myrmex.Shared.Integrations.OneC;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;

namespace Myrmex.Tests.Integrations.Authorization;

public sealed class IntegrationAuthorizationEndpointTests
{
    private const string IntegrationApiKey = "development-only-key";

    private static readonly string[] ImportPaths =
    [
        "/api/integrations/1c/warehouses/import",
        "/api/integrations/1c/uoms/import",
        "/api/integrations/1c/skus/import"
    ];

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
    public async Task OneCAdminAndImportEndpoints_WhenEligibleRole_ReturnSuccess(
        string role)
    {
        RecordingOneCClient source = new();
        RecordingOneCImportService importService = new();
        await using WebApplication app = CreateApp(source, importService);
        await app.StartAsync(TestContext.Current.CancellationToken);

        string cookie = app.Services.CreateApiSessionCookie([role]);
        using HttpResponseMessage response = await SendConnectionTestAsync(
            app,
            cookie);

        List<HttpResponseMessage> importResponses = [];
        foreach (string path in ImportPaths)
        {
            importResponses.Add(await SendPostAsync(app, path, cookie));
        }

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        foreach (HttpResponseMessage importResponse in importResponses)
        {
            using (importResponse)
            {
                Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);
            }
        }

        Assert.Equal(1, source.CallCount);
        Assert.Equal(ImportPaths.Length, importService.CallCount);
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

    [Fact]
    public async Task NotificationEndpoint_WhenIntegrationApiKeyIsValid_ReturnsAccepted()
    {
        await using WebApplication app = CreateApp(new RecordingOneCClient());
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpResponseMessage response = await SendNotificationAsync(
            app,
            apiKey: IntegrationApiKey,
            cookie: null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("wrong-key")]
    public async Task NotificationEndpoint_WhenIntegrationApiKeyIsMissingOrInvalid_Returns401(
        string? apiKey)
    {
        await using WebApplication app = CreateApp(new RecordingOneCClient());
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpResponseMessage response = await SendNotificationAsync(
            app,
            apiKey,
            cookie: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task NotificationEndpoint_WhenOnlyApiSessionCookieIsPresent_Returns401()
    {
        await using WebApplication app = CreateApp(new RecordingOneCClient());
        await app.StartAsync(TestContext.Current.CancellationToken);

        string cookie = app.Services.CreateApiSessionCookie(
            [IdentityRoleNames.WmsOperator]);
        using HttpResponseMessage response = await SendNotificationAsync(
            app,
            apiKey: null,
            cookie);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/integrations/1c/connection/test")]
    [InlineData("/api/integrations/1c/warehouses/import")]
    [InlineData("/api/integrations/1c/uoms/import")]
    [InlineData("/api/integrations/1c/skus/import")]
    public async Task OneCAdminAndImportEndpoints_WhenMachineApiKeyIsPresent_Return401(
        string path)
    {
        RecordingOneCClient source = new();
        RecordingOneCImportService importService = new();
        await using WebApplication app = CreateApp(source, importService);
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpResponseMessage response = await SendPostAsync(
            app,
            path,
            cookie: null,
            apiKey: IntegrationApiKey);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, source.CallCount);
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
        builder.Services.AddProblemDetails();
        builder.Services.AddDbContext<IntegrationDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString("N")));
        builder.Services.Configure<OneCIntegrationApiKeyOptions>(options =>
        {
            options.SourceSystem = OneCIntegrationApiKeyOptions.DefaultSourceSystem;
            options.SourceInstance = "main-infobase";
            options.ApiKey = IntegrationApiKey;
        });
        builder.Services.AddSingleton<SynchronizationWakeUp>();
        builder.Services.AddSingleton<OneCChangeNotificationValidator>();
        builder.Services.AddScoped<SynchronizationRequestFactory>();
        builder.Services.AddScoped<SynchronizationRequestStore>();
        builder.Services
            .AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                MyrmexAuthenticationSchemes.IntegrationApiKey,
                options => { });
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(
                MyrmexAuthorizationPolicies.OneCIntegration,
                MyrmexAuthorizationPolicies.ConfigureOneCIntegration);

        WebApplication app = builder.Build();
        app.UseTestApiSessionAuthentication();
        app.MapOneCIntegration();
        app.MapOneCNotificationEndpoints();
        return app;
    }

    private static Task<HttpResponseMessage> SendConnectionTestAsync(
        WebApplication app,
        string? cookie) =>
        SendPostAsync(app, "/api/integrations/1c/connection/test", cookie);

    private static async Task<HttpResponseMessage> SendPostAsync(
        WebApplication app,
        string path,
        string? cookie,
        string? apiKey = null,
        HttpContent? content = null)
    {
        using HttpClient client = CreateClient(app);
        using HttpRequestMessage request = new(HttpMethod.Post, path)
        {
            Content = content
        };
        if (cookie is not null)
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookie);
        }

        if (apiKey is not null)
        {
            request.Headers.TryAddWithoutValidation("Authorization", $"ApiKey {apiKey}");
        }

        return await client.SendAsync(
            request,
            TestContext.Current.CancellationToken);
    }

    private static Task<HttpResponseMessage> SendNotificationAsync(
        WebApplication app,
        string? apiKey,
        string? cookie) =>
        SendPostAsync(
            app,
            "/api/integrations/1c/receiving-orders/changed",
            cookie,
            apiKey,
            JsonContent.Create(new Dictionary<string, string>
            {
                ["Ref_Key"] = "80066011-d7c7-11ef-bac8-00155d01d112",
                ["DataVersion"] = Convert.ToBase64String([1, 2, 3])
            }));

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
