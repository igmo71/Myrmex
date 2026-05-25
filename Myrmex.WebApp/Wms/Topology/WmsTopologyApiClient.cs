using Microsoft.AspNetCore.Mvc;
using System.Web;

namespace Myrmex.WebApp.Wms.Topology;

public sealed class WmsTopologyApiClient(HttpClient httpClient)
{
    public async Task<ListResult<WarehouseDetails>> ListWarehousesAsync(
        ListRequest request,
        CancellationToken cancellationToken = default)
    {
        string url = BuildUrl("/api/wms/topology/warehouses", request);

        return await GetRequiredAsync<ListResult<WarehouseDetails>>(url, cancellationToken);
    }

    public async Task<WarehouseDetails> GetWarehouseByIdAsync(
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        return await GetRequiredAsync<WarehouseDetails>(
            $"/api/wms/topology/warehouses/{warehouseId}", cancellationToken);
    }

    public async Task<WarehouseDetails> CreateWarehouseAsync(
        CreateWarehouseRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostRequiredAsync<WarehouseDetails>(
            "/api/wms/topology/warehouses", request, cancellationToken);
    }

    public async Task<ApiResult<WarehouseDetails>> TryCreateWarehouseAsync(
        CreateWarehouseRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostAsApiResultAsync<WarehouseDetails>(
            "/api/wms/topology/warehouses", request, cancellationToken);
    }

    public async Task<WarehouseDetails> UpdateWarehouseDetailsAsync(
        Guid warehouseId,
        UpdateWarehouseDetailsRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PutRequiredAsync<WarehouseDetails>(
            $"/api/wms/topology/warehouses/{warehouseId}", request, cancellationToken);
    }

    public async Task<ApiResult<WarehouseDetails>> TryUpdateWarehouseDetailsAsync(
        Guid warehouseId,
        UpdateWarehouseDetailsRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PutAsApiResultAsync<WarehouseDetails>(
            $"/api/wms/topology/warehouses/{warehouseId}", request, cancellationToken);
    }

    public async Task<WarehouseDetails> DeactivateWarehouseAsync(
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        return await PostRequiredAsync<WarehouseDetails>(
            $"/api/wms/topology/warehouses/{warehouseId}/deactivate", value: null, cancellationToken);
    }

    public async Task<WarehouseDetails> ReactivateWarehouseAsync(
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        return await PostRequiredAsync<WarehouseDetails>(
            $"/api/wms/topology/warehouses/{warehouseId}/reactivate", value: null, cancellationToken);
    }

    public async Task<ApiResult<WarehouseDetails>> TryDeactivateWarehouseAsync(
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        return await PostAsApiResultAsync<WarehouseDetails>(
            $"/api/wms/topology/warehouses/{warehouseId}/deactivate",
            value: null,
            cancellationToken);
    }

    public async Task<ApiResult<WarehouseDetails>> TryReactivateWarehouseAsync(
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        return await PostAsApiResultAsync<WarehouseDetails>(
            $"/api/wms/topology/warehouses/{warehouseId}/reactivate",
            value: null,
            cancellationToken);
    }

    public async Task<ListResult<ZoneDetails>> ListZonesAsync(
        Guid warehouseId,
        ListRequest request,
        CancellationToken cancellationToken = default)
    {
        string url = BuildUrl($"/api/wms/topology/warehouses/{warehouseId}/zones", request);

        return await GetRequiredAsync<ListResult<ZoneDetails>>(url, cancellationToken);
    }

    public async Task<ZoneDetails> GetZoneByIdAsync(
        Guid zoneId,
        CancellationToken cancellationToken = default)
    {
        return await GetRequiredAsync<ZoneDetails>(
            $"/api/wms/topology/zones/{zoneId}", cancellationToken);
    }

    public async Task<ZoneDetails> CreateZoneAsync(
        Guid warehouseId,
        CreateZoneRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostRequiredAsync<ZoneDetails>(
            $"/api/wms/topology/warehouses/{warehouseId}/zones", request, cancellationToken);
    }

    public async Task<ApiResult<ZoneDetails>> TryCreateZoneAsync(
        Guid warehouseId,
        CreateZoneRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostAsApiResultAsync<ZoneDetails>(
            $"/api/wms/topology/warehouses/{warehouseId}/zones", request, cancellationToken);
    }

    public async Task<ZoneDetails> UpdateZoneDetailsAsync(
        Guid zoneId,
        UpdateZoneDetailsRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PutRequiredAsync<ZoneDetails>(
            $"/api/wms/topology/zones/{zoneId}", request, cancellationToken);
    }

    public async Task<ApiResult<ZoneDetails>> TryUpdateZoneDetailsAsync(
        Guid zoneId,
        UpdateZoneDetailsRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PutAsApiResultAsync<ZoneDetails>(
            $"/api/wms/topology/zones/{zoneId}", request, cancellationToken);
    }

    public async Task<ZoneDetails> DeactivateZoneAsync(
        Guid zoneId,
        CancellationToken cancellationToken = default)
    {
        return await PostRequiredAsync<ZoneDetails>(
            $"/api/wms/topology/zones/{zoneId}/deactivate", value: null, cancellationToken);
    }

    public async Task<ZoneDetails> ReactivateZoneAsync(
        Guid zoneId,
        CancellationToken cancellationToken = default)
    {
        return await PostRequiredAsync<ZoneDetails>(
            $"/api/wms/topology/zones/{zoneId}/reactivate", value: null, cancellationToken);
    }

    public async Task<ApiResult<ZoneDetails>> TryDeactivateZoneAsync(
    Guid zoneId,
    CancellationToken cancellationToken = default)
    {
        return await PostAsApiResultAsync<ZoneDetails>(
            $"/api/wms/topology/zones/{zoneId}/deactivate", value: null, cancellationToken);
    }

    public async Task<ApiResult<ZoneDetails>> TryReactivateZoneAsync(
        Guid zoneId,
        CancellationToken cancellationToken = default)
    {
        return await PostAsApiResultAsync<ZoneDetails>(
            $"/api/wms/topology/zones/{zoneId}/reactivate", value: null, cancellationToken);
    }

    public async Task<ListResult<StorageLocationDetails>> ListStorageLocationsByWarehouseAsync(
        Guid warehouseId,
        ListRequest request,
        CancellationToken cancellationToken = default)
    {
        string url = BuildUrl($"/api/wms/topology/warehouses/{warehouseId}/locations", request);

        return await GetRequiredAsync<ListResult<StorageLocationDetails>>(url, cancellationToken);
    }

    public async Task<ListResult<StorageLocationDetails>> ListStorageLocationsByZoneAsync(
        Guid zoneId,
        ListRequest request,
        CancellationToken cancellationToken = default)
    {
        string url = BuildUrl($"/api/wms/topology/zones/{zoneId}/locations", request);

        return await GetRequiredAsync<ListResult<StorageLocationDetails>>(url, cancellationToken);
    }

    public async Task<StorageLocationDetails> GetStorageLocationByIdAsync(
        Guid storageLocationId,
        CancellationToken cancellationToken = default)
    {
        return await GetRequiredAsync<StorageLocationDetails>(
            $"/api/wms/topology/locations/{storageLocationId}", cancellationToken);
    }

    public async Task<StorageLocationDetails> CreateStorageLocationAsync(
        Guid warehouseId,
        Guid zoneId,
        CreateStorageLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostRequiredAsync<StorageLocationDetails>(
            $"/api/wms/topology/warehouses/{warehouseId}/zones/{zoneId}/locations", request, cancellationToken);
    }

    public async Task<ApiResult<StorageLocationDetails>> TryCreateStorageLocationAsync(
        Guid warehouseId,
        Guid zoneId,
        CreateStorageLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostAsApiResultAsync<StorageLocationDetails>(
            $"/api/wms/topology/warehouses/{warehouseId}/zones/{zoneId}/locations", request, cancellationToken);
    }

    public async Task<StorageLocationDetails> UpdateStorageLocationDetailsAsync(
        Guid storageLocationId,
        UpdateStorageLocationDetailsRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PutRequiredAsync<StorageLocationDetails>(
            $"/api/wms/topology/locations/{storageLocationId}", request, cancellationToken);
    }

    public async Task<ApiResult<StorageLocationDetails>> TryUpdateStorageLocationDetailsAsync(
        Guid storageLocationId,
        UpdateStorageLocationDetailsRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PutAsApiResultAsync<StorageLocationDetails>(
            $"/api/wms/topology/locations/{storageLocationId}", request, cancellationToken);
    }

    public async Task<StorageLocationDetails> DeactivateStorageLocationAsync(
        Guid storageLocationId,
        CancellationToken cancellationToken = default)
    {
        return await PostRequiredAsync<StorageLocationDetails>(
            $"/api/wms/topology/locations/{storageLocationId}/deactivate", value: null, cancellationToken);
    }

    public async Task<StorageLocationDetails> ReactivateStorageLocationAsync(
        Guid storageLocationId,
        CancellationToken cancellationToken = default)
    {
        return await PostRequiredAsync<StorageLocationDetails>(
            $"/api/wms/topology/locations/{storageLocationId}/reactivate", value: null, cancellationToken);
    }

    public async Task<ApiResult<StorageLocationDetails>> TryDeactivateStorageLocationAsync(
    Guid storageLocationId,
    CancellationToken cancellationToken = default)
    {
        return await PostAsApiResultAsync<StorageLocationDetails>(
            $"/api/wms/topology/locations/{storageLocationId}/deactivate", value: null, cancellationToken);
    }

    public async Task<ApiResult<StorageLocationDetails>> TryReactivateStorageLocationAsync(
        Guid storageLocationId,
        CancellationToken cancellationToken = default)
    {
        return await PostAsApiResultAsync<StorageLocationDetails>(
            $"/api/wms/topology/locations/{storageLocationId}/reactivate", value: null, cancellationToken);
    }

    public async Task<IReadOnlyList<StorageLocationTypeDetails>> ListStorageLocationTypesAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        string url = includeInactive
            ? "/api/wms/topology/location-types?includeInactive=true"
            : "/api/wms/topology/location-types";

        return await GetRequiredAsync<IReadOnlyList<StorageLocationTypeDetails>>(url, cancellationToken);
    }

    public async Task<IReadOnlyList<StorageLocationStatusDetails>> ListStorageLocationStatusesAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        string url = includeInactive
            ? "/api/wms/topology/location-statuses?includeInactive=true"
            : "/api/wms/topology/location-statuses";

        return await GetRequiredAsync<IReadOnlyList<StorageLocationStatusDetails>>(url, cancellationToken);
    }

    private async Task<T> GetRequiredAsync<T>(string url, CancellationToken cancellationToken)
    {
        T? result = await httpClient.GetFromJsonAsync<T>(url, cancellationToken);

        return result ?? throw new InvalidOperationException($"API returned empty response for GET '{url}'.");
    }

    private async Task<T> PostRequiredAsync<T>(string url, object? value, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(url, value, cancellationToken);

        response.EnsureSuccessStatusCode();

        T? result = await response.Content.ReadFromJsonAsync<T>(cancellationToken);

        return result ?? throw new InvalidOperationException($"API returned empty response for POST '{url}'.");
    }

    private async Task<T> PutRequiredAsync<T>(string url, object value, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.PutAsJsonAsync(url, value, cancellationToken);

        response.EnsureSuccessStatusCode();

        T? result = await response.Content.ReadFromJsonAsync<T>(cancellationToken);

        return result ?? throw new InvalidOperationException($"API returned empty response for PUT '{url}'.");
    }

    private async Task<ApiResult<T>> PostAsApiResultAsync<T>(string url, object? value, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(url, value, cancellationToken);

        return await ReadApiResultAsync<T>(response, $"POST '{url}'", cancellationToken);
    }

    private async Task<ApiResult<T>> PutAsApiResultAsync<T>(string url, object value, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.PutAsJsonAsync(url, value, cancellationToken);

        return await ReadApiResultAsync<T>(response, $"PUT '{url}'", cancellationToken);
    }

    private static async Task<ApiResult<T>> ReadApiResultAsync<T>(
        HttpResponseMessage response,
        string operation,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            ApiError error = await ReadApiErrorAsync(response, operation, cancellationToken);

            return ApiResult<T>.Failure(error);
        }

        T? result = await response.Content.ReadFromJsonAsync<T>(cancellationToken);

        if (result is null)
        {
            return ApiResult<T>.Failure(ApiError.Create(
                status: (int)response.StatusCode,
                message: $"API returned empty response for {operation}."));
        }

        return ApiResult<T>.Success(result);
    }

    private static async Task<ApiError> ReadApiErrorAsync(
        HttpResponseMessage response,
        string operation,
        CancellationToken cancellationToken)
    {
        string fallbackMessage =
            $"API request failed for {operation}. Status code: {(int)response.StatusCode} {response.StatusCode}.";

        try
        {
            ProblemDetails? problemDetails = await response.Content
                .ReadFromJsonAsync<ProblemDetails>(cancellationToken);

            if (problemDetails is not null)
            {
                string message = problemDetails.Detail
                    ?? problemDetails.Title
                    ?? fallbackMessage;

                Dictionary<string, string> extensions = [];

                foreach (KeyValuePair<string, object?> extension in problemDetails.Extensions)
                {
                    string? value = extension.Value?.ToString();

                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        extensions[extension.Key] = value;
                    }
                }

                return ApiError.Create(
                    status: problemDetails.Status ?? (int)response.StatusCode,
                    message,
                    extensions);
            }
        }
        catch
        {
            // Ignore malformed/unexpected error payload and use safe fallback message.
        }

        return ApiError.Create(
            status: (int)response.StatusCode,
            message: fallbackMessage);
    }

    private static string BuildUrl(string path, ListRequest request)
    {
        List<string> query = [];

        query.Add($"skip={request.Skip}");
        query.Add($"take={request.Take}");

        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            query.Add($"searchText={HttpUtility.UrlEncode(request.SearchText)}");
        }

        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            query.Add($"sortBy={HttpUtility.UrlEncode(request.SortBy)}");
        }

        query.Add($"sortDescending={request.SortDescending.ToString().ToLowerInvariant()}");
        query.Add($"includeInactive={request.IncludeInactive.ToString().ToLowerInvariant()}");

        return $"{path}?{string.Join("&", query)}";
    }
}

public sealed record ListRequest(
    int Skip = 0,
    int Take = 20,
    string? SearchText = null,
    string? SortBy = null,
    bool SortDescending = false,
    bool IncludeInactive = false);

public sealed record ListResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Skip,
    int Take);

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
