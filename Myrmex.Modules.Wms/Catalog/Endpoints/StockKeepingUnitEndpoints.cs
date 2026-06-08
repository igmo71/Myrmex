using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.AspNetCore.Results;
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

        return group;
    }

    private sealed record CreateStockKeepingUnitRequest(
        string? Code,
        string? Name,
        string? Description);

    private static async Task<IResult> CreateStockKeepingUnitAsync(
        CreateStockKeepingUnitRequest request,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var command = new CreateStockKeepingUnit.Command(
            Code: request.Code,
            Name: request.Name,
            Description: request.Description);

        var result = await commandDispatcher
            .DispatchAsync<CreateStockKeepingUnit.Command, ServiceResult<StockKeepingUnitDetails>>(command, cancellationToken);

        return result.ToHttpResult();
    }
}
