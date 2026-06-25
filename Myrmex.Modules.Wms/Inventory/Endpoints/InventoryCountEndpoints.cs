using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.AppDispatching.QueryDispatching;
using Myrmex.AspNetCore.Results;
using Myrmex.AspNetCore.Security;
using Myrmex.Core.Application.Queries;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Inventory.Features.InventoryCounts;
using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Inventory;

namespace Myrmex.Modules.Wms.Inventory.Endpoints;

internal static class InventoryCountEndpoints
{
    public static RouteGroupBuilder MapInventoryCountEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/counts", ListInventoryCountsAsync)
            .WithName("ListInventoryCounts")
            .WithSummary("List Inventory Counts");

        group.MapPost("/counts", CreateInventoryCountAsync)
            .WithName("CreateInventoryCount")
            .WithSummary("Create Inventory Count");

        group.MapGet("/counts/{inventoryCountId:guid}", GetInventoryCountByIdAsync)
            .WithName("GetInventoryCountById")
            .WithSummary("Get Inventory Count By Id");

        group.MapPost("/counts/{inventoryCountId:guid}/lines", AddInventoryCountLineAsync)
            .WithName("AddInventoryCountLine")
            .WithSummary("Add Inventory Count Line");

        group.MapDelete("/counts/{inventoryCountId:guid}/lines/{lineId:guid}", RemoveInventoryCountLineAsync)
            .WithName("RemoveInventoryCountLine")
            .WithSummary("Remove Pending Inventory Count Line");

        group.MapPost("/counts/{inventoryCountId:guid}/lines/{lineId:guid}/count", RecordInventoryCountLineAsync)
            .WithName("RecordInventoryCountLine")
            .WithSummary("Record Inventory Count Line Quantity");

        group.MapPost("/counts/{inventoryCountId:guid}/lines/{lineId:guid}/apply", ApplyInventoryCountLineAsync)
            .WithName("ApplyInventoryCountLine")
            .WithSummary("Apply Inventory Count Line");

        group.MapPost("/counts/{inventoryCountId:guid}/lines/{lineId:guid}/supersede", SupersedeInventoryCountLineAsync)
            .WithName("SupersedeInventoryCountLine")
            .WithSummary("Supersede Conflicted Inventory Count Line");

        group.MapPost("/counts/{inventoryCountId:guid}/complete", CompleteInventoryCountAsync)
            .WithName("CompleteInventoryCount")
            .WithSummary("Complete Inventory Count");

        group.MapPost("/counts/{inventoryCountId:guid}/cancel", CancelInventoryCountAsync)
            .WithName("CancelInventoryCount")
            .WithSummary("Cancel Inventory Count");

        return group;
    }

    private static async Task<IResult> ListInventoryCountsAsync(
        [AsParameters] ListInventoryCountsRequest request,
        IQueryDispatcher queryDispatcher,
        CancellationToken cancellationToken = default)
    {
        var query = new ListInventoryCounts.Query
        {
            Skip = request.Skip ?? 0,
            Take = request.Take ?? ListQuery.DefaultTake,
            SortBy = request.SortBy,
            SortDescending = request.SortDescending ?? true,
            WarehouseId = request.WarehouseId,
            StatusText = request.Status,
            Status = ListInventoryCounts.ParseStatus(request.Status),
            CreatedFromUtc = request.CreatedFromUtc,
            CreatedToUtc = request.CreatedToUtc
        };

        ServiceResult<ListResult<InventoryCountListItem>> result =
            await queryDispatcher.DispatchAsync<
                ListInventoryCounts.Query,
                ServiceResult<ListResult<InventoryCountListItem>>>(
                query,
                cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> CreateInventoryCountAsync(
        CreateInventoryCountRequest request,
        HttpContext httpContext,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken = default)
    {
        string? actorId = httpContext.GetActorId();

        if (actorId is null)
        {
            return UnauthorizedResult();
        }

        var command = new CreateInventoryCount.Command(
            request.WarehouseId,
            request.Reason,
            actorId);

        ServiceResult<InventoryCountDetails> result = await commandDispatcher
            .DispatchAsync<CreateInventoryCount.Command, ServiceResult<InventoryCountDetails>>(
                command,
                cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> CompleteInventoryCountAsync(
        Guid inventoryCountId,
        ChangeInventoryCountStatusRequest request,
        HttpContext httpContext,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken = default)
    {
        string? actorId = httpContext.GetActorId();
        if (actorId is null)
        {
            return UnauthorizedResult();
        }

        var command = new CompleteInventoryCount.Command(
            inventoryCountId,
            request.ExpectedCountVersion,
            actorId);
        ServiceResult<InventoryCountDetails> result = await commandDispatcher
            .DispatchAsync<CompleteInventoryCount.Command, ServiceResult<InventoryCountDetails>>(
                command,
                cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CancelInventoryCountAsync(
        Guid inventoryCountId,
        ChangeInventoryCountStatusRequest request,
        HttpContext httpContext,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken = default)
    {
        string? actorId = httpContext.GetActorId();
        if (actorId is null)
        {
            return UnauthorizedResult();
        }

        var command = new CancelInventoryCount.Command(
            inventoryCountId,
            request.ExpectedCountVersion,
            actorId);
        ServiceResult<InventoryCountDetails> result = await commandDispatcher
            .DispatchAsync<CancelInventoryCount.Command, ServiceResult<InventoryCountDetails>>(
                command,
                cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ApplyInventoryCountLineAsync(
        Guid inventoryCountId,
        Guid lineId,
        ApplyInventoryCountLineRequest request,
        HttpContext httpContext,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken = default)
    {
        string? actorId = httpContext.GetActorId();
        if (actorId is null)
        {
            return UnauthorizedResult();
        }

        var command = new ApplyInventoryCountLine.Command(
            inventoryCountId,
            lineId,
            request.ExpectedLineVersion,
            actorId);
        ServiceResult<InventoryCountDetails> result = await commandDispatcher
            .DispatchAsync<ApplyInventoryCountLine.Command, ServiceResult<InventoryCountDetails>>(
                command,
                cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> SupersedeInventoryCountLineAsync(
        Guid inventoryCountId,
        Guid lineId,
        SupersedeInventoryCountLineRequest request,
        HttpContext httpContext,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken = default)
    {
        string? actorId = httpContext.GetActorId();
        if (actorId is null)
        {
            return UnauthorizedResult();
        }

        var command = new SupersedeInventoryCountLine.Command(
            inventoryCountId,
            lineId,
            request.ExpectedLineVersion,
            actorId);
        ServiceResult<InventoryCountDetails> result = await commandDispatcher
            .DispatchAsync<SupersedeInventoryCountLine.Command, ServiceResult<InventoryCountDetails>>(
                command,
                cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetInventoryCountByIdAsync(
        Guid inventoryCountId,
        IQueryDispatcher queryDispatcher,
        CancellationToken cancellationToken = default)
    {
        var query = new GetInventoryCountById.Query(inventoryCountId);

        ServiceResult<InventoryCountDetails> result = await queryDispatcher
            .DispatchAsync<GetInventoryCountById.Query, ServiceResult<InventoryCountDetails>>(
                query,
                cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> AddInventoryCountLineAsync(
        Guid inventoryCountId,
        AddInventoryCountLineRequest request,
        HttpContext httpContext,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken = default)
    {
        string? actorId = httpContext.GetActorId();

        if (actorId is null)
        {
            return UnauthorizedResult();
        }

        var command = new AddInventoryCountLine.Command(
            inventoryCountId,
            request.StockKeepingUnitId,
            request.StorageLocationId,
            request.ExpectedCountVersion,
            actorId);

        ServiceResult<InventoryCountDetails> result = await commandDispatcher
            .DispatchAsync<AddInventoryCountLine.Command, ServiceResult<InventoryCountDetails>>(
                command,
                cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> RemoveInventoryCountLineAsync(
        Guid inventoryCountId,
        Guid lineId,
        string? expectedLineVersion,
        HttpContext httpContext,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken = default)
    {
        string? actorId = httpContext.GetActorId();

        if (actorId is null)
        {
            return UnauthorizedResult();
        }

        var command = new RemoveInventoryCountLine.Command(
            inventoryCountId,
            lineId,
            expectedLineVersion,
            actorId);

        ServiceResult<InventoryCountDetails> result = await commandDispatcher
            .DispatchAsync<RemoveInventoryCountLine.Command, ServiceResult<InventoryCountDetails>>(
                command,
                cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> RecordInventoryCountLineAsync(
        Guid inventoryCountId,
        Guid lineId,
        RecordInventoryCountLineRequest request,
        HttpContext httpContext,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken = default)
    {
        string? actorId = httpContext.GetActorId();

        if (actorId is null)
        {
            return UnauthorizedResult();
        }

        var command = new RecordInventoryCountLine.Command(
            inventoryCountId,
            lineId,
            request.CountedQuantity,
            request.Comment,
            request.ExpectedLineVersion,
            actorId);

        ServiceResult<InventoryCountDetails> result = await commandDispatcher
            .DispatchAsync<RecordInventoryCountLine.Command, ServiceResult<InventoryCountDetails>>(
                command,
                cancellationToken);

        return result.ToHttpResult();
    }

    private static IResult UnauthorizedResult()
    {
        return ServiceResult<InventoryCountDetails>
            .Fail(ServiceError.Unauthorized())
            .ToHttpResult();
    }
}
