using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Myrmex.AspNetCore.Security;
using Myrmex.Identity.Infrastructure;

namespace Myrmex.Tests.Identity;

public sealed class IdentityHostAuthenticationTests
{
    [Fact]
    public void WebAppDefaultsToIdentityApplicationCookie()
    {
        ServiceCollection services = new();
        services.AddMyrmexIdentityWebAppAuthentication();

        using ServiceProvider provider = services.BuildServiceProvider();
        AuthenticationOptions options = provider
            .GetRequiredService<IOptions<AuthenticationOptions>>()
            .Value;
        CookieAuthenticationOptions cookie = provider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(MyrmexAuthenticationSchemes.WebAppIdentity);

        Assert.Equal(MyrmexAuthenticationSchemes.WebAppIdentity, options.DefaultScheme);
        Assert.Equal(MyrmexAuthenticationSchemes.WebAppIdentity, options.DefaultAuthenticateScheme);
        Assert.Equal(MyrmexAuthenticationSchemes.WebAppIdentity, options.DefaultChallengeScheme);
        Assert.Equal(MyrmexAuthenticationSchemes.WebAppIdentityCookieName, cookie.Cookie.Name);
        Assert.NotEqual(MyrmexAuthenticationSchemes.ApiSessionCookieName, cookie.Cookie.Name);
    }

    [Fact]
    public void ApiServiceDefaultsToTwoMinuteNonSlidingApiSessionCookie()
    {
        IConfiguration configuration = CreateConfiguration();
        ServiceCollection services = new();
        services.AddMyrmexIdentityApiAuthentication(
            configuration,
            new TestHostEnvironment(Environments.Production));

        using ServiceProvider provider = services.BuildServiceProvider();
        AuthenticationOptions options = provider
            .GetRequiredService<IOptions<AuthenticationOptions>>()
            .Value;
        CookieAuthenticationOptions cookie = provider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(MyrmexAuthenticationSchemes.ApiSession);

        Assert.Equal(MyrmexAuthenticationSchemes.ApiSession, options.DefaultScheme);
        Assert.Equal(MyrmexAuthenticationSchemes.ApiSession, options.DefaultAuthenticateScheme);
        Assert.Equal(MyrmexAuthenticationSchemes.ApiSession, options.DefaultChallengeScheme);
        Assert.Equal(TimeSpan.FromMinutes(2), cookie.ExpireTimeSpan);
        Assert.False(cookie.SlidingExpiration);
        Assert.Equal(MyrmexAuthenticationSchemes.ApiSessionCookieName, cookie.Cookie.Name);
    }

    [Fact]
    public async Task ApiServiceChallengeReturns401AndForbidReturns403()
    {
        await using WebApplication app = CreateApiApp(Environments.Production);
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = CreateClient(app);
        using HttpResponseMessage challenge = await client.GetAsync(
            "/challenge",
            TestContext.Current.CancellationToken);
        using HttpResponseMessage forbid = await client.GetAsync(
            "/forbidden",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, challenge.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbid.StatusCode);
        Assert.Null(challenge.Headers.Location);
        Assert.Null(forbid.Headers.Location);
    }

    private static WebApplication CreateApiApp(string environmentName)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(
            new WebApplicationOptions { EnvironmentName = environmentName });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Configuration.AddConfiguration(CreateConfiguration());
        builder.Services.AddMyrmexIdentityApiAuthentication(
            builder.Configuration,
            builder.Environment);
        builder.Services.AddAuthorization();

        WebApplication app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/challenge", () => Results.Ok())
            .RequireAuthorization();
        app.MapGet(
            "/forbidden",
            async (HttpContext context) =>
                await context.ForbidAsync(MyrmexAuthenticationSchemes.ApiSession));
        return app;
    }

    private static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Myrmex:Identity:ApiSession:LifetimeMinutes"] = "2"
            })
            .Build();

    private static HttpClient CreateClient(WebApplication app)
    {
        string address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.Single();
        return new HttpClient { BaseAddress = new Uri(address) };
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Myrmex.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
