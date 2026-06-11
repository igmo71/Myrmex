using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.AppDispatching.QueryDispatching;
using Myrmex.AspNetCore.Results;
using Myrmex.Core.Application.Queries;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Inventory.Features.InventoryBalances;

namespace Myrmex.Modules.Wms.Inventory.Endpoints;

internal static class InventoryBalanceEndpoints
{
    public static RouteGroupBuilder MapInventoryBalanceEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/balances", CreateInventoryBalanceAsync)
            .WithName("CreateInventoryBalance")
            .WithSummary("Create Inventory Balance");

        group.MapGet("/balances", ListInventoryBalancesAsync)
            .WithName("ListInventoryBalances")
            .WithSummary("List Inventory Balances");

        group.MapGet("/balances/{inventoryBalanceId:guid}", GetInventoryBalanceByIdAsync)
            .WithName("GetInventoryBalanceById")
            .WithSummary("Get Inventory Balance By Id");

        return group;
    }

    private sealed record CreateInventoryBalanceRequest(
        Guid? StockKeepingUnitId,
        Guid? StorageLocationId,
        decimal Quantity);

    private static async Task<IResult> CreateInventoryBalanceAsync(
        CreateInventoryBalanceRequest request,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var command = new CreateInventoryBalance.Command(
            request.StockKeepingUnitId,
            request.StorageLocationId,
            request.Quantity);

        var result = await commandDispatcher
            .DispatchAsync<CreateInventoryBalance.Command, ServiceResult<InventoryBalanceDetails>>(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ListInventoryBalancesAsync(
        int? skip,
        int? take,
        string? sortBy,
        bool? sortDescending,
        Guid? stockKeepingUnitId,
        Guid? storageLocationId,
        Guid? warehouseId,
        IQueryDispatcher queryDispatcher,
        CancellationToken cancellationToken)
    {
        var query = new ListInventoryBalances.Query
        {
            Skip = skip ?? 0,
            Take = take ?? ListQuery.DefaultTake,
            SortBy = sortBy,
            SortDescending = sortDescending ?? false,
            StockKeepingUnitId = stockKeepingUnitId,
            StorageLocationId = storageLocationId,
            WarehouseId = warehouseId
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
        CancellationToken cancellationToken)
    {
        var query = new GetInventoryBalanceById.Query(inventoryBalanceId);

        var result = await queryDispatcher
            .DispatchAsync<GetInventoryBalanceById.Query, ServiceResult<InventoryBalanceDetails>>(query, cancellationToken);
        return result.ToHttpResult();
    }
}
