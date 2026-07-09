using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Myrmex.Identity.Application.Bootstrap;

namespace Myrmex.Identity.Infrastructure;

public static class IdentityApplicationExtensions
{
    public static IServiceCollection AddMyrmexIdentityBootstrap(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<InitialAdminOptions>()
            .Bind(configuration.GetSection(InitialAdminOptions.SectionName))
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IValidateOptions<InitialAdminOptions>,
                InitialAdminOptionsValidator>());

        services.TryAddScoped<IdentityRoleInitializer>();
        services.TryAddScoped<
            IInitialAdminRoleAssigner,
            UserManagerInitialAdminRoleAssigner>();
        services.TryAddScoped<InitialAdminSeeder>();

        return services;
    }

    public static async Task RunMyrmexIdentityBootstrapAsync(
        this IHost host,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(host);

        using IServiceScope scope = host.Services.CreateScope();
        IdentityRoleInitializer roleInitializer = scope.ServiceProvider
            .GetRequiredService<IdentityRoleInitializer>();
        InitialAdminSeeder adminSeeder = scope.ServiceProvider
            .GetRequiredService<InitialAdminSeeder>();

        try
        {
            await roleInitializer.EnsureRolesAsync(cancellationToken);
            await adminSeeder.SeedAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new InvalidOperationException(
                "Identity bootstrap failed. Confirm that the developer-controlled " +
                "Identity migration has been generated, reviewed, and applied to " +
                "the configured MyrmexDatabase before starting ApiService. If the " +
                "schema is available, inspect the inner exception for the specific " +
                "role or initial-administrator bootstrap failure.",
                exception);
        }
    }
}
