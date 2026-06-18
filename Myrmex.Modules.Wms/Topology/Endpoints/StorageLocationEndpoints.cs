using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.AppDispatching.QueryDispatching;
using Myrmex.AspNetCore.Results;
using Myrmex.Core.Application.Queries;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Topology.Features.StorageLocations;
using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Topology;

namespace Myrmex.Modules.Wms.Topology.Endpoints;

internal static class StorageLocationEndpoints
{
    public static RouteGroupBuilder MapStorageLocationEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/warehouses/{warehouseId:guid}/zones/{zoneId:guid}/locations", CreateStorageLocationAsync)
            .WithName("CreateStorageLocation")
            .WithSummary("Create Storage Location");

        group.MapGet("/locations/{storageLocationId:guid}", GetStorageLocationByIdAsync)
            .WithName("GetStorageLocationById")
            .WithSummary("Get Storage Location By Id");

        group.MapGet("/warehouses/{warehouseId:guid}/locations", ListStorageLocationsByWarehouseAsync)
            .WithName("ListStorageLocationsByWarehouse")
            .WithSummary("List Storage Locations By Warehouse");

        group.MapGet("/warehouses/{warehouseId:guid}/locations/lookup", LookupStorageLocationsAsync)
            .WithName("LookupStorageLocations")
            .WithSummary("Lookup Storage Locations By Warehouse");

        group.MapGet("/zones/{zoneId:guid}/locations", ListStorageLocationsByZoneAsync)
            .WithName("ListStorageLocationsByZone")
            .WithSummary("List Storage Locations By Zone");

        group.MapPut("/locations/{storageLocationId:guid}", UpdateStorageLocationDetailsAsync)
            .WithName("UpdateStorageLocationDetails")
            .WithSummary("Update Storage Location Details");

        group.MapPost("/locations/{storageLocationId:guid}/deactivate", DeactivateStorageLocationAsync)
            .WithName("DeactivateStorageLocation")
            .WithSummary("Deactivate Storage Location");

        group.MapPost("/locations/{storageLocationId:guid}/reactivate", ReactivateStorageLocationAsync)
            .WithName("ReactivateStorageLocation")
            .WithSummary("Reactivate Storage Location");

        group.MapGet("/location-types", ListStorageLocationTypesAsync)
            .WithName("ListStorageLocationTypes")
            .WithSummary("List Storage Location Types");

        group.MapGet("/location-statuses", ListStorageLocationStatusesAsync)
            .WithName("ListStorageLocationStatuses")
            .WithSummary("List Storage Location Statuses");

        return group;
    }

    private sealed record CreateStorageLocationRequest(
        Guid StorageLocationTypeId,
        Guid StorageLocationStatusId,
        string? Code,
        string? Name,
        string? Description,
        bool IsPickable);

    private static async Task<IResult> CreateStorageLocationAsync(
        Guid warehouseId,
        Guid zoneId,
        CreateStorageLocationRequest request,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var command = new CreateStorageLocation.Command(
            WarehouseId: warehouseId,
            ZoneId: zoneId,
            StorageLocationTypeId: request.StorageLocationTypeId,
            StorageLocationStatusId: request.StorageLocationStatusId,
            Code: request.Code,
            Name: request.Name,
            Description: request.Description,
            IsPickable: request.IsPickable);

        var result = await commandDispatcher
            .DispatchAsync<CreateStorageLocation.Command, ServiceResult<StorageLocationDetails>>(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetStorageLocationByIdAsync(
        Guid storageLocationId,
        IQueryDispatcher queryDispatcher,
        CancellationToken cancellationToken)
    {
        var query = new GetStorageLocationById.Query(storageLocationId);

        var result = await queryDispatcher
            .DispatchAsync<GetStorageLocationById.Query, ServiceResult<StorageLocationDetails>>(query, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ListStorageLocationsByWarehouseAsync(
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
        var query = new ListStorageLocations.Query
        {
            WarehouseId = warehouseId,
            Skip = skip ?? 0,
            Take = take ?? ListQuery.DefaultTake,
            SearchText = searchText,
            SortBy = sortBy,
            SortDescending = sortDescending ?? false,
            IncludeInactive = includeInactive ?? false
        };

        var result = await queryDispatcher
            .DispatchAsync<ListStorageLocations.Query, ServiceResult<ListResult<StorageLocationDetails>>>(query, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> LookupStorageLocationsAsync(
        Guid warehouseId,
        [AsParameters] LookupStorageLocationsRequest request,
        IQueryDispatcher queryDispatcher,
        CancellationToken cancellationToken)
    {
        var query = new LookupStorageLocations.Query
        {
            WarehouseId = warehouseId,
            SearchText = request.SearchText,
            Take = request.Take,
            SelectableOnly = request.SelectableOnly
        };

        var result = await queryDispatcher
            .DispatchAsync<LookupStorageLocations.Query, ServiceResult<IReadOnlyList<StorageLocationLookupItem>>>(
                query,
                cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> ListStorageLocationsByZoneAsync(
        Guid zoneId,
        int? skip,
        int? take,
        string? searchText,
        string? sortBy,
        bool? sortDescending,
        bool? includeInactive,
        IQueryDispatcher queryDispatcher,
        CancellationToken cancellationToken)
    {
        var query = new ListStorageLocations.Query
        {
            ZoneId = zoneId,
            Skip = skip ?? 0,
            Take = take ?? ListQuery.DefaultTake,
            SearchText = searchText,
            SortBy = sortBy,
            SortDescending = sortDescending ?? false,
            IncludeInactive = includeInactive ?? false
        };

        var result = await queryDispatcher
            .DispatchAsync<ListStorageLocations.Query, ServiceResult<ListResult<StorageLocationDetails>>>(query, cancellationToken);
        return result.ToHttpResult();
    }

    private sealed record UpdateStorageLocationDetailsRequest(
        string? Name,
        string? Description,
        bool IsPickable);

    private static async Task<IResult> UpdateStorageLocationDetailsAsync(
        Guid storageLocationId,
        UpdateStorageLocationDetailsRequest request,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var command = new UpdateStorageLocationDetails.Command(
            StorageLocationId: storageLocationId,
            Name: request.Name,
            Description: request.Description,
            IsPickable: request.IsPickable);

        var result = await commandDispatcher
            .DispatchAsync<UpdateStorageLocationDetails.Command, ServiceResult<StorageLocationDetails>>(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeactivateStorageLocationAsync(
        Guid storageLocationId,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var command = new DeactivateStorageLocation.Command(storageLocationId);

        var result = await commandDispatcher
            .DispatchAsync<DeactivateStorageLocation.Command, ServiceResult<StorageLocationDetails>>(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ReactivateStorageLocationAsync(
        Guid storageLocationId,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var command = new ReactivateStorageLocation.Command(storageLocationId);

        var result = await commandDispatcher
            .DispatchAsync<ReactivateStorageLocation.Command, ServiceResult<StorageLocationDetails>>(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ListStorageLocationTypesAsync(
        bool? includeInactive,
        IQueryDispatcher queryDispatcher,
        CancellationToken cancellationToken)
    {
        var query = new ListStorageLocationTypes.Query(IncludeInactive: includeInactive ?? false);

        var result = await queryDispatcher
            .DispatchAsync<ListStorageLocationTypes.Query, ServiceResult<IReadOnlyList<StorageLocationTypeDetails>>>(query, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ListStorageLocationStatusesAsync(
        bool? includeInactive,
        IQueryDispatcher queryDispatcher,
        CancellationToken cancellationToken)
    {
        var query = new ListStorageLocationStatuses.Query(IncludeInactive: includeInactive ?? false);

        var result = await queryDispatcher
            .DispatchAsync<ListStorageLocationStatuses.Query, ServiceResult<IReadOnlyList<StorageLocationStatusDetails>>>(
                query,
                cancellationToken);

        return result.ToHttpResult();
    }
}
