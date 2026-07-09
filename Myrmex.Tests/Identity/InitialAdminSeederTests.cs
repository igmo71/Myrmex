using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Myrmex.Identity.Application.Bootstrap;
using Myrmex.Identity.Persistence;
using Myrmex.Shared.Identity;

namespace Myrmex.Tests.Identity;

public sealed class InitialAdminSeederTests
{
    private const string AdminEmail = "admin@example.com";
    private const string BootstrapPassword = "Myrmex1!";
    private const string ExistingPassword = "Existing1!";

    [Fact]
    public async Task DisabledBootstrap_DoesNotCreateAdminUser()
    {
        await using BootstrapTestHost host = BootstrapTestHost.Create(
            new InitialAdminOptions());

        InitialAdminBootstrapResult result = await host.SeedAdminOnlyAsync();

        Assert.Equal(InitialAdminBootstrapStatus.Disabled, result.Status);
        Assert.Equal(0, await host.CountUsersAsync());
    }

    [Fact]
    public async Task FirstRun_CreatesSupportedRolesAndInitialAdministrator()
    {
        await using BootstrapTestHost host = BootstrapTestHost.Create(
            CreateEnabledOptions());

        InitialAdminBootstrapResult result = await host.RunBootstrapAsync();

        Assert.Equal(InitialAdminBootstrapStatus.Created, result.Status);
        Assert.True(await host.RoleExistsAsync(IdentityRoleNames.MyrmexAdmin));
        Assert.True(await host.RoleExistsAsync(IdentityRoleNames.WmsOperator));
        Assert.True(await host.UserIsInRoleAsync(AdminEmail, IdentityRoleNames.MyrmexAdmin));
        Assert.Equal(1, await host.CountUsersAsync());
    }

    [Fact]
    public async Task ExistingUser_IsAssignedAdminRoleWithoutPasswordOverwrite()
    {
        await using BootstrapTestHost host = BootstrapTestHost.Create(
            CreateEnabledOptions(password: BootstrapPassword));
        await host.EnsureRolesAsync();
        await host.CreateUserAsync(AdminEmail, ExistingPassword);

        InitialAdminBootstrapResult result = await host.SeedAdminOnlyAsync();

        Assert.Equal(
            InitialAdminBootstrapStatus.ExistingUserAssignedAdminRole,
            result.Status);
        Assert.True(await host.UserIsInRoleAsync(AdminEmail, IdentityRoleNames.MyrmexAdmin));
        Assert.True(await host.CheckPasswordAsync(AdminEmail, ExistingPassword));
        Assert.False(await host.CheckPasswordAsync(AdminEmail, BootstrapPassword));
    }

    [Fact]
    public async Task MissingAdminRole_IsCreatedIdempotently()
    {
        await using BootstrapTestHost host = BootstrapTestHost.Create(
            CreateEnabledOptions());
        await host.EnsureRoleAsync(IdentityRoleNames.WmsOperator);

        await host.RunBootstrapAsync();

        Assert.True(await host.RoleExistsAsync(IdentityRoleNames.MyrmexAdmin));
        Assert.True(await host.RoleExistsAsync(IdentityRoleNames.WmsOperator));
    }

    [Fact]
    public async Task RepeatedRuns_DoNotCreateDuplicateUsers()
    {
        await using BootstrapTestHost host = BootstrapTestHost.Create(
            CreateEnabledOptions());

        InitialAdminBootstrapResult first = await host.RunBootstrapAsync();
        InitialAdminBootstrapResult second = await host.RunBootstrapAsync();

        Assert.Equal(InitialAdminBootstrapStatus.Created, first.Status);
        Assert.Equal(
            InitialAdminBootstrapStatus.ExistingUserAlreadyAdmin,
            second.Status);
        Assert.Equal(1, await host.CountUsersAsync());
    }

    [Fact]
    public async Task ConcurrentRuns_CreateOneAdministrator()
    {
        await using BootstrapTestHost host = BootstrapTestHost.Create(
            CreateEnabledOptions());
        await host.EnsureRolesAsync();

        InitialAdminBootstrapResult[] results = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(_ => host.SeedAdminOnlyAsync()));

        Assert.Single(results, result =>
            result.Status == InitialAdminBootstrapStatus.Created);
        Assert.Equal(1, await host.CountUsersAsync());
        Assert.True(await host.UserIsInRoleAsync(AdminEmail, IdentityRoleNames.MyrmexAdmin));
    }

    [Fact]
    public async Task RoleAssignmentFailure_RemovesCreatedUser()
    {
        await using BootstrapTestHost host = BootstrapTestHost.Create(
            CreateEnabledOptions(),
            failRoleAssignment: true);
        await host.EnsureRolesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => host.SeedAdminOnlyAsync());

        Assert.Equal(0, await host.CountUsersAsync());
    }

    [Fact]
    public async Task BootstrapLogs_DoNotContainPasswordOrPasswordHash()
    {
        await using BootstrapTestHost host = BootstrapTestHost.Create(
            CreateEnabledOptions(password: BootstrapPassword));

        await host.RunBootstrapAsync();

        string logs = string.Join(Environment.NewLine, host.LogMessages);
        Assert.DoesNotContain(BootstrapPassword, logs, StringComparison.Ordinal);
        Assert.DoesNotContain("PasswordHash", logs, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cookie", logs, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ticket", logs, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Data Protection", logs, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("certificate", logs, StringComparison.OrdinalIgnoreCase);
    }

    private static InitialAdminOptions CreateEnabledOptions(
        string password = BootstrapPassword) =>
        new()
        {
            Enabled = true,
            Email = AdminEmail,
            Password = password,
            DisplayName = "Initial Administrator"
        };

    private sealed class BootstrapTestHost : IAsyncDisposable
    {
        private readonly ServiceProvider _services;
        private readonly ListLoggerProvider _loggerProvider;

        private BootstrapTestHost(
            ServiceProvider services,
            ListLoggerProvider loggerProvider)
        {
            _services = services;
            _loggerProvider = loggerProvider;
        }

        public IReadOnlyCollection<string> LogMessages => _loggerProvider.Messages;

        public static BootstrapTestHost Create(
            InitialAdminOptions options,
            bool failRoleAssignment = false)
        {
            string databaseName = $"myrmex-bootstrap-{Guid.NewGuid():N}";

            ListLoggerProvider loggerProvider = new();
            ServiceCollection services = [];
            services.AddLogging(builder => builder.AddProvider(loggerProvider));
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
            services.AddSingleton(Options.Create(options));
            services.AddScoped<IIdentityRoleInitializer, IdentityRoleInitializer>();
            services.AddScoped<IInitialAdminRoleAssigner>(
                _ => failRoleAssignment
                    ? new FailingInitialAdminRoleAssigner()
                    : new UserManagerInitialAdminRoleAssigner(
                        _.GetRequiredService<UserManager<MyrmexUser>>()));
            services.AddScoped<IInitialAdminSeeder, InitialAdminSeeder>();

            return new BootstrapTestHost(
                services.BuildServiceProvider(),
                loggerProvider);
        }

        public async Task<InitialAdminBootstrapResult> RunBootstrapAsync()
        {
            await EnsureRolesAsync();
            return await SeedAdminOnlyAsync();
        }

        public async Task EnsureRolesAsync()
        {
            using IServiceScope scope = _services.CreateScope();
            await scope.ServiceProvider
                .GetRequiredService<IIdentityRoleInitializer>()
                .EnsureRolesAsync(TestContext.Current.CancellationToken);
        }

        public async Task EnsureRoleAsync(string roleName)
        {
            using IServiceScope scope = _services.CreateScope();
            RoleManager<MyrmexRole> roleManager = scope.ServiceProvider
                .GetRequiredService<RoleManager<MyrmexRole>>();
            IdentityResult result = await roleManager.CreateAsync(
                new MyrmexRole(roleName));
            Assert.True(result.Succeeded);
        }

        public async Task<InitialAdminBootstrapResult> SeedAdminOnlyAsync()
        {
            using IServiceScope scope = _services.CreateScope();
            return await scope.ServiceProvider
                .GetRequiredService<IInitialAdminSeeder>()
                .SeedAsync(TestContext.Current.CancellationToken);
        }

        public async Task CreateUserAsync(string email, string password)
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

            IdentityResult result = await userManager.CreateAsync(user, password);
            Assert.True(result.Succeeded);
        }

        public async Task<bool> RoleExistsAsync(string roleName)
        {
            using IServiceScope scope = _services.CreateScope();
            return await scope.ServiceProvider
                .GetRequiredService<RoleManager<MyrmexRole>>()
                .RoleExistsAsync(roleName);
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

        public async Task<bool> CheckPasswordAsync(string email, string password)
        {
            using IServiceScope scope = _services.CreateScope();
            UserManager<MyrmexUser> userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<MyrmexUser>>();
            MyrmexUser user = await userManager.FindByEmailAsync(email)
                ?? throw new InvalidOperationException("Test user was not found.");
            return await userManager.CheckPasswordAsync(user, password);
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

    private sealed class FailingInitialAdminRoleAssigner
        : IInitialAdminRoleAssigner
    {
        public Task<IdentityResult> AddToRoleAsync(
            MyrmexUser user,
            string role,
            CancellationToken cancellationToken) =>
            Task.FromResult(IdentityResult.Failed(
                new IdentityError { Code = "RoleAssignmentFailed" }));
    }

    private sealed class ListLoggerProvider : ILoggerProvider
    {
        private readonly List<string> _messages = [];
        private readonly object _syncRoot = new();

        public IReadOnlyCollection<string> Messages
        {
            get
            {
                lock (_syncRoot)
                {
                    return _messages.ToArray();
                }
            }
        }

        public ILogger CreateLogger(string categoryName) =>
            new ListLogger(_messages, _syncRoot);

        public void Dispose()
        {
        }
    }

    private sealed class ListLogger(
        List<string> messages,
        object syncRoot) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull =>
            null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            lock (syncRoot)
            {
                messages.Add(formatter(state, exception));
            }
        }
    }
}
