using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Myrmex.AspNetCore.Security;
using Myrmex.Core.Application.Security;
using System.Net.Http.Json;

namespace Myrmex.Tests.AspNetCore.Security;

public sealed class DevelopmentActorAuthenticationTests
{
    [Fact]
    public async Task ProtectedEndpoint_WhenDevelopmentActorDisabled_Returns401()
    {
        await using WebApplication app = CreateApp(enabled: false, actorId: "dev-operator");
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = CreateClient(app);
        using HttpResponseMessage response = await client.GetAsync(
            "/actor",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WhenDevelopmentActorEnabled_ProvidesActorClaims()
    {
        await using WebApplication app = CreateApp(enabled: true, actorId: "dev-operator");
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = CreateClient(app);
        DevelopmentActorResponse? response = await client.GetFromJsonAsync<DevelopmentActorResponse>(
            "/actor",
            TestContext.Current.CancellationToken);

        Assert.Equal("dev-operator", response?.ActorId);
        Assert.Equal("dev-operator", response?.Subject);
        Assert.Equal("dev-operator", response?.NameIdentifier);
        Assert.Equal("dev-operator", response?.Name);
    }

    [Fact]
    public async Task ProtectedEndpoint_WhenActorIdIsMissing_Returns401()
    {
        await using WebApplication app = CreateApp(enabled: true, actorId: null);
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = CreateClient(app);
        using HttpResponseMessage response = await client.GetAsync(
            "/actor",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static WebApplication CreateApp(bool enabled, string? actorId)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{DevelopmentActorAuthenticationHandler.ConfigurationSectionName}:Enabled"] = enabled.ToString(),
            [$"{DevelopmentActorAuthenticationHandler.ConfigurationSectionName}:ActorId"] = actorId
        });
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IActorContext, HttpContextActorContext>();
        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = MyrmexAuthenticationSchemes.DevelopmentActor;
                options.DefaultChallengeScheme = MyrmexAuthenticationSchemes.DevelopmentActor;
            })
            .AddScheme<AuthenticationSchemeOptions, DevelopmentActorAuthenticationHandler>(
                MyrmexAuthenticationSchemes.DevelopmentActor,
                _ => { });
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(
                MyrmexAuthorizationPolicies.WmsOperator,
                MyrmexAuthorizationPolicies.ConfigureWmsOperator);
        });

        WebApplication app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet(
                "/actor",
                (IActorContext actorContext, HttpContext context) => new DevelopmentActorResponse(
                    actorContext.ActorId,
                    context.User.FindFirst("sub")?.Value,
                    context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    context.User.Identity?.Name))
            .RequireAuthorization(MyrmexAuthorizationPolicies.WmsOperator);
        return app;
    }

    private static HttpClient CreateClient(WebApplication app)
    {
        string address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.Single();
        return new HttpClient { BaseAddress = new Uri(address) };
    }

    private sealed record DevelopmentActorResponse(
        string ActorId,
        string? Subject,
        string? NameIdentifier,
        string? Name);
}
