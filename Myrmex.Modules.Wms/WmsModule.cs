using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Myrmex.Modules.Wms.Infrastructure.Persistence;

namespace Myrmex.Modules.Wms;

public static class WmsModule
{
    public static IServiceCollection AddWmsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<WmsDbContext>(options =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("MyrmexDatabase"));
        });

        return services;
    }

    public static IEndpointRouteBuilder MapWmsModule(this IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
