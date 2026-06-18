using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Topology;
using Myrmex.WebApp.Wms.Api;
using System.Web;

namespace Myrmex.WebApp.Wms.Topology;

public sealed class WmsTopologyApiClient(HttpClient httpClient)
{
    public async Task<ListResult<WarehouseDetails>> ListWarehousesAsync(
        ListRequest request,
        CancellationToken cancellationToken = default)
    {
        string url = WmsApiUrls.BuildListUrl(
            "/api/wms/topology/warehouses",
            request);

        return await httpClient.GetRequiredAsync<ListResult<WarehouseDetails>>(url, cancellationToken);
    }

    public async Task<WarehouseDetails> GetWarehouseByIdAsync(
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.GetRequiredAsync<WarehouseDetails>(
            $"/api/wms/topology/warehouses/{warehouseId}",
            cancellationToken);
    }

    public async Task<ApiResult<WarehouseDetails>> TryCreateWarehouseAsync(
        CreateWarehouseRequest request,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PostAsApiResultAsync<WarehouseDetails>(
            "/api/wms/topology/warehouses",
            request,
            cancellationToken);
    }

    public async Task<ApiResult<WarehouseDetails>> TryUpdateWarehouseDetailsAsync(
        Guid warehouseId,
        UpdateWarehouseDetailsRequest request,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PutAsApiResultAsync<WarehouseDetails>(
            $"/api/wms/topology/warehouses/{warehouseId}",
            request,
            cancellationToken);
    }

    public async Task<ApiResult<WarehouseDetails>> TryDeactivateWarehouseAsync(
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PostAsApiResultAsync<WarehouseDetails>(
            $"/api/wms/topology/warehouses/{warehouseId}/deactivate",
            value: null,
            cancellationToken);
    }

    public async Task<ApiResult<WarehouseDetails>> TryReactivateWarehouseAsync(
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PostAsApiResultAsync<WarehouseDetails>(
            $"/api/wms/topology/warehouses/{warehouseId}/reactivate",
            value: null,
            cancellationToken);
    }

    public async Task<ListResult<ZoneDetails>> ListZonesAsync(
        Guid warehouseId,
        ListRequest request,
        CancellationToken cancellationToken = default)
    {
        string url = WmsApiUrls.BuildListUrl(
            $"/api/wms/topology/warehouses/{warehouseId}/zones",
            request);

        return await httpClient.GetRequiredAsync<ListResult<ZoneDetails>>(url, cancellationToken);
    }

    public async Task<ZoneDetails> GetZoneByIdAsync(
        Guid zoneId,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.GetRequiredAsync<ZoneDetails>(
            $"/api/wms/topology/zones/{zoneId}",
            cancellationToken);
    }

    public async Task<ApiResult<ZoneDetails>> TryCreateZoneAsync(
        Guid warehouseId,
        CreateZoneRequest request,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PostAsApiResultAsync<ZoneDetails>(
            $"/api/wms/topology/warehouses/{warehouseId}/zones",
            request,
            cancellationToken);
    }

    public async Task<ApiResult<ZoneDetails>> TryUpdateZoneDetailsAsync(
        Guid zoneId,
        UpdateZoneDetailsRequest request,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PutAsApiResultAsync<ZoneDetails>(
            $"/api/wms/topology/zones/{zoneId}",
            request,
            cancellationToken);
    }

    public async Task<ApiResult<ZoneDetails>> TryDeactivateZoneAsync(
        Guid zoneId,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PostAsApiResultAsync<ZoneDetails>(
            $"/api/wms/topology/zones/{zoneId}/deactivate",
            value: null,
            cancellationToken);
    }

    public async Task<ApiResult<ZoneDetails>> TryReactivateZoneAsync(
        Guid zoneId,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PostAsApiResultAsync<ZoneDetails>(
            $"/api/wms/topology/zones/{zoneId}/reactivate",
            value: null,
            cancellationToken);
    }

    public async Task<ListResult<StorageLocationDetails>> ListStorageLocationsByWarehouseAsync(
        Guid warehouseId,
        ListRequest request,
        CancellationToken cancellationToken = default)
    {
        string url = WmsApiUrls.BuildListUrl(
            $"/api/wms/topology/warehouses/{warehouseId}/locations",
            request);

        return await httpClient.GetRequiredAsync<ListResult<StorageLocationDetails>>(url, cancellationToken);
    }

    public async Task<IReadOnlyList<StorageLocationLookupItem>> LookupStorageLocationsAsync(
        Guid warehouseId,
        LookupStorageLocationsRequest request,
        CancellationToken cancellationToken = default)
    {
        string url = BuildStorageLocationLookupUrl(warehouseId, request);

        return await httpClient.GetRequiredAsync<IReadOnlyList<StorageLocationLookupItem>>(
            url,
            cancellationToken);
    }

    public async Task<ListResult<StorageLocationDetails>> ListStorageLocationsByZoneAsync(
        Guid zoneId,
        ListRequest request,
        CancellationToken cancellationToken = default)
    {
        string url = WmsApiUrls.BuildListUrl(
            $"/api/wms/topology/zones/{zoneId}/locations",
            request);

        return await httpClient.GetRequiredAsync<ListResult<StorageLocationDetails>>(url, cancellationToken);
    }

    public async Task<StorageLocationDetails> GetStorageLocationByIdAsync(
        Guid storageLocationId,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.GetRequiredAsync<StorageLocationDetails>(
            $"/api/wms/topology/locations/{storageLocationId}",
            cancellationToken);
    }

    public async Task<ApiResult<StorageLocationDetails>> TryCreateStorageLocationAsync(
        Guid warehouseId,
        Guid zoneId,
        CreateStorageLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PostAsApiResultAsync<StorageLocationDetails>(
            $"/api/wms/topology/warehouses/{warehouseId}/zones/{zoneId}/locations",
            request,
            cancellationToken);
    }

    public async Task<ApiResult<StorageLocationDetails>> TryUpdateStorageLocationDetailsAsync(
        Guid storageLocationId,
        UpdateStorageLocationDetailsRequest request,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PutAsApiResultAsync<StorageLocationDetails>(
            $"/api/wms/topology/locations/{storageLocationId}",
            request,
            cancellationToken);
    }

    public async Task<ApiResult<StorageLocationDetails>> TryDeactivateStorageLocationAsync(
        Guid storageLocationId,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PostAsApiResultAsync<StorageLocationDetails>(
            $"/api/wms/topology/locations/{storageLocationId}/deactivate",
            value: null,
            cancellationToken);
    }

    public async Task<ApiResult<StorageLocationDetails>> TryReactivateStorageLocationAsync(
        Guid storageLocationId,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PostAsApiResultAsync<StorageLocationDetails>(
            $"/api/wms/topology/locations/{storageLocationId}/reactivate",
            value: null,
            cancellationToken);
    }

    public async Task<IReadOnlyList<StorageLocationTypeDetails>> ListStorageLocationTypesAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        string url = includeInactive
            ? "/api/wms/topology/location-types?includeInactive=true"
            : "/api/wms/topology/location-types";

        return await httpClient.GetRequiredAsync<IReadOnlyList<StorageLocationTypeDetails>>(url, cancellationToken);
    }

    public async Task<IReadOnlyList<StorageLocationStatusDetails>> ListStorageLocationStatusesAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        string url = includeInactive
            ? "/api/wms/topology/location-statuses?includeInactive=true"
            : "/api/wms/topology/location-statuses";

        return await httpClient.GetRequiredAsync<IReadOnlyList<StorageLocationStatusDetails>>(url, cancellationToken);
    }

    private static string BuildStorageLocationLookupUrl(
        Guid warehouseId,
        LookupStorageLocationsRequest request)
    {
        string path = $"/api/wms/topology/warehouses/{warehouseId}/locations/lookup";

        List<string> query = [];

        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            query.Add($"searchText={HttpUtility.UrlEncode(request.SearchText)}");
        }

        if (request.Take.HasValue)
        {
            query.Add($"take={request.Take.Value}");
        }

        query.Add($"selectableOnly={request.SelectableOnly.ToString().ToLowerInvariant()}");

        return query.Count == 0
            ? path
            : $"{path}?{string.Join("&", query)}";
    }
}

public sealed record WarehouseDetails(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record CreateWarehouseRequest(
    string? Code,
    string? Name,
    string? Description);

public sealed record UpdateWarehouseDetailsRequest(
    string? Name,
    string? Description);

public sealed record ZoneDetails(
    Guid Id,
    Guid WarehouseId,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record CreateZoneRequest(
    string? Code,
    string? Name,
    string? Description);

public sealed record UpdateZoneDetailsRequest(
    string? Name,
    string? Description);

public sealed record StorageLocationDetails(
    Guid Id,
    Guid WarehouseId,
    Guid ZoneId,
    Guid StorageLocationTypeId,
    Guid StorageLocationStatusId,
    string Code,
    string Name,
    string? Description,
    bool IsPickable,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record CreateStorageLocationRequest(
    Guid StorageLocationTypeId,
    Guid StorageLocationStatusId,
    string? Code,
    string? Name,
    string? Description,
    bool IsPickable);

public sealed record UpdateStorageLocationDetailsRequest(
    string? Name,
    string? Description,
    bool IsPickable);

public sealed record StorageLocationTypeDetails(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsSystem,
    bool IsActive,
    int SortOrder);

public sealed record StorageLocationStatusDetails(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsSystem,
    bool IsActive,
    int SortOrder);
