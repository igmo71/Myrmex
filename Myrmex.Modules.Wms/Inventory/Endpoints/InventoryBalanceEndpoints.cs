using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.AppDispatching.QueryDispatching;
using Myrmex.AspNetCore.Results;
using Myrmex.Core.Application.Queries;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Inventory.Features.InventoryBalances;
using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Inventory;

namespace Myrmex.Modules.Wms.Inventory.Endpoints;

internal static class InventoryBalanceEndpoints
{
    public static RouteGroupBuilder MapInventoryBalanceEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/balances", ListInventoryBalancesAsync)
            .WithName("ListInventoryBalances")
            .WithSummary("List Inventory Balances");

        group.MapGet("/balances/{inventoryBalanceId:guid}", GetInventoryBalanceByIdAsync)
            .WithName("GetInventoryBalanceById")
            .WithSummary("Get Inventory Balance By Id");

        group.MapPost("/balances/move", MoveInventoryBalanceAsync)
            .WithName("MoveInventoryBalance")
            .WithSummary("Move Inventory Balance");

        return group;
    }

    private static async Task<IResult> ListInventoryBalancesAsync(
        [AsParameters] ListInventoryBalancesRequest request,
        IQueryDispatcher queryDispatcher,
        CancellationToken cancellationToken = default)
    {
        var query = new ListInventoryBalances.Query
        {
            Skip = request.Skip ?? 0,
            Take = request.Take ?? ListQuery.DefaultTake,
            SortBy = request.SortBy,
            SortDescending = request.SortDescending ?? false,
            StockKeepingUnitId = request.StockKeepingUnitId,
            StorageLocationId = request.StorageLocationId,
            WarehouseId = request.WarehouseId
        };

        var result = await queryDispatcher
            .DispatchAsync<ListInventoryBalances.Query, ServiceResult<ListResult<InventoryBalanceDetails>>>(
                query,
                cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> GetInventoryBalanceByIdAsync(
        Guid inventoryBalanceId,
        IQueryDispatcher queryDispatcher,
        CancellationToken cancellationToken = default)
    {
        var query = new GetInventoryBalanceById.Query(inventoryBalanceId);

        var result = await queryDispatcher
            .DispatchAsync<GetInventoryBalanceById.Query, ServiceResult<InventoryBalanceDetails>>(query, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> MoveInventoryBalanceAsync(
        MoveInventoryBalanceRequest request,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken = default)
    {
        var command = new MoveInventoryBalance.Command(
            request.StockKeepingUnitId,
            request.SourceStorageLocationId,
            request.DestinationStorageLocationId,
            request.Quantity,
            request.Reason,
            request.ExpectedSourceBalanceVersion);

        var result = await commandDispatcher
            .DispatchAsync<MoveInventoryBalance.Command, ServiceResult<MoveInventoryBalanceResult>>(
                command,
                cancellationToken);

        return result.ToHttpResult();
    }
}
