using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Myrmex.AspNetCore.Security;
using Myrmex.Integrations.OneC.Configuration;
using Myrmex.Integrations.OneC.Endpoints;
using Myrmex.Integrations.OneC.Notifications;
using Myrmex.Integrations.OneC.Security;
using Myrmex.Integrations.Persistence;
using Myrmex.Integrations.Persistence.SqlServer;
using Myrmex.Integrations.Synchronization;
using Myrmex.Integrations.Synchronization.Processing;
using System.Net.Http.Json;
using System.Text.Json;

namespace Myrmex.Tests.Integrations.OneC.Endpoints;

public sealed class OneCNotificationValidationTests
{
    private const string ApiKey = "development-only-key";

    public static TheoryData<Dictionary<string, object?>, string> InvalidPayloads()
    {
        string validDataVersion = Convert.ToBase64String([1, 2, 3]);
        return new TheoryData<Dictionary<string, object?>, string>
        {
            {
                new Dictionary<string, object?>
                {
                    ["DataVersion"] = validDataVersion
                },
                "Ref_Key"
            },
            {
                CreatePayload(refKey: "not-a-guid", dataVersion: validDataVersion),
                "Ref_Key"
            },
            {
                CreatePayload(dataVersion: "not-base64!"),
                "DataVersion"
            },
            {
                CreatePayload(dataVersion: Convert.ToBase64String([])),
                "DataVersion"
            },
            {
                CreatePayload(dataVersion: Convert.ToBase64String(new byte[SynchronizationRequest.ExternalDataVersionMaxLength + 1])),
                "DataVersion"
            },
            {
                CreatePayload(number: new string('N', SynchronizationRequest.ExternalDocumentNumberMaxLength + 1)),
                "Number"
            },
            {
                CreatePayload(date: "not-a-date"),
                "Date"
            }
        };
    }

    [Theory]
    [MemberData(nameof(InvalidPayloads))]
    public async Task NotificationEndpoint_WhenPayloadInvalid_ReturnsFieldIdentifyingProblemDetails(
        Dictionary<string, object?> payload,
        string expectedField)
    {
        await using WebApplication app = CreateApp();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = CreateClient(app);
        using HttpRequestMessage request = CreateNotificationRequest(payload);

        using HttpResponseMessage response = await client.SendAsync(
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken);
        ValidationProblemDetails? problem =
            JsonSerializer.Deserialize<ValidationProblemDetails>(
                body,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(problem);
        Assert.Contains(expectedField, problem.Errors.Keys);

        Assert.DoesNotContain(ApiKey, body, StringComparison.Ordinal);
        Assert.DoesNotContain("FormatException", body, StringComparison.Ordinal);
        Assert.DoesNotContain("System.", body, StringComparison.Ordinal);
        Assert.Empty(await ReadRequestsAsync(app));
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
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<SynchronizationWakeUp>();
        builder.Services.AddSingleton<OneCChangeNotificationValidator>();
        builder.Services.AddScoped<SynchronizationRequestFactory>();
        builder.Services.AddScoped<SynchronizationRequestStore>();
        builder.Services.AddSingleton<SqlServerDuplicateSynchronizationRequestDetector>();
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

    private static Dictionary<string, object?> CreatePayload(
        string refKey = "80066011-d7c7-11ef-bac8-00155d01d112",
        string? dataVersion = null,
        string? number = "UT-00001004",
        string? date = "2025-01-21T10:15:36") =>
        new()
        {
            ["Ref_Key"] = refKey,
            ["DataVersion"] = dataVersion ?? Convert.ToBase64String([1, 2, 3]),
            ["Number"] = number,
            ["Date"] = date
        };

    private static HttpRequestMessage CreateNotificationRequest(
        Dictionary<string, object?> payload)
    {
        HttpRequestMessage request = new(
            HttpMethod.Post,
            "/api/integrations/1c/receiving-orders/changed")
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
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    private static HttpClient CreateClient(WebApplication app)
    {
        string address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.Single();
        return new HttpClient { BaseAddress = new Uri(address) };
    }
}
