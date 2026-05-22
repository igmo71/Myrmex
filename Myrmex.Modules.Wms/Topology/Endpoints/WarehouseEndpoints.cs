using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.AppDispatching.QueryDispatching;
using Myrmex.Core.Application.Queries;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Common.Http;
using Myrmex.Modules.Wms.Topology.Features.Warehouses;
using static Myrmex.Modules.Wms.Topology.Features.Warehouses.ListWarehouses;

namespace Myrmex.Modules.Wms.Topology.Endpoints;

internal static class WarehouseEndpoints
{
    public static RouteGroupBuilder MapWarehouseEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("warehouses", CreateWarehouseAsync)
            .WithName("CreateWarehouse")
            .WithSummary("Create Warehouse");
        group.MapGet("warehouses/{warehouseId:guid}", GetWarehouseByIdAsync)
            .WithName("GetWarehouseById")
            .WithSummary("Get Warehouse By Id");
        group.MapGet("warehouses", ListWarehousesAsync)
            .WithName("ListWarehouses")
            .WithSummary("List Warehouses");

        return group;
    }
    private static async Task<IResult> CreateWarehouseAsync(
        CreateWarehouse.Command command,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var result = await commandDispatcher
            .DispatchAsync<CreateWarehouse.Command, ServiceResult<CreateWarehouse.Result>>(command, cancellationToken);
        return result.ToHttpResult();
    }
    private static async Task<IResult> GetWarehouseByIdAsync(
        Guid warehouseId,
        IQueryDispatcher queryDispatcher,
        CancellationToken cancellationToken)
    {
        var query = new GetWarehouseById.Query(warehouseId);

        var result = await queryDispatcher
            .DispatchAsync<GetWarehouseById.Query, ServiceResult<GetWarehouseById.Result>>(query, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ListWarehousesAsync(
        int? skip,
        int? take,
        string? searchText,
        string? sortBy,
        bool? sortDescending,
        bool? includeInactive,
        IQueryDispatcher queryDispatcher,
        CancellationToken cancellationToken)
    {
        var query = new ListWarehouses.Query
        {
            Skip = skip ?? 0,
            Take = take ?? 20,
            SearchText = searchText,
            SortBy = sortBy,
            SortDescending = sortDescending ?? false,
            IncludeInactive = includeInactive ?? false
        };

        var result = await queryDispatcher
            .DispatchAsync<ListWarehouses.Query, ServiceResult<ListResult<Item>>>(query, cancellationToken);
        return result.ToHttpResult();
    }
}
