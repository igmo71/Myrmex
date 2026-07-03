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
using Myrmex.Shared.Wms.Topology;

namespace Myrmex.Modules.Wms.Topology.Endpoints;

internal static class WarehouseEndpoints
{
    public static RouteGroupBuilder MapWarehouseEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/warehouses", CreateWarehouseAsync)
            .WithName("CreateWarehouse")
            .WithSummary("Create Warehouse");

        group.MapGet("/warehouses/lookup", LookupWarehousesAsync)
            .WithName("LookupWarehouses")
            .WithSummary("Lookup Warehouses");

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

    private static async Task<IResult> LookupWarehousesAsync(
        [AsParameters] LookupWarehousesRequest request,
        IQueryDispatcher queryDispatcher,
        CancellationToken cancellationToken)
    {
        var query = new LookupWarehouses.Query
        {
            SearchText = request.SearchText,
            Take = request.Take,
            SelectableOnly = request.SelectableOnly ?? true
        };

        var result = await queryDispatcher
            .DispatchAsync<LookupWarehouses.Query, ServiceResult<IReadOnlyList<WarehouseLookupItem>>>(
                query,
                cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> ListWarehousesAsync(
        [AsParameters] ListWarehousesRequest request,
        IQueryDispatcher queryDispatcher,
        CancellationToken cancellationToken)
    {
        var query = new ListWarehouses.Query
        {
            Skip = request.Skip ?? 0,
            Take = request.Take ?? ListQuery.DefaultTake,
            SearchText = request.SearchText,
            SortBy = request.SortBy,
            SortDescending = request.SortDescending ?? false,
            IncludeInactive = request.IncludeInactive ?? false
        };

        var result = await queryDispatcher
            .DispatchAsync<ListWarehouses.Query, ServiceResult<ListResult<WarehouseDetails>>>(query, cancellationToken);
        return result.ToHttpResult();
    }

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
