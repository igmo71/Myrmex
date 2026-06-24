using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.AppDispatching.QueryDispatching;
using Myrmex.AspNetCore.Results;
using Myrmex.AspNetCore.Security;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Inventory.Features.InventoryCounts;
using Myrmex.Shared.Wms.Inventory;

namespace Myrmex.Modules.Wms.Inventory.Endpoints;

internal static class InventoryCountEndpoints
{
    public static RouteGroupBuilder MapInventoryCountEndpoints(this RouteGroupBuilder group)
    {
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

        return group;
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

    private static IResult UnauthorizedResult()
    {
        return ServiceResult<InventoryCountDetails>
            .Fail(ServiceError.Unauthorized())
            .ToHttpResult();
    }
}
