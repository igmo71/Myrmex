using Microsoft.AspNetCore.Antiforgery;
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
using Myrmex.WebApp.Identity;
using System.Net.Http.Json;
using System.Security.Claims;

namespace Myrmex.Tests.Identity;

public sealed class WebAppAccountFlowTests
{
    private const string Password = "Myrmex1!";

    [Fact]
    public async Task Login_WithExistingUserAndValidPassword_IssuesApplicationCookie()
    {
        await using AccountTestApp app = await AccountTestApp.CreateAsync();
        AppUser user = await app.CreateUserAsync();
        using HttpClient client = app.CreateClient();
        AntiforgeryPayload antiforgery = await app.GetAntiforgeryAsync(client);

        using HttpResponseMessage login = await client.PostAsync(
            "/account/login-submit",
            CreateLoginForm(antiforgery, user.Email!, Password, "/protected"),
            TestContext.Current.CancellationToken);
        using HttpResponseMessage protectedResponse = await client.GetAsync(
            "/protected",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal("/protected", login.Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.OK, protectedResponse.StatusCode);
        Assert.Equal(
            user.Id.ToString(),
            await protectedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Login_WithInvalidPassword_DoesNotIssueApplicationCookie()
    {
        await using AccountTestApp app = await AccountTestApp.CreateAsync();
        AppUser user = await app.CreateUserAsync();
        using HttpClient client = app.CreateClient();
        AntiforgeryPayload antiforgery = await app.GetAntiforgeryAsync(client);

        using HttpResponseMessage login = await client.PostAsync(
            "/account/login-submit",
            CreateLoginForm(antiforgery, user.Email!, "wrong-password", "/protected"),
            TestContext.Current.CancellationToken);
        using HttpResponseMessage protectedResponse = await client.GetAsync(
            "/protected",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.StartsWith(
            "/account/login?loginFailed=true",
            login.Headers.Location?.OriginalString,
            StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.Redirect, protectedResponse.StatusCode);

        Assert.NotNull(protectedResponse.Headers.Location);

        Assert.StartsWith(
            "/account/login",
            protectedResponse.Headers.Location.PathAndQuery,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Login_WithExternalReturnUrl_RedirectsToHome()
    {
        await using AccountTestApp app = await AccountTestApp.CreateAsync();
        AppUser user = await app.CreateUserAsync();
        using HttpClient client = app.CreateClient();
        AntiforgeryPayload antiforgery = await app.GetAntiforgeryAsync(client);

        using HttpResponseMessage login = await client.PostAsync(
            "/account/login-submit",
            CreateLoginForm(
                antiforgery,
                user.Email!,
                Password,
                "https://example.com/evil"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal("/", login.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Logout_ClearsApplicationCookie()
    {
        await using AccountTestApp app = await AccountTestApp.CreateAsync();
        AppUser user = await app.CreateUserAsync();
        using HttpClient client = app.CreateClient();
        AntiforgeryPayload loginAntiforgery = await app.GetAntiforgeryAsync(client);

        using HttpResponseMessage login = await client.PostAsync(
            "/account/login-submit",
            CreateLoginForm(loginAntiforgery, user.Email!, Password, "/protected"),
            TestContext.Current.CancellationToken);
        login.EnsureSuccessOrRedirect();

        AntiforgeryPayload logoutAntiforgery = await app.GetAntiforgeryAsync(client);
        using HttpResponseMessage logout = await client.PostAsync(
            "/account/logout-submit",
            CreateLogoutForm(logoutAntiforgery, "/"),
            TestContext.Current.CancellationToken);
        using HttpResponseMessage protectedResponse = await client.GetAsync(
            "/protected",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        Assert.Equal("/", logout.Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.Redirect, protectedResponse.StatusCode);

        Assert.NotNull(protectedResponse.Headers.Location);

        Assert.StartsWith(
            "/account/login",
            protectedResponse.Headers.Location.PathAndQuery,
            StringComparison.Ordinal);
    }

    private static FormUrlEncodedContent CreateLoginForm(
        AntiforgeryPayload antiforgery,
        string userNameOrEmail,
        string password,
        string returnUrl) =>
        new(new Dictionary<string, string>
        {
            [antiforgery.FieldName] = antiforgery.Token,
            ["UserNameOrEmail"] = userNameOrEmail,
            ["Password"] = password,
            ["ReturnUrl"] = returnUrl
        });

    private static FormUrlEncodedContent CreateLogoutForm(
        AntiforgeryPayload antiforgery,
        string returnUrl) =>
        new(new Dictionary<string, string>
        {
            [antiforgery.FieldName] = antiforgery.Token,
            ["ReturnUrl"] = returnUrl
        });

    private sealed class AccountTestApp : IAsyncDisposable
    {
        private readonly WebApplication _app;

        private AccountTestApp(WebApplication app, Uri baseAddress)
        {
            _app = app;
            BaseAddress = baseAddress;
        }

        private Uri BaseAddress { get; }

        public static async Task<AccountTestApp> CreateAsync()
        {
            string databaseName = $"myrmex-account-flow-{Guid.NewGuid():N}";

            WebApplicationBuilder builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Services.AddLogging();
            builder.Services.AddDbContext<IdentityDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));
            builder.Services.AddIdentityCore<AppUser>()
                .AddRoles<AppRole>()
                .AddSignInManager()
                .AddEntityFrameworkStores<IdentityDbContext>();
            builder.Services.AddMyrmexIdentityWebAppAuthentication();
            builder.Services.Configure<CookieAuthenticationOptions>(
                MyrmexAuthenticationSchemes.WebAppIdentity,
                options => options.Cookie.SecurePolicy = CookieSecurePolicy.None);
            builder.Services.AddAuthorization();
            builder.Services.AddAntiforgery();

            WebApplication app = builder.Build();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseAntiforgery();
            app.MapMyrmexIdentityWebAppAccountEndpoints();
            app.MapGet(
                    "/test/antiforgery",
                    (HttpContext context, IAntiforgery antiforgery) =>
                    {
                        AntiforgeryTokenSet tokens = antiforgery.GetAndStoreTokens(context);
                        return Results.Json(new AntiforgeryPayload(
                            tokens.FormFieldName!,
                            tokens.RequestToken!));
                    })
                .AllowAnonymous();
            app.MapGet(
                    "/protected",
                    (HttpContext context) =>
                        context.User.FindFirst(ClaimTypes.NameIdentifier)!.Value)
                .RequireAuthorization();

            await app.StartAsync(TestContext.Current.CancellationToken);
            string address = app.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!
                .Addresses.Single();
            return new AccountTestApp(app, new Uri(address));
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

        public async Task<AntiforgeryPayload> GetAntiforgeryAsync(
            HttpClient client)
        {
            AntiforgeryPayload? payload = await client
                .GetFromJsonAsync<AntiforgeryPayload>(
                    "/test/antiforgery",
                    TestContext.Current.CancellationToken);
            return payload ?? throw new InvalidOperationException(
                "The antiforgery endpoint did not return a token.");
        }

        public async Task<AppUser> CreateUserAsync()
        {
            using IServiceScope scope = _app.Services.CreateScope();
            UserManager<AppUser> userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<AppUser>>();
            string email = $"{Guid.NewGuid():N}@example.com";
            AppUser user = new()
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };
            IdentityResult result = await userManager.CreateAsync(user, Password);
            Assert.True(result.Succeeded);
            return user;
        }

        public async ValueTask DisposeAsync()
        {
            await _app.DisposeAsync();
        }
    }

    private sealed record AntiforgeryPayload(string FieldName, string Token);
}

internal static class HttpResponseMessageTestExtensions
{
    public static void EnsureSuccessOrRedirect(this HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode &&
            response.StatusCode != HttpStatusCode.Redirect)
        {
            response.EnsureSuccessStatusCode();
        }
    }
}
