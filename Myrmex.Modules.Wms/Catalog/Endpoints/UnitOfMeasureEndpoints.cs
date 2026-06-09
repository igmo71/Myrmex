using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.AppDispatching.QueryDispatching;
using Myrmex.AspNetCore.Results;
using Myrmex.Core.Application.Queries;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Features.UnitsOfMeasure;

namespace Myrmex.Modules.Wms.Catalog.Endpoints;

internal static class UnitOfMeasureEndpoints
{
    public static RouteGroupBuilder MapUnitOfMeasureEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/uoms", CreateUnitOfMeasureAsync)
            .WithName("CreateUnitOfMeasure")
            .WithSummary("Create UoM");

        group.MapGet("/uoms/{unitOfMeasureId:guid}", GetUnitOfMeasureByIdAsync)
            .WithName("GetUnitOfMeasureById")
            .WithSummary("Get UoM By Id");

        group.MapGet("/uoms", ListUnitsOfMeasureAsync)
            .WithName("ListUnitsOfMeasure")
            .WithSummary("List UoMs");

        return group;
    }

    private sealed record CreateUnitOfMeasureRequest(
        string? Code,
        string? Name,
        string? Symbol);

    private static async Task<IResult> CreateUnitOfMeasureAsync(
        CreateUnitOfMeasureRequest request,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var command = new CreateUnitOfMeasure.Command(
            Code: request.Code,
            Name: request.Name,
            Symbol: request.Symbol);

        var result = await commandDispatcher
            .DispatchAsync<CreateUnitOfMeasure.Command, ServiceResult<UnitOfMeasureDetails>>(command, cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> GetUnitOfMeasureByIdAsync(
        Guid unitOfMeasureId,
        IQueryDispatcher queryDispatcher,
        CancellationToken cancellationToken)
    {
        var query = new GetUnitOfMeasureById.Query(unitOfMeasureId);

        var result = await queryDispatcher
            .DispatchAsync<GetUnitOfMeasureById.Query, ServiceResult<UnitOfMeasureDetails>>(query, cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> ListUnitsOfMeasureAsync(
        int? skip,
        int? take,
        string? searchText,
        string? sortBy,
        bool? sortDescending,
        bool? includeInactive,
        IQueryDispatcher queryDispatcher,
        CancellationToken cancellationToken)
    {
        var query = new ListUnitsOfMeasure.Query
        {
            Skip = skip ?? 0,
            Take = take ?? ListQuery.DefaultTake,
            SearchText = searchText,
            SortBy = sortBy,
            SortDescending = sortDescending ?? false,
            IncludeInactive = includeInactive ?? false
        };

        var result = await queryDispatcher
            .DispatchAsync<ListUnitsOfMeasure.Query, ServiceResult<ListResult<UnitOfMeasureDetails>>>(query, cancellationToken);

        return result.ToHttpResult();
    }
}
