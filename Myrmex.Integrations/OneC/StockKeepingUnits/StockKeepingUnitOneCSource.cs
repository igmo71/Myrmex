using Microsoft.Extensions.Options;
using Myrmex.Integrations.OneC.Common.Transport;
using Myrmex.Integrations.OneC.Configuration;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Myrmex.Integrations.OneC.StockKeepingUnits;

internal interface IStockKeepingUnitOneCSource
{
    IAsyncEnumerable<IReadOnlyList<StockKeepingUnitSourceRecord>> ReadPagesAsync(
        CancellationToken cancellationToken);

    Task<StockKeepingUnitSourceRecord?> ReadCurrentAsync(
        Guid externalRefKey,
        CancellationToken cancellationToken);

    Task ProbeAsync(CancellationToken cancellationToken);
}

internal sealed class StockKeepingUnitOneCSource(
    IOneCODataTransport transport,
    IOptions<OneCOptions> options) : IStockKeepingUnitOneCSource
{
    private const int MaximumDataVersionLength = 128;
    private const string Projection =
        "Ref_Key,DataVersion,DeletionMark,IsFolder,Code,Description,НаименованиеПолное,Артикул,ЕдиницаИзмерения_Key";

    public async IAsyncEnumerable<IReadOnlyList<StockKeepingUnitSourceRecord>> ReadPagesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        OneCOptions currentOptions = options.Value;
        int offset = 0;
        while (true)
        {
            List<KeyValuePair<string, string>> parameters =
            [
                new("$format", "json"),
                new("$select", Projection),
                new("$orderby", "Ref_Key"),
                new("$skip", offset.ToString(CultureInfo.InvariantCulture)),
                new("$top", currentOptions.BatchSize.ToString(CultureInfo.InvariantCulture))
            ];
            if (currentOptions.UseFolderFilter)
            {
                parameters.Add(new("$filter", "IsFolder eq false"));
            }

            IReadOnlyList<StockKeepingUnitSourceRecord> page =
                await transport.ReadCollectionAsync<StockKeepingUnitSourceRecord>(
                    currentOptions.NomenclatureEntitySet!,
                    parameters,
                    cancellationToken);
            if (page.Count == 0)
            {
                yield break;
            }

            yield return page;
            offset = checked(offset + page.Count);
            if (page.Count < currentOptions.BatchSize)
            {
                yield break;
            }
        }
    }

    public async Task<StockKeepingUnitSourceRecord?> ReadCurrentAsync(
        Guid externalRefKey,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(externalRefKey, Guid.Empty);
        IReadOnlyList<StockKeepingUnitSourceRecord> records =
            await transport.ReadCollectionAsync<StockKeepingUnitSourceRecord>(
                options.Value.NomenclatureEntitySet!,
                [
                    new("$format", "json"),
                    new("$select", Projection),
                    new("$filter", $"Ref_Key eq guid'{externalRefKey:D}'"),
                    new("$top", "2")
                ],
                cancellationToken);

        if (records.Count != 1)
        {
            throw MalformedCurrentObject();
        }

        if (records[0].Ref_Key != externalRefKey ||
            records[0].DataVersion is null ||
            records[0].DataVersion.Length is < 1 or > MaximumDataVersionLength)
        {
            throw MalformedCurrentObject();
        }

        return records[0];
    }

    public async Task ProbeAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<ReferenceProbe> records = await transport.ReadCollectionAsync<ReferenceProbe>(
            options.Value.NomenclatureEntitySet!,
            [
                new("$format", "json"),
                new("$top", "1"),
                new("$orderby", "Ref_Key"),
                new("$select", "Ref_Key")
            ],
            cancellationToken);
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
