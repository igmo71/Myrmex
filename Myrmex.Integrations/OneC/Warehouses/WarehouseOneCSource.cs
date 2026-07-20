using Microsoft.Extensions.Options;
using Myrmex.Integrations.OneC.Common.Transport;
using Myrmex.Integrations.OneC.Configuration;
using System.Text.Json.Serialization;

namespace Myrmex.Integrations.OneC.Warehouses;

internal interface IWarehouseOneCSource
{
    Task<IReadOnlyList<WarehouseSourceRecord>> ReadAllAsync(CancellationToken cancellationToken);

    Task<WarehouseSourceRecord?> ReadCurrentAsync(
        Guid externalRefKey,
        CancellationToken cancellationToken);

    Task ProbeAsync(CancellationToken cancellationToken);
}

internal sealed class WarehouseOneCSource(
    IOneCODataTransport transport,
    IOptions<OneCOptions> options) : IWarehouseOneCSource
{
    private const int MaximumDataVersionLength = 128;

    public Task<IReadOnlyList<WarehouseSourceRecord>> ReadAllAsync(
        CancellationToken cancellationToken)
    {
        OneCOptions currentOptions = options.Value;
        List<KeyValuePair<string, string>> parameters =
        [
            new("$format", "json"),
            new("$select", Select(currentOptions)),
            new("$orderby", "Ref_Key")
        ];
        if (currentOptions.UseFolderFilter)
        {
            parameters.Add(new("$filter", "IsFolder eq false"));
        }

        return transport.ReadCollectionAsync<WarehouseSourceRecord>(
            currentOptions.WarehousesEntitySet!,
            parameters,
            cancellationToken);
    }

    public async Task<WarehouseSourceRecord?> ReadCurrentAsync(
        Guid externalRefKey,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(externalRefKey, Guid.Empty);
        IReadOnlyList<WarehouseSourceRecord> records =
            await transport.ReadCollectionAsync<WarehouseSourceRecord>(
                options.Value.WarehousesEntitySet!,
                CurrentParameters(Select(options.Value), externalRefKey),
                cancellationToken);
        return ValidateCurrent(records, externalRefKey);
    }

    public async Task ProbeAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<ReferenceProbe> records = await transport.ReadCollectionAsync<ReferenceProbe>(
            options.Value.WarehousesEntitySet!,
            ProbeParameters(),
            cancellationToken);
        ValidateProbe(records);
    }

    private static string Select(OneCOptions options) => options.WarehouseCodeAvailable
        ? "Ref_Key,DataVersion,DeletionMark,IsFolder,Code,Description"
        : "Ref_Key,DataVersion,DeletionMark,IsFolder,Description";

    private static KeyValuePair<string, string>[] CurrentParameters(
        string select,
        Guid externalRefKey) =>
    [
        new("$format", "json"),
        new("$select", select),
        new("$filter", $"Ref_Key eq guid'{externalRefKey:D}'"),
        new("$top", "2")
    ];

    private static KeyValuePair<string, string>[] ProbeParameters() =>
    [
        new("$format", "json"),
        new("$top", "1"),
        new("$orderby", "Ref_Key"),
        new("$select", "Ref_Key")
    ];

    private static WarehouseSourceRecord? ValidateCurrent(
        IReadOnlyList<WarehouseSourceRecord> records,
        Guid externalRefKey)
    {
        if (records.Count == 0)
        {
            return null;
        }

        if (records.Count != 1 || records[0].Ref_Key != externalRefKey ||
            records[0].DataVersion.Length is < 1 or > MaximumDataVersionLength)
        {
            throw MalformedCurrentObject();
        }

        return records[0];
    }

    private static void ValidateProbe(IReadOnlyList<ReferenceProbe> records)
    {
        if (records.Any(record => record.Ref_Key == Guid.Empty))
        {
            throw new OneCTransportException(
                OneCTransportFailureReason.MalformedResponse,
                "The 1С OData service returned an invalid collection envelope.");
        }
    }

    private static OneCTransportException MalformedCurrentObject() =>
        new(
            OneCTransportFailureReason.MalformedResponse,
            "The 1С OData service returned an invalid current reference object.");

    private sealed class ReferenceProbe
    {
        [JsonPropertyName("Ref_Key")]
        public Guid Ref_Key { get; init; }
    }
}
