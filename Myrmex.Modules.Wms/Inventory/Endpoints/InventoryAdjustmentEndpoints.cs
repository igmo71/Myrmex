using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.AspNetCore.Results;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Inventory.Features.InventoryAdjustments;
using Myrmex.Shared.Wms.Inventory;

namespace Myrmex.Modules.Wms.Inventory.Endpoints;

internal static class InventoryAdjustmentEndpoints
{
    public static RouteGroupBuilder MapInventoryAdjustmentEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/adjustments", AdjustInventoryBalanceAsync)
            .WithName("AdjustInventoryBalance")
            .WithSummary("Adjust Inventory Balance");

        return group;
    }

    private static async Task<IResult> AdjustInventoryBalanceAsync(
        AdjustInventoryBalanceRequest request,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken = default)
    {
        var command = new AdjustInventoryBalance.Command(
            request.StockKeepingUnitId,
            request.StorageLocationId,
            request.CountedQuantity,
            request.Reason,
            request.ExpectedBalanceVersion);

        var result = await commandDispatcher
            .DispatchAsync<AdjustInventoryBalance.Command, ServiceResult<InventoryBalanceDetails>>(command, cancellationToken);

        return result.ToHttpResult();
    }
}
