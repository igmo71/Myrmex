using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Myrmex.AspNetCore.Security;
using Myrmex.Integrations.OneC.Configuration;
using Myrmex.Integrations.OneC.Endpoints;
using Myrmex.Integrations.OneC.Notifications;
using Myrmex.Integrations.OneC.Security;
using Myrmex.Integrations.Persistence;
using Myrmex.Integrations.Synchronization;
using Myrmex.Integrations.Synchronization.Processing;
using System.Net.Http.Json;

namespace Myrmex.Tests.Integrations.OneC.Endpoints;

public sealed class OneCNotificationEndpointTests
{
    private const string ApiKey = "development-only-key";
    private static readonly DateTimeOffset ReceivedAtUtc =
        DateTimeOffset.Parse("2026-07-14T12:00:00Z");

    [Theory]
    [InlineData("/api/integrations/1c/receiving-orders/changed", SynchronizationEntityTypes.ReceivingOrder)]
    [InlineData("/api/integrations/1c/shipping-orders/changed", SynchronizationEntityTypes.ShippingOrder)]
    public async Task NotificationEndpoint_WhenValid_PersistsRequestAndReturnsEmptyAccepted(
        string route,
        string expectedEntityType)
    {
        await using WebApplication app = CreateApp();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = CreateClient(app);
        using HttpRequestMessage request = CreateNotificationRequest(
            route,
            new Dictionary<string, object?>
            {
                ["Ref_Key"] = "80066011-d7c7-11ef-bac8-00155d01d112",
                ["DataVersion"] = Convert.ToBase64String([1, 2, 3]),
                ["Number"] = "UT-00001004",
                ["Date"] = "2025-01-21T10:15:36",
                ["Ignored"] = "unknown property"
            });

        using HttpResponseMessage response = await client.SendAsync(
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(
            string.Empty,
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        SynchronizationRequest saved = Assert.Single(await ReadRequestsAsync(app));
        Assert.Equal(OneCIntegrationApiKeyOptions.DefaultSourceSystem, saved.SourceSystem);
        Assert.Equal("main-infobase", saved.SourceInstance);
        Assert.Equal(expectedEntityType, saved.EntityType);
        Assert.Equal("80066011-d7c7-11ef-bac8-00155d01d112", saved.ExternalId);
        Assert.Equal(new byte[] { 1, 2, 3 }, saved.ExternalDataVersion);
        Assert.Equal("UT-00001004", saved.ExternalDocumentNumber);
        Assert.Equal(new DateTime(2025, 1, 21, 10, 15, 36), saved.ExternalDocumentDate);
        Assert.Equal(DateTimeKind.Unspecified, saved.ExternalDocumentDate!.Value.Kind);
        Assert.Equal(SynchronizationTriggers.ChangeNotification, saved.Trigger);
        Assert.Equal(SynchronizationStatus.Pending, saved.Status);
        Assert.Equal(ReceivedAtUtc, saved.ReceivedAtUtc);
        Assert.True(app.Services.GetRequiredService<SynchronizationWakeUp>().Reader.TryRead(out _));
    }

    [Fact]
    public async Task NotificationEndpoint_DoesNotChangeGlobalJsonCaseSensitivity()
    {
        await using WebApplication app = CreateApp();

        JsonOptions options = app.Services
            .GetRequiredService<IOptions<JsonOptions>>()
            .Value;

        Assert.True(options.SerializerOptions.PropertyNameCaseInsensitive);
        await Task.CompletedTask;
    }

    private static WebApplication CreateApp()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddLogging();
        builder.Services.AddProblemDetails();
        string databaseName = Guid.NewGuid().ToString("N");
        builder.Services.AddDbContext<IntegrationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        builder.Services.Configure<OneCIntegrationApiKeyOptions>(options =>
        {
            options.SourceSystem = OneCIntegrationApiKeyOptions.DefaultSourceSystem;
            options.SourceInstance = "main-infobase";
            options.ApiKey = ApiKey;
        });
        builder.Services.AddSingleton<TimeProvider>(
            new FixedTimeProvider(ReceivedAtUtc));
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
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapOneCNotificationEndpoints();
        return app;
    }

    private static HttpRequestMessage CreateNotificationRequest(
        string route,
        Dictionary<string, object?> payload)
    {
        HttpRequestMessage request = new(HttpMethod.Post, route)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.TryAddWithoutValidation("Authorization", $"ApiKey {ApiKey}");
        return request;
    }

    private static async Task<List<SynchronizationRequest>> ReadRequestsAsync(
        WebApplication app)
    {
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        IntegrationDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
        return await dbContext.SynchronizationRequests
            .OrderBy(request => request.ReceivedAtUtc)
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    private static HttpClient CreateClient(WebApplication app)
    {
        string address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.Single();
        return new HttpClient { BaseAddress = new Uri(address) };
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
