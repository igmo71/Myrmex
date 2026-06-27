using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Myrmex.Integrations.OneC.Configuration;
using Myrmex.Integrations.OneC.Imports;
using Myrmex.Integrations.OneC.Transport;

namespace Myrmex.Integrations.OneC;

public static class OneCIntegrationModule
{
    public static IServiceCollection AddOneCIntegration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<OneCOptions>(configuration.GetSection(OneCOptions.SectionName));
        services.TryAddSingleton(TimeProvider.System);
        services.AddHttpClient<IOneCODataClient, OneCODataClient>(client =>
        {
            client.Timeout = Timeout.InfiniteTimeSpan;
        });
        services.AddScoped<IOneCImportService, OneCImportService>();
        return services;
    }
}
