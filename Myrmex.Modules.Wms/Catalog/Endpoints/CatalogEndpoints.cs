using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Myrmex.Modules.Wms.Catalog.Endpoints;

internal static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/wms/catalog")
            .WithTags("Wms Catalog");

        group.MapStockKeepingUnitEndpoints();
        group.MapUnitOfMeasureEndpoints();

        return endpoints;
    }
}
