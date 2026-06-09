using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.AspNetCore.Results;
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
}
