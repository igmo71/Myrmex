using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Myrmex.AspNetCore.Security;
using Myrmex.Identity.Infrastructure;
using Myrmex.Identity.Infrastructure.Sessions;
using Myrmex.Identity.Persistence;
using Myrmex.Shared.Identity;
using System.Security.Claims;

namespace Myrmex.Tests.Identity;

public sealed class IdentityApiSessionTicketIssuerTests
{
    [Fact]
    public async Task IssueAsync_ReloadsCurrentPersistentRoles()
    {
        await using ServiceProvider provider = CreateProvider();

        using IServiceScope scope = provider.CreateScope();

        UserManager<MyrmexUser> users = scope.ServiceProvider.GetRequiredService<UserManager<MyrmexUser>>();
        RoleManager<MyrmexRole> roles = scope.ServiceProvider.GetRequiredService<RoleManager<MyrmexRole>>();

        MyrmexUser user = await CreateUserAsync(scope.ServiceProvider, IdentityRoleNames.WmsOperator);

        if (!await roles.RoleExistsAsync(IdentityRoleNames.MyrmexAdmin))
        {
            IdentityResult roleResult = await roles.CreateAsync(
                new MyrmexRole(IdentityRoleNames.MyrmexAdmin));

            Assert.True(roleResult.Succeeded);
        }

        IIdentityApiSessionTicketIssuer issuer = scope.ServiceProvider
            .GetRequiredService<IIdentityApiSessionTicketIssuer>();

        AuthenticationTicket first = Unprotect(
            scope.ServiceProvider,
            (await issuer.IssueAsync(CreatePrincipal(user.Id)))!);

        await users.RemoveFromRoleAsync(user, IdentityRoleNames.WmsOperator);
        await users.AddToRoleAsync(user, IdentityRoleNames.MyrmexAdmin);

        AuthenticationTicket second = Unprotect(
            scope.ServiceProvider,
            (await issuer.IssueAsync(CreatePrincipal(user.Id)))!);

        Assert.Equal([IdentityRoleNames.WmsOperator], GetRoles(first));
        Assert.Equal([IdentityRoleNames.MyrmexAdmin], GetRoles(second));
    }

    [Fact]
    public async Task IssueAsync_DeletedOrLockedOutUser_ReturnsNoTicket()
    {
        await using ServiceProvider provider = CreateProvider();
        using IServiceScope scope = provider.CreateScope();
        UserManager<MyrmexUser> users = scope.ServiceProvider.GetRequiredService<UserManager<MyrmexUser>>();
        IIdentityApiSessionTicketIssuer issuer = scope.ServiceProvider
            .GetRequiredService<IIdentityApiSessionTicketIssuer>();
        MyrmexUser deleted = await CreateUserAsync(scope.ServiceProvider, IdentityRoleNames.WmsOperator);
        MyrmexUser locked = await CreateUserAsync(scope.ServiceProvider, IdentityRoleNames.WmsOperator);
        await users.DeleteAsync(deleted);
        await users.SetLockoutEnabledAsync(locked, true);
        await users.SetLockoutEndDateAsync(locked, DateTimeOffset.UtcNow.AddHours(1));

        Assert.Null(await issuer.IssueAsync(CreatePrincipal(deleted.Id)));
        Assert.Null(await issuer.IssueAsync(CreatePrincipal(locked.Id)));
    }

    [Fact]
    public async Task IssueAsync_CreatesMinimalTwoMinuteApiSessionTicket()
    {
        DateTimeOffset now = new(2026, 7, 8, 10, 0, 0, TimeSpan.Zero);
        await using ServiceProvider provider = CreateProvider(new FixedTimeProvider(now));
        using IServiceScope scope = provider.CreateScope();
        MyrmexUser user = await CreateUserAsync(scope.ServiceProvider, IdentityRoleNames.WmsOperator);
        IIdentityApiSessionTicketIssuer issuer = scope.ServiceProvider
            .GetRequiredService<IIdentityApiSessionTicketIssuer>();
        ClaimsPrincipal browserPrincipal = CreatePrincipal(
            user.Id,
            new Claim(ClaimTypes.Email, "operator@example.com"),
            new Claim(ClaimTypes.Name, "Operator"),
            new Claim("sub", user.Id.ToString()),
            new Claim(ClaimTypes.Role, IdentityRoleNames.MyrmexAdmin),
            new Claim("password", "must-not-propagate"),
            new Claim("browser-cookie", ".Myrmex.Identity.Application=secret"));

        string protectedTicket = (await issuer.IssueAsync(browserPrincipal))!;
        AuthenticationTicket ticket = Unprotect(scope.ServiceProvider, protectedTicket);

        Assert.Equal(MyrmexAuthenticationSchemes.ApiSession, ticket.AuthenticationScheme);
        Assert.Equal(now, ticket.Properties.IssuedUtc);
        Assert.Equal(now.AddMinutes(2), ticket.Properties.ExpiresUtc);
        Assert.False(ticket.Properties.IsPersistent);
        Assert.Equal(user.Id.ToString(), Assert.Single(
            ticket.Principal.FindAll(ClaimTypes.NameIdentifier)).Value);
        Assert.Equal([IdentityRoleNames.WmsOperator], GetRoles(ticket));
        Assert.DoesNotContain(ticket.Principal.Claims, claim =>
            claim.Type is ClaimTypes.Email or ClaimTypes.Name or "sub" or "password" or "browser-cookie");
    }

    [Fact]
    public async Task IssueAsync_WhenCancelled_ThrowsCancellation()
    {
        await using ServiceProvider provider = CreateProvider();
        using IServiceScope scope = provider.CreateScope();
        IIdentityApiSessionTicketIssuer issuer = scope.ServiceProvider
            .GetRequiredService<IIdentityApiSessionTicketIssuer>();
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            issuer.IssueAsync(CreatePrincipal(Guid.NewGuid()), cancellation.Token));
    }

    [Fact]
    public async Task IssueAsync_WithoutExactlyOneNonEmptyGuidUserId_ReturnsNoTicket()
    {
        await using ServiceProvider provider = CreateProvider();
        using IServiceScope scope = provider.CreateScope();
        MyrmexUser user = await CreateUserAsync(scope.ServiceProvider, IdentityRoleNames.WmsOperator);
        IIdentityApiSessionTicketIssuer issuer = scope.ServiceProvider
            .GetRequiredService<IIdentityApiSessionTicketIssuer>();
        ClaimsPrincipal duplicate = new(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            ],
            "Identity.Application"));

        Assert.Null(await issuer.IssueAsync(new ClaimsPrincipal(new ClaimsIdentity())));
        Assert.Null(await issuer.IssueAsync(new ClaimsPrincipal(
            new ClaimsIdentity([], "Identity.Application"))));
        Assert.Null(await issuer.IssueAsync(CreatePrincipal(Guid.Empty)));
        Assert.Null(await issuer.IssueAsync(duplicate));
    }

    private static ServiceProvider CreateProvider(TimeProvider? timeProvider = null)
    {
        string identityDatabaseName =
            $"myrmex-session-ticket-identity-{Guid.NewGuid():N}";

        ServiceCollection services = new();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddDbContext<MyrmexIdentityDbContext>(options =>
            options.UseInMemoryDatabase(identityDatabaseName));
        services.AddIdentityCore<MyrmexUser>()
            .AddRoles<MyrmexRole>()
            .AddSignInManager()
            .AddEntityFrameworkStores<MyrmexIdentityDbContext>();
        services.AddDataProtection();
        services.AddMyrmexIdentityApiAuthentication(CreateConfiguration());
        services.AddSingleton(timeProvider ?? TimeProvider.System);
        services.AddScoped<IIdentityApiSessionTicketIssuer, IdentityApiSessionTicketIssuer>();
        return services.BuildServiceProvider();
    }

    private static async Task<MyrmexUser> CreateUserAsync(
        IServiceProvider services,
        string role)
    {
        UserManager<MyrmexUser> users = services.GetRequiredService<UserManager<MyrmexUser>>();
        RoleManager<MyrmexRole> roles = services.GetRequiredService<RoleManager<MyrmexRole>>();
        if (!await roles.RoleExistsAsync(role))
        {
            Assert.True((await roles.CreateAsync(new MyrmexRole(role))).Succeeded);
        }

        string email = $"{Guid.NewGuid():N}@example.com";
        MyrmexUser user = new()
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            PasswordHash = "sensitive-password-hash"
        };
        Assert.True((await users.CreateAsync(user)).Succeeded);
        Assert.True((await users.AddToRoleAsync(user, role)).Succeeded);
        return user;
    }

    private static ClaimsPrincipal CreatePrincipal(Guid userId, params Claim[] additionalClaims)
    {
        Claim[] claims = [new(ClaimTypes.NameIdentifier, userId.ToString()), .. additionalClaims];
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Identity.Application"));
    }

    private static AuthenticationTicket Unprotect(IServiceProvider services, string value) =>
        services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(MyrmexAuthenticationSchemes.ApiSession)
            .TicketDataFormat.Unprotect(value)!;

    private static string[] GetRoles(AuthenticationTicket ticket) =>
        ticket.Principal.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray();

    private static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Myrmex:Identity:ApiSession:LifetimeMinutes"] = "2"
        }).Build();

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

}
