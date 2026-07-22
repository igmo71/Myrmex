using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.AppDispatching.QueryDispatching;
using Myrmex.AspNetCore.Results;
using Myrmex.Core.Application.Queries;
using Myrmex.Core.Application.Security;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Receiving.Features.ReceivingOrders;
using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Receiving;

namespace Myrmex.Modules.Wms.Receiving.Endpoints;

internal static class ReceivingOrderEndpoints
{
    public static RouteGroupBuilder MapReceivingOrderEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("", ListAsync)
            .WithName("ListReceivingOrders").WithSummary("List Receiving Orders");
        group.MapGet("/{receivingOrderId:guid}", GetAsync)
            .WithName("GetReceivingOrderById").WithSummary("Get Receiving Order By Id");
        group.MapPost("", CreateAsync)
            .WithName("CreateReceivingOrder").WithSummary("Create Receiving Order");
        group.MapPut("/{receivingOrderId:guid}", UpdateDraftAsync)
            .WithName("UpdateReceivingOrderDraft").WithSummary("Update Receiving Order Draft");
        group.MapDelete("/{receivingOrderId:guid}", DeleteDraftAsync)
            .WithName("DeleteReceivingOrderDraft").WithSummary("Delete Receiving Order Draft");
        group.MapPost("/{receivingOrderId:guid}/start", StartAsync)
            .WithName("StartReceivingOrder").WithSummary("Start Receiving Order");
        group.MapPost("/{receivingOrderId:guid}/lines/{lineId:guid}/receive", ReceiveLineAsync)
            .WithName("ReceiveReceivingOrderLine").WithSummary("Receive Receiving Order Line");
        group.MapPost("/{receivingOrderId:guid}/complete", CompleteAsync)
            .WithName("CompleteReceivingOrder").WithSummary("Complete Receiving Order");
        return group;
    }

    private static async Task<IResult> ListAsync(
        [AsParameters] ReceivingOrderListRequest request,
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var query = new ListReceivingOrders.Query
        {
            Skip = request.Skip ?? 0,
            Take = request.Take ?? ListQuery.DefaultTake,
            SearchText = request.SearchText,
            NormalizedSearchText = ListReceivingOrders.NormalizeSearchText(request.SearchText),
            WarehouseId = request.WarehouseId,
            StatusText = request.Status,
            Status = ListReceivingOrders.ParseStatus(request.Status),
            SortBy = request.SortBy,
            SortDescending = request.SortDescending ?? true
        };

        ServiceResult<ListResult<ReceivingOrderListItem>> result =
            await dispatcher.DispatchAsync<
                ListReceivingOrders.Query,
                ServiceResult<ListResult<ReceivingOrderListItem>>>(
                query,
                cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetAsync(
        Guid receivingOrderId,
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        ServiceResult<ReceivingOrderDetails> result = await dispatcher.DispatchAsync<
            GetReceivingOrderById.Query,
            ServiceResult<ReceivingOrderDetails>>(
            new(receivingOrderId), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreateAsync(
        CreateReceivingOrderRequest request,
        IActorContext actorContext,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        ServiceResult<ReceivingOrderDetails> result = await dispatcher.DispatchAsync<
            CreateReceivingOrder.Command,
            ServiceResult<ReceivingOrderDetails>>(
            new(request.Number, request.WarehouseId, request.ReceivingLocationId, request.Lines, actorContext.ActorId),
            cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> StartAsync(
        Guid receivingOrderId,
        ReceivingOrderActionRequest request,
        IActorContext actorContext,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        ServiceResult<ReceivingOrderDetails> result = await dispatcher.DispatchAsync<
            StartReceivingOrder.Command,
            ServiceResult<ReceivingOrderDetails>>(
            new(receivingOrderId, request.ExpectedOrderVersion, actorContext.ActorId), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> UpdateDraftAsync(
        Guid receivingOrderId,
        UpdateReceivingOrderDraftRequest request,
        IActorContext actorContext,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        ServiceResult<ReceivingOrderDetails> result = await dispatcher.DispatchAsync<
            UpdateReceivingOrderDraft.Command,
            ServiceResult<ReceivingOrderDetails>>(
            new(
                receivingOrderId,
                request.Number,
                request.WarehouseId,
                request.ReceivingLocationId,
                request.ExpectedOrderVersion,
                request.Lines,
                actorContext.ActorId),
            cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeleteDraftAsync(
        Guid receivingOrderId,
        string? expectedOrderVersion,
        IActorContext actorContext,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        ServiceResult result = await dispatcher.DispatchAsync<
            DeleteReceivingOrderDraft.Command,
            ServiceResult>(
            new(receivingOrderId, expectedOrderVersion, actorContext.ActorId),
            cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ReceiveLineAsync(
        Guid receivingOrderId,
        Guid lineId,
        ReceiveReceivingOrderLineRequest request,
        IActorContext actorContext,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        ServiceResult<ReceivingOrderDetails> result = await dispatcher.DispatchAsync<
            ReceiveReceivingOrderLine.Command,
            ServiceResult<ReceivingOrderDetails>>(
            new(receivingOrderId, lineId, request.Quantity, request.ExpectedOrderVersion, actorContext.ActorId),
            cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CompleteAsync(
        Guid receivingOrderId,
        ReceivingOrderActionRequest request,
        IActorContext actorContext,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        ServiceResult<ReceivingOrderDetails> result = await dispatcher.DispatchAsync<
            CompleteReceivingOrder.Command,
            ServiceResult<ReceivingOrderDetails>>(
            new(receivingOrderId, request.ExpectedOrderVersion, actorContext.ActorId), cancellationToken);
        return result.ToHttpResult();
    }
}
