using Microsoft.AspNetCore.Mvc;

namespace Myrmex.WebApp.Wms.Catalog;

public sealed class WmsCatalogApiClient(HttpClient httpClient)
{
    public async Task<ApiResult<StockKeepingUnitDetails>> TryCreateStockKeepingUnitAsync(
        CreateStockKeepingUnitRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostAsApiResultAsync<StockKeepingUnitDetails>(
            "/api/wms/catalog/skus",
            request,
            cancellationToken);
    }

    private async Task<ApiResult<T>> PostAsApiResultAsync<T>(
        string url,
        object? value,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(url, value, cancellationToken);

        return await ReadApiResultAsync<T>(response, $"POST '{url}'", cancellationToken);
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
}

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
