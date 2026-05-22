using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Common.Http;
using Myrmex.Modules.Wms.Topology.Features.Warehouses;

namespace Myrmex.Modules.Wms.Topology.Endpoints;

internal static class WarehouseEndpoints
{
    public static RouteGroupBuilder MapWarehouseEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("warehouses", CreateWarehouseAsync);

        return group;
    }
    private static async Task<IResult> CreateWarehouseAsync(
        CreateWarehouse.Command command,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        //CreateWarehouse.Command command = new(
        //    request.Code,
        //    request.Name,
        //    request.Description);

        var result =
            await commandDispatcher.DispatchAsync<CreateWarehouse.Command, ServiceResult<CreateWarehouse.Response>>(
            command,
            cancellationToken);

        return result.ToHttpResult();
    }
}
