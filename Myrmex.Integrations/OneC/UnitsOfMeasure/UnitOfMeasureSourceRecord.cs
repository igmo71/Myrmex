using System.Text.Json.Serialization;

namespace Myrmex.Integrations.OneC.UnitsOfMeasure;

internal sealed class UnitOfMeasureSourceRecord
{
    [JsonPropertyName("Ref_Key")]
    public Guid Ref_Key { get; init; }

    [JsonPropertyName("DataVersion")]
    public byte[] DataVersion { get; init; } = [];

    [JsonPropertyName("DeletionMark")]
    public bool DeletionMark { get; init; }

    [JsonPropertyName("Description")]
    public string? Description { get; init; }

    [JsonPropertyName("НаименованиеПолное")]
    public string? НаименованиеПолное { get; init; }

    [JsonPropertyName("МеждународноеСокращение")]
    public string? МеждународноеСокращение { get; init; }

    [JsonPropertyName("ТипИзмеряемойВеличины")]
    public string? ТипИзмеряемойВеличины { get; init; }

    [JsonPropertyName("Числитель")]
    public decimal? Числитель { get; init; }

    [JsonPropertyName("Знаменатель")]
    public decimal? Знаменатель { get; init; }
}
