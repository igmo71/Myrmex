using System.Text.Json.Serialization;

namespace Myrmex.Integrations.OneC.Transport;

internal sealed class OneCODataCollectionResponse<T>
{
    [JsonPropertyName("value")]
    public IReadOnlyList<T>? Value { get; init; }
}
