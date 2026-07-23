using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Myrmex.AspNetCore.Security;

namespace Myrmex.Modules.Wms.Receiving.Endpoints;

internal static class ReceivingEndpoints
{
    public static IEndpointRouteBuilder MapReceivingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints
            .MapGroup("/api/wms/receiving-orders")
            .WithTags("Wms Receiving")
            .RequireAuthorization(MyrmexAuthorizationPolicies.WmsOperator);
        group.MapReceivingOrderEndpoints();
        return endpoints;
    }
}
