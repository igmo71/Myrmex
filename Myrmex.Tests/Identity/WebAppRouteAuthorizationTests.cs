using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Myrmex.AspNetCore.Security;
using Myrmex.Identity.Infrastructure;
using Myrmex.Identity.Persistence;
using Myrmex.Shared.Identity;

namespace Myrmex.Tests.Identity;

public sealed class WebAppRouteAuthorizationTests
{
    [Fact]
    public async Task ProtectedRoute_ForAnonymousUser_ChallengesToLogin()
    {
        await using RouteAuthorizationTestApp app =
            await RouteAuthorizationTestApp.CreateAsync();
        using HttpClient client = app.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            "/wms/protected",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.StartsWith(
            "/account/login",
            response.Headers.Location.PathAndQuery,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProtectedRoute_ForAuthenticatedOperator_ReturnsOk()
    {
        await using RouteAuthorizationTestApp app =
            await RouteAuthorizationTestApp.CreateAsync();
        using HttpClient client = app.CreateClient();
        await app.SignInAsync(client, IdentityRoleNames.WmsOperator);

        using HttpResponseMessage response = await client.GetAsync(
            "/wms/protected",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "protected",
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ProtectedRoute_ForAuthenticatedUserWithoutRequiredRole_RedirectsToAccessDenied()
    {
        await using RouteAuthorizationTestApp app =
            await RouteAuthorizationTestApp.CreateAsync();
        using HttpClient client = app.CreateClient();
        await app.SignInAsync(client, role: null);

        using HttpResponseMessage response = await client.GetAsync(
            "/wms/protected",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.StartsWith(
            "/account/access-denied",
            response.Headers.Location.PathAndQuery,
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/account/login", "login")]
    [InlineData("/account/access-denied", "access denied")]
    public async Task AccountRoutes_AreAnonymouslyAccessible(
        string route,
        string expectedContent)
    {
        await using RouteAuthorizationTestApp app =
            await RouteAuthorizationTestApp.CreateAsync();
        using HttpClient client = app.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            route,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            expectedContent,
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    private sealed class RouteAuthorizationTestApp : IAsyncDisposable
    {
        private readonly WebApplication _app;

        private RouteAuthorizationTestApp(WebApplication app, Uri baseAddress)
        {
            _app = app;
            BaseAddress = baseAddress;
        }

        private Uri BaseAddress { get; }

        public static async Task<RouteAuthorizationTestApp> CreateAsync()
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Services.AddLogging();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddDbContext<IdentityDbContext>(options =>
                options.UseInMemoryDatabase(
                    $"myrmex-route-authorization-{Guid.NewGuid():N}"));
            builder.Services.AddIdentityCore<AppUser>()
                .AddRoles<AppRole>()
                .AddSignInManager()
                .AddEntityFrameworkStores<IdentityDbContext>();
            builder.Services.AddMyrmexIdentityWebAppAuthentication();
            builder.Services.Configure<CookieAuthenticationOptions>(
                MyrmexAuthenticationSchemes.WebAppIdentity,
                options => options.Cookie.SecurePolicy = CookieSecurePolicy.None);
            builder.Services.AddAuthorizationBuilder()
                .AddPolicy(
                    MyrmexAuthorizationPolicies.WmsOperator,
                    MyrmexAuthorizationPolicies.ConfigureWmsOperator)
                .AddPolicy(
                    MyrmexAuthorizationPolicies.MyrmexAdmin,
                    MyrmexAuthorizationPolicies.ConfigureMyrmexAdmin);

            WebApplication app = builder.Build();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapGet("/account/login", () => "login")
                .AllowAnonymous();
            app.MapGet("/account/access-denied", () => "access denied")
                .AllowAnonymous();
            app.MapGet("/wms/protected", () => "protected")
                .RequireAuthorization(MyrmexAuthorizationPolicies.WmsOperator);
            app.MapPost(
                    "/test/sign-in",
                    async (
                        HttpContext context,
                        SignInManager<AppUser> signInManager,
                        UserManager<AppUser> userManager,
                        RoleManager<AppRole> roleManager) =>
                    {
                        string? role = context.Request.Query["role"];
                        AppUser user = new()
                        {
                            Id = Guid.NewGuid(),
                            UserName = $"{Guid.NewGuid():N}@example.com",
                            Email = $"{Guid.NewGuid():N}@example.com",
                            EmailConfirmed = true
                        };

                        IdentityResult createUser = await userManager.CreateAsync(user);
                        Assert.True(createUser.Succeeded);

                        if (!string.IsNullOrWhiteSpace(role))
                        {
                            IdentityResult createRole = await roleManager.CreateAsync(
                                new AppRole(role));
                            Assert.True(createRole.Succeeded);

                            IdentityResult addToRole = await userManager.AddToRoleAsync(
                                user,
                                role);
                            Assert.True(addToRole.Succeeded);
                        }

                        await signInManager.SignInAsync(user, isPersistent: false);

                        return Results.Ok(user.Id);
                    })
                .AllowAnonymous();

            await app.StartAsync(TestContext.Current.CancellationToken);
            string address = app.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!
                .Addresses.Single();
            return new RouteAuthorizationTestApp(app, new Uri(address));
        }

        public HttpClient CreateClient()
        {
            HttpClientHandler handler = new()
            {
                AllowAutoRedirect = false,
                CookieContainer = new CookieContainer()
            };
            return new HttpClient(handler) { BaseAddress = BaseAddress };
        }

        public async Task SignInAsync(HttpClient client, string? role)
        {
            string path = role is null
                ? "/test/sign-in"
                : $"/test/sign-in?role={Uri.EscapeDataString(role)}";

            using HttpResponseMessage response = await client.PostAsync(
                path,
                content: null,
                TestContext.Current.CancellationToken);
            response.EnsureSuccessStatusCode();
        }

        public async ValueTask DisposeAsync()
        {
            await _app.DisposeAsync();
        }
    }
}
