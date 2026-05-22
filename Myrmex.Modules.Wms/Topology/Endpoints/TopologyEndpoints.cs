using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Myrmex.Modules.Wms.Topology.Endpoints;

internal static class TopologyEndpoints
{
    public static IEndpointRouteBuilder MapTopologyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/wms/topology")
            .WithTags("Wms Topology");

        group.MapWarehouseEndpoints();

        return endpoints.MapTopologyEndpoints();
    }
}
