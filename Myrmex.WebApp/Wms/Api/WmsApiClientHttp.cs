using Microsoft.AspNetCore.Mvc;

namespace Myrmex.WebApp.Wms.Api;

internal static class WmsApiClientHttp
{
    public static async Task<T> GetRequiredAsync<T>(
        this HttpClient httpClient,
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

    public static async Task<ApiResult<T>> PostAsApiResultAsync<T>(
        this HttpClient httpClient,
        string url,
        object? value,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(url, value, cancellationToken);

        return await ReadApiResultAsync<T>(response, $"POST '{url}'", cancellationToken);
    }

    public static async Task<ApiResult<T>> PostAsApiResultAsync<T>(
        this HttpClient httpClient,
        string url,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.PostAsync(
            url,
            content: null,
            cancellationToken);

        return await ReadApiResultAsync<T>(response, $"POST '{url}'", cancellationToken);
    }

    public static async Task<ApiResult<T>> PutAsApiResultAsync<T>(
        this HttpClient httpClient,
        string url,
        object value,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.PutAsJsonAsync(url, value, cancellationToken);

        return await ReadApiResultAsync<T>(response, $"PUT '{url}'", cancellationToken);
    }

    public static async Task<ApiResult<T>> DeleteAsApiResultAsync<T>(
        this HttpClient httpClient,
        string url,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.DeleteAsync(url, cancellationToken);

        return await ReadApiResultAsync<T>(response, $"DELETE '{url}'", cancellationToken);
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
}
