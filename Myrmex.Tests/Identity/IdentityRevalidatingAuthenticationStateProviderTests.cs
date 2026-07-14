using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Myrmex.Identity.Persistence;
using Myrmex.WebApp.Identity;
using System.Security.Claims;

namespace Myrmex.Tests.Identity;

public sealed class IdentityRevalidatingAuthenticationStateProviderTests
{
    [Fact]
    public async Task ValidateAuthenticationStateAsync_WithDeletedUser_ReturnsFalse()
    {
        await using RevalidationTestHost host =
            RevalidationTestHost.Create();
        AppUser user = await host.CreateUserAsync();
        ClaimsPrincipal principal = await host.CreatePrincipalAsync(user.Id);

        await host.DeleteUserAsync(user.Id);

        Assert.False(await host.ValidateAsync(principal));
    }

    [Fact]
    public async Task ValidateAuthenticationStateAsync_WithChangedSecurityStamp_ReturnsFalse()
    {
        await using RevalidationTestHost host =
            RevalidationTestHost.Create();
        AppUser user = await host.CreateUserAsync();
        ClaimsPrincipal principal = await host.CreatePrincipalAsync(user.Id);

        await host.UpdateSecurityStampAsync(user.Id);

        Assert.False(await host.ValidateAsync(principal));
    }

    [Fact]
    public async Task ValidateAuthenticationStateAsync_RevalidatesAndRejectsStaleCircuitPrincipal()
    {
        await using RevalidationTestHost host =
            RevalidationTestHost.Create();
        AppUser user = await host.CreateUserAsync();
        ClaimsPrincipal principal = await host.CreatePrincipalAsync(user.Id);

        Assert.True(await host.ValidateAsync(principal));

        await host.UpdateSecurityStampAsync(user.Id);

        Assert.False(await host.ValidateAsync(principal));
    }

    [Theory]
    [MemberData(nameof(InvalidStableIdentityPrincipals))]
    public async Task ValidateAuthenticationStateAsync_WithInvalidStableUserId_ReturnsFalse(
        ClaimsPrincipal principal)
    {
        await using RevalidationTestHost host =
            RevalidationTestHost.Create();

        Assert.False(await host.ValidateAsync(principal));
    }

    public static TheoryData<ClaimsPrincipal> InvalidStableIdentityPrincipals()
    {
        TheoryData<ClaimsPrincipal> data = [];
        data.Add(CreatePrincipal());
        data.Add(CreatePrincipal(new Claim(ClaimTypes.NameIdentifier, "")));
        data.Add(CreatePrincipal(new Claim(ClaimTypes.NameIdentifier, "not-a-guid")));
        data.Add(CreatePrincipal(new Claim(ClaimTypes.NameIdentifier, Guid.Empty.ToString())));
        data.Add(CreatePrincipal(
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())));
        return data;
    }

    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "Identity.Application"));

    private sealed class RevalidationTestHost : IAsyncDisposable
    {
        private readonly ServiceProvider _services;
        private readonly TestableIdentityRevalidatingAuthenticationStateProvider _provider;

        private RevalidationTestHost(ServiceProvider services)
        {
            _services = services;
            _provider = new TestableIdentityRevalidatingAuthenticationStateProvider(
                _services.GetRequiredService<IServiceScopeFactory>());
        }

        public static RevalidationTestHost Create()
        {
            string databaseName = $"myrmex-revalidation-{Guid.NewGuid():N}";

            ServiceCollection services = [];
            services.AddLogging();
            services.AddHttpContextAccessor();

            services
                .AddAuthentication()
                .AddCookie(IdentityConstants.ApplicationScheme);

            services.AddDbContext<IdentityDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));
            services.AddIdentityCore<AppUser>()
                .AddRoles<AppRole>()
                .AddSignInManager()
                .AddEntityFrameworkStores<IdentityDbContext>();

            return new RevalidationTestHost(services.BuildServiceProvider());
        }

        public async Task<AppUser> CreateUserAsync()
        {
            using IServiceScope scope = _services.CreateScope();
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

            IdentityResult result = await userManager.CreateAsync(user);
            Assert.True(result.Succeeded);

            return user;
        }

        public async Task<ClaimsPrincipal> CreatePrincipalAsync(Guid userId)
        {
            using IServiceScope scope = _services.CreateScope();
            UserManager<AppUser> userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<AppUser>>();
            SignInManager<AppUser> signInManager = scope.ServiceProvider
                .GetRequiredService<SignInManager<AppUser>>();
            AppUser user = await userManager.FindByIdAsync(userId.ToString())
                ?? throw new InvalidOperationException("Test user was not found.");

            return await signInManager.CreateUserPrincipalAsync(user);
        }

        public async Task DeleteUserAsync(Guid userId)
        {
            using IServiceScope scope = _services.CreateScope();
            UserManager<AppUser> userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<AppUser>>();
            AppUser user = await userManager.FindByIdAsync(userId.ToString())
                ?? throw new InvalidOperationException("Test user was not found.");

            IdentityResult result = await userManager.DeleteAsync(user);
            Assert.True(result.Succeeded);
        }

        public async Task UpdateSecurityStampAsync(Guid userId)
        {
            using IServiceScope scope = _services.CreateScope();
            UserManager<AppUser> userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<AppUser>>();
            AppUser user = await userManager.FindByIdAsync(userId.ToString())
                ?? throw new InvalidOperationException("Test user was not found.");

            IdentityResult result = await userManager.UpdateSecurityStampAsync(user);
            Assert.True(result.Succeeded);
        }

        public Task<bool> ValidateAsync(ClaimsPrincipal principal) =>
            _provider.ValidateAsync(
                new AuthenticationState(principal),
                TestContext.Current.CancellationToken);

        public ValueTask DisposeAsync() =>
            _services.DisposeAsync();
    }

    private sealed class TestableIdentityRevalidatingAuthenticationStateProvider(
        IServiceScopeFactory scopeFactory)
        : IdentityRevalidatingAuthenticationStateProvider(
            scopeFactory,
            NullLoggerFactory.Instance)
    {
        public Task<bool> ValidateAsync(
            AuthenticationState authenticationState,
            CancellationToken cancellationToken) =>
            ValidateAuthenticationStateAsync(
                authenticationState,
                cancellationToken);
    }
}
