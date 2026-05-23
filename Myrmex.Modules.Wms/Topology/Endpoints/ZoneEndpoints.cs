using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.AppDispatching.QueryDispatching;
using Myrmex.AspNetCore.Results;
using Myrmex.Core.Application.Queries;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Topology.Features.Zones;

namespace Myrmex.Modules.Wms.Topology.Endpoints;

internal static class ZoneEndpoints
{
    public static RouteGroupBuilder MapZoneEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/warehouses/{warehouseId:guid}/zones", CreateZoneAsync)
            .WithName("CreateZone")
            .WithSummary("Create Zone");

        group.MapGet("/zones/{zoneId:guid}", GetZoneByIdAsync)
            .WithName("GetZoneById")
            .WithSummary("Get Zone By Id");

        group.MapGet("/warehouses/{warehouseId:guid}/zones", ListZonesAsync)
            .WithName("ListZones")
            .WithSummary("List Zones");

        group.MapPut("/zones/{zoneId:guid}", UpdateZoneDetailsAsync)
            .WithName("UpdateZoneDetails")
            .WithSummary("Update Zone Details");

        group.MapPost("/zones/{zoneId:guid}/deactivate", DeactivateZoneAsync)
            .WithName("DeactivateZone")
            .WithSummary("Deactivate Zone");

        group.MapPost("/zones/{zoneId:guid}/reactivate", ReactivateZoneAsync)
            .WithName("ReactivateZone")
            .WithSummary("Reactivate Zone");

        return group;
    }

    private sealed record CreateZoneRequest(
        string? Code,
        string? Name,
        string? Description);

    private static async Task<IResult> CreateZoneAsync(
        Guid warehouseId,
        CreateZoneRequest request,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var command = new CreateZone.Command(
            WarehouseId: warehouseId,
            Code: request.Code,
            Name: request.Name,
            Description: request.Description);

        var result = await commandDispatcher
            .DispatchAsync<CreateZone.Command, ServiceResult<ZoneDetails>>(command, cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> GetZoneByIdAsync(
        Guid zoneId,
        IQueryDispatcher queryDispatcher,
        CancellationToken cancellationToken)
    {
        var query = new GetZoneById.Query(zoneId);

        var result = await queryDispatcher
            .DispatchAsync<GetZoneById.Query, ServiceResult<ZoneDetails>>(query, cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> ListZonesAsync(
        Guid warehouseId,
        int? skip,
        int? take,
        string? searchText,
        string? sortBy,
        bool? sortDescending,
        bool? includeInactive,
        IQueryDispatcher queryDispatcher,
        CancellationToken cancellationToken)
    {
        var query = new ListZones.Query(warehouseId)
        {
            Skip = skip ?? 0,
            Take = take ?? ListQuery.DefaultTake,
            SearchText = searchText,
            SortBy = sortBy,
            SortDescending = sortDescending ?? false,
            IncludeInactive = includeInactive ?? false
        };

        var result = await queryDispatcher
            .DispatchAsync<ListZones.Query, ServiceResult<ListResult<ZoneDetails>>>(query, cancellationToken);

        return result.ToHttpResult();
    }

    private sealed record UpdateZoneDetailsRequest(
        string? Name,
        string? Description);

    private static async Task<IResult> UpdateZoneDetailsAsync(
        Guid zoneId,
        UpdateZoneDetailsRequest request,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var command = new UpdateZoneDetails.Command(
            ZoneId: zoneId,
            Name: request.Name,
            Description: request.Description);

        var result = await commandDispatcher
            .DispatchAsync<UpdateZoneDetails.Command, ServiceResult<ZoneDetails>>(command, cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> DeactivateZoneAsync(
        Guid zoneId,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var command = new DeactivateZone.Command(zoneId);

        var result = await commandDispatcher
            .DispatchAsync<DeactivateZone.Command, ServiceResult<ZoneDetails>>(command, cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> ReactivateZoneAsync(
        Guid zoneId,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var command = new ReactivateZone.Command(zoneId);

        var result = await commandDispatcher
            .DispatchAsync<ReactivateZone.Command, ServiceResult<ZoneDetails>>(command, cancellationToken);

        return result.ToHttpResult();
    }
}
