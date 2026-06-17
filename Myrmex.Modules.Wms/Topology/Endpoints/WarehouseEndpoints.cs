using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.AppDispatching.QueryDispatching;
using Myrmex.AspNetCore.Results;
using Myrmex.Core.Application.Queries;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Topology.Features.Warehouses;
using Myrmex.Shared.Common;

namespace Myrmex.Modules.Wms.Topology.Endpoints;

internal static class WarehouseEndpoints
{
    public static RouteGroupBuilder MapWarehouseEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/warehouses", CreateWarehouseAsync)
            .WithName("CreateWarehouse")
            .WithSummary("Create Warehouse");

        group.MapGet("/warehouses/{warehouseId:guid}", GetWarehouseByIdAsync)
            .WithName("GetWarehouseById")
            .WithSummary("Get Warehouse By Id");

        group.MapGet("/warehouses", ListWarehousesAsync)
            .WithName("ListWarehouses")
            .WithSummary("List Warehouses");

        group.MapPut("/warehouses/{warehouseId:guid}", UpdateWarehouseDetailsAsync)
            .WithName("UpdateWarehouseDetails")
            .WithSummary("Update Warehouse Details");

        group.MapPost("/warehouses/{warehouseId:guid}/deactivate", DeactivateWarehouseAsync)
            .WithName("DeactivateWarehouse")
            .WithSummary("Deactivate Warehouse");

        group.MapPost("/warehouses/{warehouseId:guid}/reactivate", ReactivateWarehouseAsync)
            .WithName("ReactivateWarehouse")
            .WithSummary("Reactivate Warehouse");

        return group;
    }

    private sealed record CreateWarehouseRequest(
        string? Code,
        string? Name,
        string? Description);

    private static async Task<IResult> CreateWarehouseAsync(
        CreateWarehouseRequest request,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var command = new CreateWarehouse.Command(
            Code: request.Code,
            Name: request.Name,
            Description: request.Description);

        var result = await commandDispatcher
            .DispatchAsync<CreateWarehouse.Command, ServiceResult<WarehouseDetails>>(command, cancellationToken);
        return result.ToHttpResult();
    }
    private static async Task<IResult> GetWarehouseByIdAsync(
        Guid warehouseId,
        IQueryDispatcher queryDispatcher,
        CancellationToken cancellationToken)
    {
        var query = new GetWarehouseById.Query(warehouseId);

        var result = await queryDispatcher
            .DispatchAsync<GetWarehouseById.Query, ServiceResult<WarehouseDetails>>(query, cancellationToken);
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
            Take = take ?? ListQuery.DefaultTake,
            SearchText = searchText,
            SortBy = sortBy,
            SortDescending = sortDescending ?? false,
            IncludeInactive = includeInactive ?? false
        };

        var result = await queryDispatcher
            .DispatchAsync<ListWarehouses.Query, ServiceResult<ListResult<WarehouseDetails>>>(query, cancellationToken);
        return result.ToHttpResult();
    }

    private sealed record UpdateWarehouseDetailsRequest(
        string? Name,
        string? Description);

    private static async Task<IResult> UpdateWarehouseDetailsAsync(
        Guid warehouseId,
        UpdateWarehouseDetailsRequest request,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var command = new UpdateWarehouseDetails.Command(
            WarehouseId: warehouseId,
            Name: request.Name,
            Description: request.Description);

        var result = await commandDispatcher
            .DispatchAsync<UpdateWarehouseDetails.Command, ServiceResult<WarehouseDetails>>(command, cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> DeactivateWarehouseAsync(
        Guid warehouseId,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var command = new DeactivateWarehouse.Command(warehouseId);

        var result = await commandDispatcher
            .DispatchAsync<DeactivateWarehouse.Command, ServiceResult<WarehouseDetails>>(command, cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> ReactivateWarehouseAsync(
        Guid warehouseId,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var command = new ReactivateWarehouse.Command(warehouseId);

        var result = await commandDispatcher
            .DispatchAsync<ReactivateWarehouse.Command, ServiceResult<WarehouseDetails>>(command, cancellationToken);

        return result.ToHttpResult();
    }
}
