using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.AspNetCore.Results;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Inventory.Features.InventoryTransfers;
using Myrmex.Shared.Wms.Inventory;

namespace Myrmex.Modules.Wms.Inventory.Endpoints;

internal static class InventoryTransferEndpoints
{
    public static RouteGroupBuilder MapInventoryTransferEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/transfers", CreateInventoryTransferAsync)
            .WithName("CreateInventoryTransfer")
            .WithSummary("Create Inventory Transfer");

        return group;
    }

    private static async Task<IResult> CreateInventoryTransferAsync(
        CreateInventoryTransferRequest request,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateInventoryTransfer.Command(
            request.SourceWarehouseId,
            request.DestinationWarehouseId,
            request.TransitStorageLocationId,
            (request.Lines ?? [])
                .Select(line => new CreateInventoryTransfer.Line(
                    line.StockKeepingUnitId,
                    line.SourceStorageLocationId,
                    line.DestinationStorageLocationId,
                    line.RequestedQuantity))
                .ToArray());

        var result = await commandDispatcher
            .DispatchAsync<CreateInventoryTransfer.Command, ServiceResult<InventoryTransferDetails>>(command, cancellationToken);

        return result.ToHttpResult();
    }
}
