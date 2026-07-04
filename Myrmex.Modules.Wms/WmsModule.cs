using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Myrmex.Modules.Wms.DemoData.Configuration;
using Myrmex.Modules.Wms.DemoData.Endpoints;
using Myrmex.Modules.Wms.DemoData.Features;
using Myrmex.Modules.Wms.Catalog.Endpoints;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Endpoints;
using Myrmex.Modules.Wms.Topology.Endpoints;

namespace Myrmex.Modules.Wms;

public static class WmsModule
{
    public static IServiceCollection AddWmsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<WmsDemoDataOptions>(
            configuration.GetSection(WmsDemoDataOptions.SectionName));
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<WmsDemoDataOperationGate>();
        services.TryAddSingleton<IWmsDemoDataStageHook, NoOpWmsDemoDataStageHook>();
        services.AddScoped<WmsDemoDataSeeder>();
        services.AddScoped<WmsDemoDataClearService>();

        services.AddDbContext<WmsDbContext>(options =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("MyrmexDatabase"));
        });

        return services;
    }

    public static IEndpointRouteBuilder MapWmsModule(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapTopologyEndpoints();
        endpoints.MapCatalogEndpoints();
        endpoints.MapInventoryEndpoints();

        WmsDemoDataOptions options = endpoints.ServiceProvider
            .GetRequiredService<IOptions<WmsDemoDataOptions>>()
            .Value;
        IHostEnvironment environment = endpoints.ServiceProvider
            .GetRequiredService<IHostEnvironment>();
        if (options.Enabled && !environment.IsProduction())
        {
            endpoints.MapDemoDataAdminEndpoints();
        }

        return endpoints;
    }
}
