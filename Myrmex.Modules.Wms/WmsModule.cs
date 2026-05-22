using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Myrmex.Modules.Wms;

public static class WmsModule
{
    public static IServiceCollection AddWmsModule(this IServiceCollection services, IConfiguration configuration)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapWmsModule(this IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
