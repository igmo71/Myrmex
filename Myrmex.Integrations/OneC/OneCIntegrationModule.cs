using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Myrmex.AspNetCore.Security;
using Myrmex.Integrations.OneC.Common.Imports;
using Myrmex.Integrations.OneC.Common.References;
using Myrmex.Integrations.OneC.Common.Transport;
using Myrmex.Integrations.OneC.Configuration;
using Myrmex.Integrations.OneC.Connection;
using Myrmex.Integrations.OneC.Notifications;
using Myrmex.Integrations.OneC.Security;
using Myrmex.Integrations.OneC.StockKeepingUnits;
using Myrmex.Integrations.OneC.UnitsOfMeasure;
using Myrmex.Integrations.OneC.Warehouses;
using Myrmex.Integrations.Persistence;
using Myrmex.Integrations.Persistence.SqlServer;
using Myrmex.Integrations.Synchronization;
using Myrmex.Integrations.Synchronization.Configuration;
using Myrmex.Integrations.Synchronization.Processing;

namespace Myrmex.Integrations.OneC;

public static class OneCIntegrationModule
{
    private const string DatabaseConnectionName = "MyrmexDatabase";
    private const string IntegrationPersistenceHealthCheckName = "integration-synchronization-db";

    public static IServiceCollection AddOneCIntegration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<OneCOptions>(configuration.GetSection(OneCOptions.SectionName));

        services.AddOptions<OneCIntegrationApiKeyOptions>()
            .Bind(configuration.GetSection(OneCIntegrationApiKeyOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<OneCIntegrationApiKeyOptions>, OneCIntegrationApiKeyOptionsValidator>();

        services.AddOptions<SynchronizationOptions>()
            .Bind(configuration.GetSection(SynchronizationOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<SynchronizationOptions>, SynchronizationOptionsValidator>();

        services.TryAddSingleton(TimeProvider.System);

        services.AddDbContext<IntegrationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString(DatabaseConnectionName)));

        services
            .AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                MyrmexAuthenticationSchemes.IntegrationApiKey,
                options => { });

        services.AddAuthorizationBuilder()
            .AddPolicy(
                MyrmexAuthorizationPolicies.OneCIntegration,
                MyrmexAuthorizationPolicies.ConfigureOneCIntegration);

        services.AddHealthChecks()
            .AddCheck<IntegrationDbContextHealthCheck>(
                IntegrationPersistenceHealthCheckName,
                failureStatus: HealthStatus.Unhealthy);

        services.AddSingleton<SynchronizationWakeUp>();
        services.AddSingleton<SynchronizationRetryPolicy>();
        services.AddSingleton<OneCChangeNotificationValidator>();
        services.AddScoped<SynchronizationRequestFactory>();
        services.AddScoped<SynchronizationRequestStore>();
        services.AddSingleton<ReferenceSynchronizationHandlerResultMapper>();
        services.AddScoped<ISynchronizationHandler, WarehouseReferenceSynchronizationHandler>();
        services.AddScoped<ISynchronizationHandler, UnitOfMeasureReferenceSynchronizationHandler>();
        services.AddScoped<ISynchronizationHandler, StockKeepingUnitReferenceSynchronizationHandler>();
        services.AddScoped<ISynchronizationHandlerResolver, SynchronizationHandlerResolver>();
        services.AddScoped<SynchronizationProcessor>();
        services.AddHostedService<SynchronizationWorker>();

        services.AddSingleton<SqlServerDuplicateSynchronizationRequestDetector>();

        services.AddHttpClient<IOneCODataTransport, OneCODataTransport>(client =>
        {
            client.Timeout = Timeout.InfiniteTimeSpan;
        });

        services.AddSingleton<OneCImportGate>();
        services.AddScoped<OneCImportResponseFactory>();

        services.AddScoped<IWarehouseOneCSource, WarehouseOneCSource>();
        services.AddScoped<IWarehouseOneCImport, WarehouseOneCImport>();
        services.AddScoped<IWarehouseOneCSynchronizer, WarehouseOneCSynchronizer>();

        services.AddScoped<IUnitOfMeasureOneCSource, UnitOfMeasureOneCSource>();
        services.AddScoped<IUnitOfMeasureOneCImport, UnitOfMeasureOneCImport>();
        services.AddScoped<IUnitOfMeasureOneCSynchronizer, UnitOfMeasureOneCSynchronizer>();

        services.AddScoped<IStockKeepingUnitOneCSource, StockKeepingUnitOneCSource>();
        services.AddScoped<IStockKeepingUnitOneCImport, StockKeepingUnitOneCImport>();
        services.AddScoped<IStockKeepingUnitOneCSynchronizer, StockKeepingUnitOneCSynchronizer>();

        services.AddScoped<OneCConnectionTest>();

        return services;
    }
}
