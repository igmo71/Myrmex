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

    public void ValidateConfiguration()
    {
        _ = ValidateAndGetBaseUri(_options.Value);
    }

    public Task<IReadOnlyList<Catalog_Склады>> ReadWarehousesAsync(
        CancellationToken cancellationToken)
    {
        OneCOptions options = _options.Value;
        Uri baseUri = ValidateAndGetBaseUri(options);
        string select = options.WarehouseCodeAvailable
            ? "Ref_Key,DeletionMark,IsFolder,Code,Description"
            : "Ref_Key,DeletionMark,IsFolder,Description";

        List<KeyValuePair<string, string>> parameters =
        [
            new("$format", "json"),
            new("$select", select),
            new("$orderby", "Ref_Key")
        ];
        if (options.UseFolderFilter)
        {
            parameters.Add(new("$filter", "IsFolder eq false"));
        }

        return ReadCollectionAsync<Catalog_Склады>(
            baseUri,
            options.WarehousesEntitySet!,
            parameters,
            options,
            cancellationToken);
    }

    public Task<IReadOnlyList<Catalog_УпаковкиЕдиницыИзмерения>> ReadUnitsOfMeasureAsync(
        CancellationToken cancellationToken)
    {
        OneCOptions options = _options.Value;
        Uri baseUri = ValidateAndGetBaseUri(options);
        KeyValuePair<string, string>[] parameters =
        [
            new("$format", "json"),
            new("$select", "Ref_Key,DeletionMark,Code,Description,НаименованиеПолное,МеждународноеСокращение"),
            new("$orderby", "Ref_Key")
        ];

        return ReadCollectionAsync<Catalog_УпаковкиЕдиницыИзмерения>(
            baseUri,
            options.UnitsOfMeasureEntitySet,
            parameters,
            options,
            cancellationToken);
    }

    private async Task ProbeEntitySetAsync(
        Uri baseUri,
        string entitySet,
        OneCOptions options,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ReferenceProbe> items = await ReadCollectionAsync<ReferenceProbe>(
            baseUri,
            entitySet,
            [
                new("$format", "json"),
                new("$top", "1"),
                new("$orderby", "Ref_Key"),
                new("$select", "Ref_Key")
            ],
            options,
            cancellationToken);

        if (items.Any(item => item.Ref_Key == Guid.Empty))
        {
            throw new OneCTransportException(
                OneCTransportFailureReason.MalformedResponse,
                "The 1С OData service returned an invalid collection envelope.");
        }
    }

    private async Task<IReadOnlyList<T>> ReadCollectionAsync<T>(
        Uri baseUri,
        string entitySet,
        IEnumerable<KeyValuePair<string, string>> parameters,
        OneCOptions options,
        CancellationToken cancellationToken)
    {
        string query = string.Join("&", parameters.Select(parameter =>
            $"{parameter.Key}={Uri.EscapeDataString(parameter.Value)}"));
        string relativeUrl = $"{Uri.EscapeDataString(entitySet)}?{query}";
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
