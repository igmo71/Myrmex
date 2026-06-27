using System.Security.Claims;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Myrmex.Integrations.OneC.Endpoints;
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

    private static WebApplication CreateApp(IOneCODataClient client, bool authenticated)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(client);
        builder.Services.AddSingleton<TimeProvider>(new FixedTimeProvider(CheckedAtUtc));

        WebApplication app = builder.Build();
        if (authenticated)
        {
            app.Use(async (context, next) =>
            {
                context.User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim("sub", "operator")],
                    authenticationType: "Test"));
                await next();
            });
        }
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

        public Task TestConnectionAsync(CancellationToken cancellationToken)
        {
            CallCount++;
            return exception is null ? Task.CompletedTask : Task.FromException(exception);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
