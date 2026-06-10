using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.AppDispatching.QueryDispatching;
using Myrmex.AspNetCore.Results;
using Myrmex.Core.Application.Queries;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Features.StockKeepingUnits;

namespace Myrmex.Modules.Wms.Catalog.Endpoints;

internal static class StockKeepingUnitEndpoints
{
    public static RouteGroupBuilder MapStockKeepingUnitEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/skus", CreateStockKeepingUnitAsync)
            .WithName("CreateStockKeepingUnit")
            .WithSummary("Create SKU");

        group.MapGet("/skus/{stockKeepingUnitId:guid}", GetStockKeepingUnitByIdAsync)
            .WithName("GetStockKeepingUnitById")
            .WithSummary("Get SKU By Id");

        group.MapGet("/skus", ListStockKeepingUnitsAsync)
            .WithName("ListStockKeepingUnits")
            .WithSummary("List SKUs");

        group.MapPut("/skus/{stockKeepingUnitId:guid}", UpdateStockKeepingUnitDetailsAsync)
            .WithName("UpdateStockKeepingUnitDetails")
            .WithSummary("Update SKU Details");

        group.MapPost("/skus/{stockKeepingUnitId:guid}/deactivate", DeactivateStockKeepingUnitAsync)
            .WithName("DeactivateStockKeepingUnit")
            .WithSummary("Deactivate SKU");

        group.MapPost("/skus/{stockKeepingUnitId:guid}/reactivate", ReactivateStockKeepingUnitAsync)
            .WithName("ReactivateStockKeepingUnit")
            .WithSummary("Reactivate SKU");

        return group;
    }

    private sealed record CreateStockKeepingUnitRequest(
        string? Code,
        string? Name,
        string? Description,
        Guid? BaseUnitOfMeasureId);

    private static async Task<IResult> CreateStockKeepingUnitAsync(
        CreateStockKeepingUnitRequest request,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var command = new CreateStockKeepingUnit.Command(
            Code: request.Code,
            Name: request.Name,
            Description: request.Description,
            BaseUnitOfMeasureId: request.BaseUnitOfMeasureId);

        var result = await commandDispatcher
            .DispatchAsync<CreateStockKeepingUnit.Command, ServiceResult<StockKeepingUnitDetails>>(command, cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> GetStockKeepingUnitByIdAsync(
        Guid stockKeepingUnitId,
        IQueryDispatcher queryDispatcher,
        CancellationToken cancellationToken)
    {
        var query = new GetStockKeepingUnitById.Query(stockKeepingUnitId);

        var result = await queryDispatcher
            .DispatchAsync<GetStockKeepingUnitById.Query, ServiceResult<StockKeepingUnitDetails>>(query, cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> ListStockKeepingUnitsAsync(
        int? skip,
        int? take,
        string? searchText,
        string? sortBy,
        bool? sortDescending,
        bool? includeInactive,
        IQueryDispatcher queryDispatcher,
        CancellationToken cancellationToken)
    {
        var query = new ListStockKeepingUnits.Query
        {
            Skip = skip ?? 0,
            Take = take ?? ListQuery.DefaultTake,
            SearchText = searchText,
            SortBy = sortBy,
            SortDescending = sortDescending ?? false,
            IncludeInactive = includeInactive ?? false
        };

        var result = await queryDispatcher
            .DispatchAsync<ListStockKeepingUnits.Query, ServiceResult<ListResult<StockKeepingUnitDetails>>>(query, cancellationToken);

        return result.ToHttpResult();
    }

    private sealed record UpdateStockKeepingUnitDetailsRequest(
        string? Name,
        string? Description);

    private static async Task<IResult> UpdateStockKeepingUnitDetailsAsync(
        Guid stockKeepingUnitId,
        UpdateStockKeepingUnitDetailsRequest request,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var command = new UpdateStockKeepingUnitDetails.Command(
            StockKeepingUnitId: stockKeepingUnitId,
            Name: request.Name,
            Description: request.Description);

        var result = await commandDispatcher
            .DispatchAsync<UpdateStockKeepingUnitDetails.Command, ServiceResult<StockKeepingUnitDetails>>(command, cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> DeactivateStockKeepingUnitAsync(
        Guid stockKeepingUnitId,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var command = new DeactivateStockKeepingUnit.Command(stockKeepingUnitId);

        var result = await commandDispatcher
            .DispatchAsync<DeactivateStockKeepingUnit.Command, ServiceResult<StockKeepingUnitDetails>>(command, cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> ReactivateStockKeepingUnitAsync(
        Guid stockKeepingUnitId,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var command = new ReactivateStockKeepingUnit.Command(stockKeepingUnitId);

        var result = await commandDispatcher
            .DispatchAsync<ReactivateStockKeepingUnit.Command, ServiceResult<StockKeepingUnitDetails>>(command, cancellationToken);

        return result.ToHttpResult();
    }
}
