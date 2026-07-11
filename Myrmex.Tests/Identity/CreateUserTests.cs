using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Myrmex.Core.Results;
using Myrmex.Identity.Application.Users;
using Myrmex.Identity.Persistence;
using Myrmex.Shared.Identity;

namespace Myrmex.Tests.Identity;

public sealed class CreateUserTests
{
    private const string ValidPassword = "Myrmex1!";

    [Fact]
    public async Task HandleAsync_NormalizesEmailAndAssignsSupportedRoles()
    {
        await using CreateUserTestHost host = CreateUserTestHost.Create();
        await host.EnsureSupportedRolesAsync();

        ServiceResult<IdentityUserDetails> result = await host.HandleAsync(
            new CreateUser.Command(
                " Admin@Example.COM ",
                " Administrator ",
                ValidPassword,
                [IdentityRoleNames.WmsOperator, IdentityRoleNames.MyrmexAdmin]));

        Assert.True(result.IsSuccess);
        Assert.Equal("Admin@Example.COM", result.Value.Email);
        Assert.Equal("Administrator", result.Value.DisplayName);
        Assert.Equal(
            [IdentityRoleNames.WmsOperator, IdentityRoleNames.MyrmexAdmin],
            result.Value.Roles);
        MyrmexUser user = await host.FindByEmailAsync("admin@example.com");
        Assert.Equal("ADMIN@EXAMPLE.COM", user.NormalizedEmail);
        Assert.Equal("ADMIN@EXAMPLE.COM", user.NormalizedUserName);
        Assert.True(await host.UserIsInRoleAsync(user.Email!, IdentityRoleNames.WmsOperator));
        Assert.True(await host.UserIsInRoleAsync(user.Email!, IdentityRoleNames.MyrmexAdmin));
    }

    [Fact]
    public async Task HandleAsync_WhenDuplicateEmail_ReturnsConflictWithoutCreatingUser()
    {
        await using CreateUserTestHost host = CreateUserTestHost.Create();
        await host.EnsureSupportedRolesAsync();
        await host.CreateExistingUserAsync("admin@example.com");

        ServiceResult<IdentityUserDetails> result = await host.HandleAsync(
            new CreateUser.Command(
                "ADMIN@example.com",
                null,
                ValidPassword,
                [IdentityRoleNames.WmsOperator]));

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorType.Conflict, result.Error.Type);
        Assert.Equal("IdentityUser.Duplicate", result.Error.Code);
        Assert.Equal(1, await host.CountUsersAsync());
    }

    [Fact]
    public async Task HandleAsync_WhenPasswordInvalid_ReturnsValidationWithoutPersistingUser()
    {
        await using CreateUserTestHost host = CreateUserTestHost.Create();
        await host.EnsureSupportedRolesAsync();
        const string invalidPassword = "short";

        ServiceResult<IdentityUserDetails> result = await host.HandleAsync(
            new CreateUser.Command(
                "operator@example.com",
                null,
                invalidPassword,
                [IdentityRoleNames.WmsOperator]));

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorType.Invalid, result.Error.Type);
        Assert.Equal(0, await host.CountUsersAsync());
        string errorText = string.Join(
            Environment.NewLine,
            result.Error.DetailList.Select(error => $"{error.Code} {error.Message}"));
        Assert.DoesNotContain(invalidPassword, errorText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_WhenRoleMissing_ReturnsFailureAndRollsBackCreatedUser()
    {
        await using CreateUserTestHost host = CreateUserTestHost.Create();
        await host.EnsureRoleAsync(IdentityRoleNames.WmsOperator);

        ServiceResult<IdentityUserDetails> result = await host.HandleAsync(
            new CreateUser.Command(
                "operator@example.com",
                null,
                ValidPassword,
                [IdentityRoleNames.WmsOperator, IdentityRoleNames.MyrmexAdmin]));

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorType.Failure, result.Error.Type);
        Assert.Equal("IdentityUser.RoleAssignmentFailed", result.Error.Code);
        Assert.Equal(0, await host.CountUsersAsync());
    }

    [Fact]
    public async Task HandleAsync_WhenRolesAreEmptyOrUnsupported_ReturnsValidation()
    {
        await using CreateUserTestHost host = CreateUserTestHost.Create();

        ServiceResult<IdentityUserDetails> emptyRoleResult = await host.HandleAsync(
            new CreateUser.Command(
                "operator@example.com",
                null,
                ValidPassword,
                []));
        ServiceResult<IdentityUserDetails> unsupportedRoleResult = await host.HandleAsync(
            new CreateUser.Command(
                "admin@example.com",
                null,
                ValidPassword,
                ["Unsupported"]));

        Assert.False(emptyRoleResult.IsSuccess);
        Assert.Equal(ServiceErrorType.Invalid, emptyRoleResult.Error.Type);
        Assert.Contains(
            emptyRoleResult.Error.DetailList,
            error => error.Code == "IdentityUser.RolesRequired");
        Assert.False(unsupportedRoleResult.IsSuccess);
        Assert.Equal(ServiceErrorType.Invalid, unsupportedRoleResult.Error.Type);
        Assert.Contains(
            unsupportedRoleResult.Error.DetailList,
            error => error.Code == "IdentityUser.RoleUnsupported");
        Assert.Equal(0, await host.CountUsersAsync());
    }

    [Fact]
    public async Task HandleAsync_WhenCancelledBeforeWork_ThrowsAndDoesNotPersistUser()
    {
        await using CreateUserTestHost host = CreateUserTestHost.Create();
        await host.EnsureSupportedRolesAsync();
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            host.HandleAsync(
                new CreateUser.Command(
                    "operator@example.com",
                    null,
                    ValidPassword,
                    [IdentityRoleNames.WmsOperator]),
                cancellation.Token));
        Assert.Equal(0, await host.CountUsersAsync());
    }

    private sealed class CreateUserTestHost : IAsyncDisposable
    {
        private readonly ServiceProvider _services;

        private CreateUserTestHost(ServiceProvider services)
        {
            _services = services;
        }

        public static CreateUserTestHost Create()
        {
            string databaseName = $"myrmex-create-user-{Guid.NewGuid():N}";
            ServiceCollection services = [];
            services.AddLogging();
            services.AddHttpContextAccessor();
            services.AddDbContext<MyrmexIdentityDbContext>(builder =>
                builder
                    .UseInMemoryDatabase(databaseName)
                    .ConfigureWarnings(warnings => warnings.Ignore(
                        InMemoryEventId.TransactionIgnoredWarning)));
            services.AddIdentityCore<MyrmexUser>()
                .AddRoles<MyrmexRole>()
                .AddSignInManager()
                .AddEntityFrameworkStores<MyrmexIdentityDbContext>();
            services.AddScoped<CreateUser.Handler>();

            return new CreateUserTestHost(services.BuildServiceProvider());
        }

        public async Task<ServiceResult<IdentityUserDetails>> HandleAsync(
            CreateUser.Command command,
            CancellationToken cancellationToken = default)
        {
            using IServiceScope scope = _services.CreateScope();
            return await scope.ServiceProvider
                .GetRequiredService<CreateUser.Handler>()
                .HandleAsync(command, cancellationToken);
        }

        public async Task EnsureSupportedRolesAsync()
        {
            await EnsureRoleAsync(IdentityRoleNames.MyrmexAdmin);
            await EnsureRoleAsync(IdentityRoleNames.WmsOperator);
        }

        public async Task EnsureRoleAsync(string role)
        {
            using IServiceScope scope = _services.CreateScope();
            RoleManager<MyrmexRole> roleManager = scope.ServiceProvider
                .GetRequiredService<RoleManager<MyrmexRole>>();
            if (!await roleManager.RoleExistsAsync(role))
            {
                IdentityResult result = await roleManager.CreateAsync(
                    new MyrmexRole(role));
                Assert.True(result.Succeeded);
            }
        }

        public async Task CreateExistingUserAsync(string email)
        {
            using IServiceScope scope = _services.CreateScope();
            UserManager<MyrmexUser> userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<MyrmexUser>>();
            MyrmexUser user = new()
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };
            IdentityResult result = await userManager.CreateAsync(user, ValidPassword);
            Assert.True(result.Succeeded);
        }

        public async Task<MyrmexUser> FindByEmailAsync(string email)
        {
            using IServiceScope scope = _services.CreateScope();
            UserManager<MyrmexUser> userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<MyrmexUser>>();
            return await userManager.FindByEmailAsync(email)
                ?? throw new InvalidOperationException("Test user was not found.");
        }

        public async Task<bool> UserIsInRoleAsync(string email, string role)
        {
            using IServiceScope scope = _services.CreateScope();
            UserManager<MyrmexUser> userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<MyrmexUser>>();
            MyrmexUser user = await userManager.FindByEmailAsync(email)
                ?? throw new InvalidOperationException("Test user was not found.");
            return await userManager.IsInRoleAsync(user, role);
        }

        public async Task<int> CountUsersAsync()
        {
            using IServiceScope scope = _services.CreateScope();
            return await scope.ServiceProvider
                .GetRequiredService<MyrmexIdentityDbContext>()
                .Users
                .CountAsync(TestContext.Current.CancellationToken);
        }

        public ValueTask DisposeAsync() => _services.DisposeAsync();
    }
}
