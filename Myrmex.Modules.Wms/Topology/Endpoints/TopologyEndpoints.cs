using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Myrmex.AspNetCore.Security;

namespace Myrmex.Modules.Wms.Topology.Endpoints;

internal static class TopologyEndpoints
{
    public static IEndpointRouteBuilder MapTopologyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/wms/topology")
            .WithTags("Wms Topology")
            .RequireAuthorization(MyrmexAuthorizationPolicies.WmsOperator);

        group.MapWarehouseEndpoints();
        group.MapZoneEndpoints();
        group.MapStorageLocationEndpoints();


        return endpoints;
    }
}
