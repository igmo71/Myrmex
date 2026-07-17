using System.Text.Json.Serialization;

namespace Myrmex.Integrations.OneC.Transport;

internal sealed class Catalog_УпаковкиЕдиницыИзмерения
{
    [JsonPropertyName("Ref_Key")]
    public Guid Ref_Key { get; init; }

    [JsonPropertyName("DataVersion")]
    public byte[] DataVersion { get; init; } = [];

    [JsonPropertyName("DeletionMark")]
    public bool DeletionMark { get; init; }

    [JsonPropertyName("Code")]
    public string? Code { get; init; }

    [JsonPropertyName("Description")]
    public string? Description { get; init; }

    [JsonPropertyName("НаименованиеПолное")]
    public string? НаименованиеПолное { get; init; }

    [JsonPropertyName("МеждународноеСокращение")]
    public string? МеждународноеСокращение { get; init; }
}
