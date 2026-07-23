using Microsoft.Extensions.Options;
using Myrmex.Integrations.OneC.Common.Transport;
using Myrmex.Integrations.OneC.Configuration;
using System.Text.Json.Serialization;

namespace Myrmex.Integrations.OneC.UnitsOfMeasure;

internal interface IUnitOfMeasureOneCSource
{
    Task<IReadOnlyList<UnitOfMeasureSourceRecord>> ReadAllAsync(
        CancellationToken cancellationToken);

    Task<UnitOfMeasureSourceRecord?> ReadCurrentAsync(
        Guid externalRefKey,
        CancellationToken cancellationToken);

    Task ProbeAsync(CancellationToken cancellationToken);
}

internal sealed class UnitOfMeasureOneCSource(
    IOneCODataTransport transport,
    IOptions<OneCOptions> options) : IUnitOfMeasureOneCSource
{
    private const int MaximumDataVersionLength = 128;
    private const string Projection =
        "Ref_Key,DataVersion,DeletionMark,Description,НаименованиеПолное,МеждународноеСокращение," +
        "ТипИзмеряемойВеличины,Числитель,Знаменатель";

    public Task<IReadOnlyList<UnitOfMeasureSourceRecord>> ReadAllAsync(
        CancellationToken cancellationToken) =>
        transport.ReadCollectionAsync<UnitOfMeasureSourceRecord>(
            options.Value.UnitsOfMeasureEntitySet,
            [
                new("$format", "json"),
                new("$select", Projection),
                new("$orderby", "Ref_Key")
            ],
            cancellationToken);

    public async Task<UnitOfMeasureSourceRecord?> ReadCurrentAsync(
        Guid externalRefKey,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(externalRefKey, Guid.Empty);
        IReadOnlyList<UnitOfMeasureSourceRecord> records =
            await transport.ReadCollectionAsync<UnitOfMeasureSourceRecord>(
                options.Value.UnitsOfMeasureEntitySet,
                [
                    new("$format", "json"),
                    new("$select", Projection),
                    new("$filter", $"Ref_Key eq guid'{externalRefKey:D}'"),
                    new("$top", "2")
                ],
                cancellationToken);

        if (records.Count == 0)
        {
            return null;
        }

        if (records.Count != 1 ||
            records[0].Ref_Key != externalRefKey ||
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
            options.Value.UnitsOfMeasureEntitySet,
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
