using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddOptions<InitialAdminOptions>()
            .Bind(configuration.GetSection(InitialAdminOptions.SectionName))
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<InitialAdminOptions>>(
                serviceProvider => new InitialAdminOptionsValidator(
                    environment,
                    serviceProvider.GetRequiredService<IOptions<IdentityOptions>>())));

        services.TryAddScoped<IIdentityRoleInitializer, IdentityRoleInitializer>();
        services.TryAddScoped<
            IInitialAdminRoleAssigner,
            UserManagerInitialAdminRoleAssigner>();
        services.TryAddScoped<IInitialAdminSeeder, InitialAdminSeeder>();

        return services;
    }

    public static async Task RunMyrmexIdentityBootstrapAsync(
        this IHost host,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(host);

        using IServiceScope scope = host.Services.CreateScope();
        IIdentityRoleInitializer roleInitializer = scope.ServiceProvider
            .GetRequiredService<IIdentityRoleInitializer>();
        IInitialAdminSeeder adminSeeder = scope.ServiceProvider
            .GetRequiredService<IInitialAdminSeeder>();

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
