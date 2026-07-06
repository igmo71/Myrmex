using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Myrmex.AspNetCore.Security;

namespace Myrmex.Modules.Wms.Catalog.Endpoints;

internal static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/wms/catalog")
            .WithTags("Wms Catalog")
            .RequireAuthorization(MyrmexAuthorizationPolicies.WmsOperator);

        group.MapStockKeepingUnitEndpoints();
        group.MapUnitOfMeasureEndpoints();
        group.MapSkuBarcodeEndpoints();

        return endpoints;
    }
}
