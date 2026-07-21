using System.Text.Json.Serialization;

namespace Myrmex.Integrations.OneC.StockKeepingUnits;

internal sealed class StockKeepingUnitSourceRecord
{
    [JsonPropertyName("Ref_Key")]
    public Guid Ref_Key { get; init; }

    [JsonPropertyName("DataVersion")]
    public byte[] DataVersion { get; init; } = [];

    [JsonPropertyName("DeletionMark")]
    public bool DeletionMark { get; init; }

    [JsonPropertyName("IsFolder")]
    public bool IsFolder { get; init; }

    [JsonPropertyName("Code")]
    public string? Code { get; init; }

    [JsonPropertyName("Description")]
    public string? Description { get; init; }

    [JsonPropertyName("НаименованиеПолное")]
    public string? НаименованиеПолное { get; init; }

    [JsonPropertyName("Артикул")]
    public string? Артикул { get; init; }

    [JsonPropertyName("ЕдиницаИзмерения_Key")]
    public Guid? ЕдиницаИзмерения_Key { get; init; }
}
