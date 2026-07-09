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
using Myrmex.Shared.Identity;
using System.Net.Http.Json;

namespace Myrmex.Tests.AspNetCore.Security;

public sealed class DevelopmentActorAuthenticationTests
{
    private const string DevOperatorActorId = "11111111-1111-1111-1111-111111111111";

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
        await using WebApplication app = CreateApp(
            enabled: true,
            actorId: DevOperatorActorId,
            role: IdentityRoleNames.WmsOperator);

        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = CreateClient(app);

        DevelopmentActorResponse? response = await client.GetFromJsonAsync<DevelopmentActorResponse>(
            "/actor",
            TestContext.Current.CancellationToken);

        Assert.Equal(DevOperatorActorId, response?.ActorId);
        Assert.Equal(DevOperatorActorId, response?.Subject);
        Assert.Equal(DevOperatorActorId, response?.NameIdentifier);

        // Если Name теперь тоже заполняется GUID — оставить так.
        // Если Name остался отдельным display name — ожидание надо поправить под новую модель.
        Assert.Equal(DevOperatorActorId, response?.Name);
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

    private static WebApplication CreateApp(bool enabled, string? actorId, string? role = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        Dictionary<string, string?> configuration = new()
        {
            [$"{DevelopmentActorAuthenticationHandler.ConfigurationSectionName}:Enabled"] = enabled.ToString(),
            [$"{DevelopmentActorAuthenticationHandler.ConfigurationSectionName}:ActorId"] = actorId
        };

        if (!string.IsNullOrWhiteSpace(role))
        {
            configuration[$"{DevelopmentActorAuthenticationHandler.ConfigurationSectionName}:Roles:0"] = role;
        }

        builder.Configuration.AddInMemoryCollection(configuration);

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
