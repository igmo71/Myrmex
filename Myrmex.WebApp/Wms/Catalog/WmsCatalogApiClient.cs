using Microsoft.AspNetCore.Mvc;
using System.Web;

namespace Myrmex.WebApp.Wms.Catalog;

public sealed class WmsCatalogApiClient(HttpClient httpClient)
{
    public async Task<ListResult<StockKeepingUnitDetails>> ListStockKeepingUnitsAsync(
        ListRequest request,
        CancellationToken cancellationToken = default)
    {
        string url = BuildUrl(
            "/api/wms/catalog/skus",
            request);

        return await GetRequiredAsync<ListResult<StockKeepingUnitDetails>>(url, cancellationToken);
    }

    public async Task<StockKeepingUnitDetails> GetStockKeepingUnitByIdAsync(
        Guid stockKeepingUnitId,
        CancellationToken cancellationToken = default)
    {
        return await GetRequiredAsync<StockKeepingUnitDetails>(
            $"/api/wms/catalog/skus/{stockKeepingUnitId}",
            cancellationToken);
    }

    public async Task<ApiResult<StockKeepingUnitDetails>> TryCreateStockKeepingUnitAsync(
        CreateStockKeepingUnitRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostAsApiResultAsync<StockKeepingUnitDetails>(
            "/api/wms/catalog/skus",
            request,
            cancellationToken);
    }

    public async Task<ApiResult<UnitOfMeasureDetails>> TryCreateUnitOfMeasureAsync(
        CreateUnitOfMeasureRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostAsApiResultAsync<UnitOfMeasureDetails>(
            "/api/wms/catalog/uoms",
            request,
            cancellationToken);
    }

    public async Task<ApiResult<StockKeepingUnitDetails>> TryUpdateStockKeepingUnitDetailsAsync(
        Guid stockKeepingUnitId,
        UpdateStockKeepingUnitDetailsRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PutAsApiResultAsync<StockKeepingUnitDetails>(
            $"/api/wms/catalog/skus/{stockKeepingUnitId}",
            request,
            cancellationToken);
    }

    public async Task<ApiResult<StockKeepingUnitDetails>> TryDeactivateStockKeepingUnitAsync(
        Guid stockKeepingUnitId,
        CancellationToken cancellationToken = default)
    {
        return await PostAsApiResultAsync<StockKeepingUnitDetails>(
            $"/api/wms/catalog/skus/{stockKeepingUnitId}/deactivate",
            value: null,
            cancellationToken);
    }

    public async Task<ApiResult<StockKeepingUnitDetails>> TryReactivateStockKeepingUnitAsync(
        Guid stockKeepingUnitId,
        CancellationToken cancellationToken = default)
    {
        return await PostAsApiResultAsync<StockKeepingUnitDetails>(
            $"/api/wms/catalog/skus/{stockKeepingUnitId}/reactivate",
            value: null,
            cancellationToken);
    }

    private async Task<T> GetRequiredAsync<T>(
        string url,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(
            url,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw await ReadApiExceptionAsync(
                response,
                $"GET '{url}'",
                cancellationToken);
        }

        T? result = await response.Content.ReadFromJsonAsync<T>(cancellationToken);

        return result ?? throw new InvalidOperationException(
            $"API returned empty response for GET '{url}'.");
    }

    private async Task<ApiResult<T>> PostAsApiResultAsync<T>(
        string url,
        object? value,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(url, value, cancellationToken);

        return await ReadApiResultAsync<T>(response, $"POST '{url}'", cancellationToken);
    }

    private async Task<ApiResult<T>> PutAsApiResultAsync<T>(
        string url,
        object value,
        CancellationToken cancellationToken)
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

    private static async Task<ApiException> ReadApiExceptionAsync(
        HttpResponseMessage response,
        string operation,
        CancellationToken cancellationToken)
    {
        ApiError error = await ReadApiErrorAsync(
            response,
            operation,
            cancellationToken);

        return new ApiException(
            status: error.Status,
            message: error.Message,
            extensions: error.Extensions);
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

public sealed record StockKeepingUnitDetails(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record CreateStockKeepingUnitRequest(
    string? Code,
    string? Name,
    string? Description);

public sealed record UpdateStockKeepingUnitDetailsRequest(
    string? Name,
    string? Description);

public sealed record UnitOfMeasureDetails(
    Guid Id,
    string Code,
    string Name,
    string? Symbol,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record CreateUnitOfMeasureRequest(
    string? Code,
    string? Name,
    string? Symbol);
