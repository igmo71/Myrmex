using Microsoft.Extensions.Options;
using Myrmex.Integrations.OneC.Configuration;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Myrmex.Integrations.OneC.Transport;

internal sealed class OneCODataClient : IOneCODataClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly IOptions<OneCOptions> _options;

    public OneCODataClient(HttpClient httpClient, IOptions<OneCOptions> options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task TestConnectionAsync(CancellationToken cancellationToken)
    {
        OneCOptions options = _options.Value;
        Uri baseUri = ValidateAndGetBaseUri(options);

        await ProbeEntitySetAsync(baseUri, options.WarehousesEntitySet!, options, cancellationToken);
        await ProbeEntitySetAsync(baseUri, options.UnitsOfMeasureEntitySet, options, cancellationToken);
        await ProbeEntitySetAsync(baseUri, options.NomenclatureEntitySet!, options, cancellationToken);
    }

    private async Task ProbeEntitySetAsync(
        Uri baseUri,
        string entitySet,
        OneCOptions options,
        CancellationToken cancellationToken)
    {
        string relativeUrl = $"{Uri.EscapeDataString(entitySet)}?$format=json&$top=1&$orderby=Ref_Key&$select=Ref_Key";
        using HttpRequestMessage request = new(HttpMethod.Get, new Uri(baseUri, relativeUrl));
        string credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{options.Username}:{options.Password}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));

        try
        {
            using HttpResponseMessage response = await _httpClient.SendAsync(
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
            OneCODataCollectionResponse<ReferenceProbe>? envelope;
            try
            {
                envelope = await JsonSerializer.DeserializeAsync<OneCODataCollectionResponse<ReferenceProbe>>(
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

            if (envelope?.Value is null || envelope.Value.Any(item => item.Ref_Key == Guid.Empty))
            {
                throw new OneCTransportException(
                    OneCTransportFailureReason.MalformedResponse,
                    "The 1С OData service returned an invalid collection envelope.");
            }
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

    private sealed class ReferenceProbe
    {
        [JsonPropertyName("Ref_Key")]
        public Guid Ref_Key { get; init; }
    }
}
