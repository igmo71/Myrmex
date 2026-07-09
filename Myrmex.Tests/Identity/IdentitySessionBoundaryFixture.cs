using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Myrmex.AspNetCore.Security;
using Myrmex.Core.Application.Security;
using Myrmex.Identity.Infrastructure;
using Myrmex.Identity.Infrastructure.Sessions;
using Myrmex.Identity.Persistence;
using Myrmex.Shared.Identity;
using Myrmex.WebApp.Identity;
using System.Net.Http.Json;
using System.Security.Claims;

namespace Myrmex.Tests.Identity;

internal sealed class IdentitySessionBoundaryFixture : IAsyncDisposable
{
    private const string ApplicationName = "Myrmex.SessionBoundary.Tests";
    private readonly string _identityDatabaseName =
    $"myrmex-session-boundary-identity-{Guid.NewGuid():N}";

    private readonly string _rootPath = Path.Combine(
        Path.GetTempPath(),
        $"myrmex-session-boundary-{Guid.NewGuid():N}");
    private WebApplication? _api;
    private ServiceProvider? _webServices;

    public Uri ApiAddress { get; private set; } = null!;

    public static async Task<IdentitySessionBoundaryFixture> CreateAsync()
    {
        IdentitySessionBoundaryFixture fixture = new();
        Directory.CreateDirectory(fixture.KeyPath);
        await fixture.StartApiAsync();
        fixture.BuildWebServices();
        return fixture;
    }

    public async Task<MyrmexUser> CreateUserAsync(params string[] roles)
    {
        using IServiceScope scope = _webServices!.CreateScope();
        UserManager<MyrmexUser> users = scope.ServiceProvider.GetRequiredService<UserManager<MyrmexUser>>();
        RoleManager<MyrmexRole> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<MyrmexRole>>();
        foreach (string role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                IdentityResult roleResult = await roleManager.CreateAsync(new MyrmexRole(role));
                Assert.True(roleResult.Succeeded);
            }
        }

        string email = $"{Guid.NewGuid():N}@example.com";
        MyrmexUser user = new()
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };
        Assert.True((await users.CreateAsync(user)).Succeeded);
        if (roles.Length > 0)
        {
            Assert.True((await users.AddToRolesAsync(user, roles)).Succeeded);
        }

        return user;
    }

    public async Task RemoveRoleAsync(MyrmexUser user, string role)
    {
        using IServiceScope scope = _webServices!.CreateScope();
        UserManager<MyrmexUser> users = scope.ServiceProvider.GetRequiredService<UserManager<MyrmexUser>>();
        MyrmexUser persisted = (await users.FindByIdAsync(user.Id.ToString()))!;
        Assert.True((await users.RemoveFromRoleAsync(persisted, role)).Succeeded);
    }

    public Task<HttpResponseMessage> SendForUserAsync(Guid userId) =>
        SendWithPrincipalAsync(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            "Identity.Application")));

    public Task<HttpResponseMessage> SendAnonymousAsync() =>
        SendWithPrincipalAsync(new ClaimsPrincipal(new ClaimsIdentity()));

    public Task<HttpResponseMessage> SendMissingIdAsync() =>
        SendWithPrincipalAsync(new ClaimsPrincipal(new ClaimsIdentity([], "Identity.Application")));

    public async Task<HttpResponseMessage> SendRawTicketAsync(string ticket)
    {
        HttpClient client = new() { BaseAddress = ApiAddress };
        HttpRequestMessage request = new(HttpMethod.Get, "/actor");
        request.Headers.TryAddWithoutValidation(
            "Cookie",
            $"{MyrmexAuthenticationSchemes.ApiSessionCookieName}={ticket}");
        HttpResponseMessage response = await client.SendAsync(
            request,
            TestContext.Current.CancellationToken);
        client.Dispose();
        request.Dispose();
        return response;
    }

    public async Task<HttpResponseMessage> SendBrowserCookieAsync(string cookieValue)
    {
        HttpClient client = new() { BaseAddress = ApiAddress };
        HttpRequestMessage request = new(HttpMethod.Get, "/actor");
        request.Headers.TryAddWithoutValidation(
            "Cookie",
            $"{MyrmexAuthenticationSchemes.WebAppIdentityCookieName}={cookieValue}");
        HttpResponseMessage response = await client.SendAsync(
            request,
            TestContext.Current.CancellationToken);
        client.Dispose();
        request.Dispose();
        return response;
    }

    public string ProtectTicket(
        string applicationName = ApplicationName,
        string scheme = MyrmexAuthenticationSchemes.ApiSession,
        string? keyPath = null,
        DateTimeOffset? issuedUtc = null,
        DateTimeOffset? expiresUtc = null)
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddDataProtection()
            .SetApplicationName(applicationName)
            .PersistKeysToFileSystem(new DirectoryInfo(keyPath ?? KeyPath));
        services.AddAuthentication().AddCookie(scheme);
        using ServiceProvider provider = services.BuildServiceProvider();
        CookieAuthenticationOptions options = provider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(scheme);
        DateTimeOffset issued = issuedUtc ?? DateTimeOffset.UtcNow;
        AuthenticationTicket ticket = new(
            new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Role, IdentityRoleNames.WmsOperator)
                ],
                scheme)),
            new AuthenticationProperties
            {
                IssuedUtc = issued,
                ExpiresUtc = expiresUtc ?? issued.AddMinutes(2),
                IsPersistent = false,
                AllowRefresh = false
            },
            scheme);
        return options.TicketDataFormat.Protect(ticket);
    }

    public string CreateDifferentKeyPath()
    {
        string path = Path.Combine(_rootPath, $"wrong-key-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    public static async Task<BoundaryActorResponse?> ReadActorAsync(
        HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<BoundaryActorResponse>(
            TestContext.Current.CancellationToken);

    private string KeyPath => Path.Combine(_rootPath, "keys");

    private async Task<HttpResponseMessage> SendWithPrincipalAsync(ClaimsPrincipal principal)
    {
        using IServiceScope scope = _webServices!.CreateScope();

        MutableAuthenticationStateProvider authenticationState = scope.ServiceProvider
            .GetRequiredService<MutableAuthenticationStateProvider>();
        authenticationState.Principal = principal;

        IIdentityApiSessionTicketIssuer issuer = scope.ServiceProvider
            .GetRequiredService<IIdentityApiSessionTicketIssuer>();

        string? ticket = await issuer.IssueAsync(
            principal,
            TestContext.Current.CancellationToken);

        HttpClient client = new() { BaseAddress = ApiAddress };

        HttpRequestMessage request = new(HttpMethod.Get, "/actor");
        request.Headers.TryAddWithoutValidation(
            "Cookie",
            $"{MyrmexAuthenticationSchemes.ApiSessionCookieName}={ticket}");

        HttpResponseMessage response = await client.SendAsync(
            request,
            TestContext.Current.CancellationToken);

        client.Dispose();
        request.Dispose();

        return response;
    }

    private async Task StartApiAsync()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production
        });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        IConfiguration configuration = CreateConfiguration();
        builder.Configuration.AddConfiguration(configuration);
        builder.Services.AddDataProtection()
            .SetApplicationName(ApplicationName)
            .PersistKeysToFileSystem(new DirectoryInfo(KeyPath));
        builder.Services.AddMyrmexIdentityApiAuthentication(
            builder.Configuration,
            builder.Environment);
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IActorContext, HttpContextActorContext>();
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(
                MyrmexAuthorizationPolicies.WmsOperator,
                MyrmexAuthorizationPolicies.ConfigureWmsOperator);

        _api = builder.Build();
        _api.UseAuthentication();
        _api.UseAuthorization();
        _api.MapGet(
                "/actor",
                (HttpContext context, IActorContext actorContext) =>
                    new BoundaryActorResponse(
                        context.User.FindFirst(ClaimTypes.NameIdentifier)!.Value,
                        actorContext.ActorId,
                        context.User.FindAll(ClaimTypes.Role)
                            .Select(claim => claim.Value)
                            .ToArray()))
            .RequireAuthorization(MyrmexAuthorizationPolicies.WmsOperator);
        await _api.StartAsync(TestContext.Current.CancellationToken);
        string address = _api.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.Single();
        ApiAddress = new Uri(address);
    }

    private void BuildWebServices()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddDbContext<MyrmexIdentityDbContext>(options =>
            options.UseInMemoryDatabase(_identityDatabaseName));
        services.AddIdentityCore<MyrmexUser>()
            .AddRoles<MyrmexRole>()
            .AddSignInManager()
            .AddEntityFrameworkStores<MyrmexIdentityDbContext>();
        services.AddDataProtection()
            .SetApplicationName(ApplicationName)
            .PersistKeysToFileSystem(new DirectoryInfo(KeyPath));
        services.AddMyrmexIdentityApiAuthentication(
            CreateConfiguration(),
            new TestHostEnvironment());
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IIdentityApiSessionTicketIssuer, IdentityApiSessionTicketIssuer>();
        services.AddScoped<MutableAuthenticationStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(provider =>
            provider.GetRequiredService<MutableAuthenticationStateProvider>());
        services.AddTransient<IdentityApiAuthenticationHandler>();
        _webServices = services.BuildServiceProvider();
    }

    private static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Myrmex:Identity:ApiSession:LifetimeMinutes"] = "2"
        }).Build();

    public async ValueTask DisposeAsync()
    {
        if (_api is not null)
        {
            await _api.DisposeAsync();
        }

        if (_webServices is not null)
        {
            await _webServices.DisposeAsync();
        }

        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }

    internal sealed record BoundaryActorResponse(
        string UserId,
        string ActorId,
        string[] Roles);

    private sealed class MutableAuthenticationStateProvider : AuthenticationStateProvider
    {
        public ClaimsPrincipal Principal { get; set; } = new(new ClaimsIdentity());

        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(Principal));
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "Myrmex.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
