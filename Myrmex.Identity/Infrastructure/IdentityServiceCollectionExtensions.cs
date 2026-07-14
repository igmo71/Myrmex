using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Myrmex.Identity.Infrastructure.Sessions;
using Myrmex.Identity.Persistence;

namespace Myrmex.Identity.Infrastructure;

public static class IdentityServiceCollectionExtensions
{
    private const string DatabaseConnectionName = "MyrmexDatabase";

    public static IServiceCollection AddMyrmexIdentity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        string connectionString =
            configuration.GetConnectionString(DatabaseConnectionName)
            ?? throw new InvalidOperationException(
                $"Connection string '{DatabaseConnectionName}' is not configured.");

        services.AddDbContext<IdentityDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddHttpContextAccessor();
        services.AddIdentityCore<AppUser>()
            .AddRoles<AppRole>()
            .AddSignInManager()
            .AddEntityFrameworkStores<IdentityDbContext>();

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<
            IIdentityApiSessionTicketIssuer,
            IdentityApiSessionTicketIssuer>();

        return services;
    }
}
