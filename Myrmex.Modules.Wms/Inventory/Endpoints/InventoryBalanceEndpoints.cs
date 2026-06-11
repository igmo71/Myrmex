using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.AspNetCore.Results;
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
}
