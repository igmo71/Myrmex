using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.AppDispatching.QueryDispatching;
using Myrmex.AspNetCore.Results;
using Myrmex.Core.Application.Queries;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Inventory.Features.InventoryTransfers;
using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Inventory;

namespace Myrmex.Modules.Wms.Inventory.Endpoints;

internal static class InventoryTransferEndpoints
{
    public static RouteGroupBuilder MapInventoryTransferEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/transfers", ListInventoryTransfersAsync)
            .WithName("ListInventoryTransfers")
            .WithSummary("List Inventory Transfers");

        group.MapGet("/transfers/{transferId:guid}", GetInventoryTransferByIdAsync)
            .WithName("GetInventoryTransferById")
            .WithSummary("Get Inventory Transfer By Id");

        group.MapPost("/transfers", CreateInventoryTransferAsync)
            .WithName("CreateInventoryTransfer")
            .WithSummary("Create Inventory Transfer");

        group.MapPost("/transfers/{transferId:guid}/lines/{lineId:guid}/move", MoveInventoryTransferLineAsync)
            .WithName("MoveInventoryTransferLine")
            .WithSummary("Move Inventory Transfer Line");

        group.MapPost("/transfers/{transferId:guid}/lines/{lineId:guid}/pick", PickInventoryTransferLineAsync)
            .WithName("PickInventoryTransferLine")
            .WithSummary("Pick Inventory Transfer Line To Transit");

        group.MapPost("/transfers/{transferId:guid}/lines/{lineId:guid}/place", PlaceInventoryTransferLineAsync)
            .WithName("PlaceInventoryTransferLine")
            .WithSummary("Place Inventory Transfer Line From Transit");

        return group;
    }

    private static async Task<IResult> ListInventoryTransfersAsync(
        [AsParameters] ListInventoryTransfersRequest request,
        IQueryDispatcher queryDispatcher,
        CancellationToken cancellationToken = default)
    {
        var query = new ListInventoryTransfers.Query
        {
            Skip = request.Skip ?? 0,
            Take = request.Take ?? ListQuery.DefaultTake,
            SortBy = request.SortBy,
            SortDescending = request.SortDescending ?? true,
            WarehouseId = request.WarehouseId,
            StatusText = request.Status,
            Status = ListInventoryTransfers.ParseStatus(request.Status),
            CreatedFromUtc = request.CreatedFromUtc,
            CreatedToUtc = request.CreatedToUtc,
            TransferCode = request.TransferCode,
            SourceStorageLocationId = request.SourceStorageLocationId,
            DestinationStorageLocationId = request.DestinationStorageLocationId,
            StockKeepingUnitId = request.StockKeepingUnitId,
            HasTransitLocation = request.HasTransitLocation
        };

        var result = await queryDispatcher
            .DispatchAsync<ListInventoryTransfers.Query, ServiceResult<ListResult<InventoryTransferListItem>>>(
                query,
                cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> GetInventoryTransferByIdAsync(
        Guid transferId,
        IQueryDispatcher queryDispatcher,
        CancellationToken cancellationToken = default)
    {
        var query = new GetInventoryTransferById.Query(transferId);

        var result = await queryDispatcher
            .DispatchAsync<GetInventoryTransferById.Query, ServiceResult<InventoryTransferDetails>>(query, cancellationToken);

        return result.ToHttpResult();
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

    private static async Task<IResult> MoveInventoryTransferLineAsync(
        Guid transferId,
        Guid lineId,
        MoveInventoryTransferLineRequest request,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken = default)
    {
        var command = new MoveInventoryTransferLine.Command(
            transferId,
            lineId,
            request.Quantity);

        var result = await commandDispatcher
            .DispatchAsync<MoveInventoryTransferLine.Command, ServiceResult<InventoryTransferDetails>>(command, cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> PickInventoryTransferLineAsync(
        Guid transferId,
        Guid lineId,
        PickInventoryTransferLineRequest request,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken = default)
    {
        var command = new PickInventoryTransferLine.Command(
            transferId,
            lineId,
            request.Quantity);

        var result = await commandDispatcher
            .DispatchAsync<PickInventoryTransferLine.Command, ServiceResult<InventoryTransferDetails>>(command, cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> PlaceInventoryTransferLineAsync(
        Guid transferId,
        Guid lineId,
        PlaceInventoryTransferLineRequest request,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken = default)
    {
        var command = new PlaceInventoryTransferLine.Command(
            transferId,
            lineId,
            request.Quantity);

        var result = await commandDispatcher
            .DispatchAsync<PlaceInventoryTransferLine.Command, ServiceResult<InventoryTransferDetails>>(command, cancellationToken);

        return result.ToHttpResult();
    }
}
