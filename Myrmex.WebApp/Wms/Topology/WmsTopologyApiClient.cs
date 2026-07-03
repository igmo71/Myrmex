using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Topology;
using Myrmex.WebApp.Wms.Api;
using System.Web;

namespace Myrmex.WebApp.Wms.Topology;

public sealed class WmsTopologyApiClient(HttpClient httpClient)
{
    public async Task<ListResult<WarehouseDetails>> ListWarehousesAsync(
        ListWarehousesRequest request,
        CancellationToken cancellationToken = default)
    {
        string url = BuildWarehouseListUrl(request);

        return await httpClient.GetRequiredAsync<ListResult<WarehouseDetails>>(url, cancellationToken);
    }

    public async Task<IReadOnlyList<WarehouseLookupItem>> LookupWarehousesAsync(
        LookupWarehousesRequest request,
        CancellationToken cancellationToken = default)
    {
        string url = BuildWarehouseLookupUrl(request);

        return await httpClient.GetRequiredAsync<IReadOnlyList<WarehouseLookupItem>>(
            url,
            cancellationToken);
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
        ListZonesRequest request,
        CancellationToken cancellationToken = default)
    {
        string url = BuildZoneListUrl(warehouseId, request);

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
        ListStorageLocationsRequest request,
        CancellationToken cancellationToken = default)
    {
        string url = BuildStorageLocationListUrl(
            $"/api/wms/topology/warehouses/{warehouseId}/locations",
            request,
            includeWarehouseId: false,
            includeZoneId: true);

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
        ListStorageLocationsRequest request,
        CancellationToken cancellationToken = default)
    {
        string url = BuildStorageLocationListUrl(
            $"/api/wms/topology/zones/{zoneId}/locations",
            request,
            includeWarehouseId: true,
            includeZoneId: false);

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

    private static string BuildWarehouseListUrl(ListWarehousesRequest request)
    {
        return BuildTopologyListUrl(
            "/api/wms/topology/warehouses",
            request.Skip,
            request.Take,
            request.SearchText,
            request.SortBy,
            request.SortDescending,
            request.IncludeInactive);
    }

    private static string BuildWarehouseLookupUrl(LookupWarehousesRequest request)
    {
        const string path = "/api/wms/topology/warehouses/lookup";
        List<string> query = [];

        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            query.Add($"searchText={HttpUtility.UrlEncode(request.SearchText)}");
        }

        if (request.Take.HasValue)
        {
            query.Add($"take={request.Take.Value}");
        }

        if (request.SelectableOnly.HasValue)
        {
            query.Add($"selectableOnly={request.SelectableOnly.Value.ToString().ToLowerInvariant()}");
        }

        return query.Count == 0
            ? path
            : $"{path}?{string.Join("&", query)}";
    }

    private static string BuildZoneListUrl(Guid warehouseId, ListZonesRequest request)
    {
        return BuildTopologyListUrl(
            $"/api/wms/topology/warehouses/{warehouseId}/zones",
            request.Skip,
            request.Take,
            request.SearchText,
            request.SortBy,
            request.SortDescending,
            request.IncludeInactive);
    }

    private static string BuildStorageLocationListUrl(
        string path,
        ListStorageLocationsRequest request,
        bool includeWarehouseId,
        bool includeZoneId)
    {
        List<string> query = BuildTopologyListQuery(
            request.Skip,
            request.Take,
            request.SearchText,
            request.SortBy,
            request.SortDescending,
            request.IncludeInactive);

        if (includeWarehouseId && request.WarehouseId.HasValue)
        {
            query.Add($"warehouseId={request.WarehouseId.Value}");
        }

        if (includeZoneId && request.ZoneId.HasValue)
        {
            query.Add($"zoneId={request.ZoneId.Value}");
        }

        if (request.StorageLocationTypeId.HasValue)
        {
            query.Add($"storageLocationTypeId={request.StorageLocationTypeId.Value}");
        }

        if (request.StorageLocationStatusId.HasValue)
        {
            query.Add($"storageLocationStatusId={request.StorageLocationStatusId.Value}");
        }

        return query.Count == 0
            ? path
            : $"{path}?{string.Join("&", query)}";
    }

    private static string BuildTopologyListUrl(
        string path,
        int? skip,
        int? take,
        string? searchText,
        string? sortBy,
        bool? sortDescending,
        bool? includeInactive)
    {
        List<string> query = BuildTopologyListQuery(
            skip,
            take,
            searchText,
            sortBy,
            sortDescending,
            includeInactive);

        return query.Count == 0
            ? path
            : $"{path}?{string.Join("&", query)}";
    }

    private static List<string> BuildTopologyListQuery(
        int? skip,
        int? take,
        string? searchText,
        string? sortBy,
        bool? sortDescending,
        bool? includeInactive)
    {
        List<string> query = [];

        if (skip.HasValue)
        {
            query.Add($"skip={skip.Value}");
        }

        if (take.HasValue)
        {
            query.Add($"take={take.Value}");
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            query.Add($"searchText={HttpUtility.UrlEncode(searchText)}");
        }

        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            query.Add($"sortBy={HttpUtility.UrlEncode(sortBy)}");
        }

        if (sortDescending.HasValue)
        {
            query.Add($"sortDescending={sortDescending.Value.ToString().ToLowerInvariant()}");
        }

        if (includeInactive.HasValue)
        {
            query.Add($"includeInactive={includeInactive.Value.ToString().ToLowerInvariant()}");
        }

        return query;
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

        if (request.SelectableOnly.HasValue)
        {
            query.Add($"selectableOnly={request.SelectableOnly.Value.ToString().ToLowerInvariant()}");
        }

        if (!string.IsNullOrWhiteSpace(request.StorageLocationTypeCode))
        {
            query.Add($"storageLocationTypeCode={HttpUtility.UrlEncode(request.StorageLocationTypeCode)}");
        }

        if (request.ExcludeTransitTypes == true)
        {
            query.Add("excludeTransitTypes=true");
        }

        return query.Count == 0
            ? path
            : $"{path}?{string.Join("&", query)}";
    }
}
