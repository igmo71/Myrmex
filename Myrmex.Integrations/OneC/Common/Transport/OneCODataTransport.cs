using Microsoft.Extensions.Options;
using Myrmex.Integrations.OneC.Configuration;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Myrmex.Integrations.OneC.Common.Transport;

internal sealed class OneCODataTransport(
    HttpClient httpClient,
    IOptions<OneCOptions> options) : IOneCODataTransport
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    public void ValidateConfiguration() => _ = ValidateAndGetBaseUri(options.Value);

    public async Task<IReadOnlyList<T>> ReadCollectionAsync<T>(
        string entitySet,
        IEnumerable<KeyValuePair<string, string>> parameters,
        CancellationToken cancellationToken)
        where T : class
    {
        OneCOptions currentOptions = options.Value;
        Uri baseUri = ValidateAndGetBaseUri(currentOptions);
        string query = string.Join("&", parameters.Select(parameter =>
            $"{parameter.Key}={Uri.EscapeDataString(parameter.Value)}"));
        string relativeUrl = $"{Uri.EscapeDataString(entitySet)}?{query}";
        using HttpRequestMessage request = new(HttpMethod.Get, new Uri(baseUri, relativeUrl));
        string credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{currentOptions.Username}:{currentOptions.Password}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using CancellationTokenSource timeout =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(currentOptions.TimeoutSeconds));

        try
        {
            using HttpResponseMessage response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                timeout.Token);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                throw new OneCTransportException(
                    OneCTransportFailureReason.AuthenticationFailed,
                    "1С rejected the configured credentials.");
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new OneCTransportException(
                    OneCTransportFailureReason.EntitySetUnavailable,
                    $"The configured 1С entity set '{entitySet}' is unavailable.");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new OneCTransportException(
                    OneCTransportFailureReason.SourceUnavailable,
                    "The 1С OData service is unavailable.");
            }

            await using Stream content = await response.Content.ReadAsStreamAsync(timeout.Token);
            OneCODataCollectionResponse<T>? envelope;
            try
            {
                envelope = await JsonSerializer.DeserializeAsync<OneCODataCollectionResponse<T>>(
                    content,
                    SerializerOptions,
                    timeout.Token);
            }
            catch (JsonException exception)
            {
                throw new OneCTransportException(
                    OneCTransportFailureReason.MalformedResponse,
                    "The 1С OData service returned an invalid response.",
                    exception);
            }

            if (envelope?.Value is null)
            {
                throw new OneCTransportException(
                    OneCTransportFailureReason.MalformedResponse,
                    "The 1С OData service returned an invalid collection envelope.");
            }

            return envelope.Value;
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new OneCTransportException(
                OneCTransportFailureReason.Timeout,
                "The 1С OData request timed out.",
                exception);
        }
        catch (HttpRequestException exception)
        {
            throw new OneCTransportException(
                OneCTransportFailureReason.SourceUnavailable,
                "The 1С OData service is unavailable.",
                exception);
        }
    }

    private static Uri ValidateAndGetBaseUri(OneCOptions options)
    {
        if (!options.Enabled)
        {
            throw new OneCTransportException(
                OneCTransportFailureReason.Disabled,
                "The 1С integration is disabled.");
        }

        bool validBaseUrl = Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out Uri? baseUri)
            && baseUri.Scheme is "http" or "https";
        bool valid = validBaseUrl
            && !string.IsNullOrWhiteSpace(options.Username)
            && !string.IsNullOrWhiteSpace(options.Password)
            && !string.IsNullOrWhiteSpace(options.WarehousesEntitySet)
            && !string.IsNullOrWhiteSpace(options.UnitsOfMeasureEntitySet)
            && !string.IsNullOrWhiteSpace(options.NomenclatureEntitySet)
            && !string.IsNullOrWhiteSpace(options.ReceivingOrdersEntitySet)
            && options.BatchSize is >= 1 and <= OneCOptions.MaximumBatchSize
            && options.TimeoutSeconds > 0;

        if (!valid)
        {
            throw new OneCTransportException(
                OneCTransportFailureReason.InvalidConfiguration,
                "The 1С integration configuration is incomplete or invalid.");
        }

        string normalized = baseUri!.AbsoluteUri.EndsWith('/')
            ? baseUri.AbsoluteUri
            : baseUri.AbsoluteUri + "/";
        return new Uri(normalized, UriKind.Absolute);
    }
}
