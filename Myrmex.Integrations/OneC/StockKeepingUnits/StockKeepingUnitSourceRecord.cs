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

    [JsonPropertyName("ВесИспользовать")]
    public bool ВесИспользовать { get; init; }

    [JsonPropertyName("ВесЧислитель")]
    public decimal? ВесЧислитель { get; init; }

    [JsonPropertyName("ВесЗнаменатель")]
    public decimal? ВесЗнаменатель { get; init; }

    [JsonPropertyName("ВесЕдиницаИзмерения_Key")]
    public Guid? ВесЕдиницаИзмерения_Key { get; init; }

    [JsonPropertyName("ДлинаИспользовать")]
    public bool ДлинаИспользовать { get; init; }

    [JsonPropertyName("ДлинаЧислитель")]
    public decimal? ДлинаЧислитель { get; init; }

    [JsonPropertyName("ДлинаЗнаменатель")]
    public decimal? ДлинаЗнаменатель { get; init; }

    [JsonPropertyName("ДлинаЕдиницаИзмерения_Key")]
    public Guid? ДлинаЕдиницаИзмерения_Key { get; init; }

    [JsonPropertyName("ПлощадьИспользовать")]
    public bool ПлощадьИспользовать { get; init; }

    [JsonPropertyName("ПлощадьЧислитель")]
    public decimal? ПлощадьЧислитель { get; init; }

    [JsonPropertyName("ПлощадьЗнаменатель")]
    public decimal? ПлощадьЗнаменатель { get; init; }

    [JsonPropertyName("ПлощадьЕдиницаИзмерения_Key")]
    public Guid? ПлощадьЕдиницаИзмерения_Key { get; init; }

    [JsonPropertyName("ОбъемИспользовать")]
    public bool ОбъемИспользовать { get; init; }

    [JsonPropertyName("ОбъемЧислитель")]
    public decimal? ОбъемЧислитель { get; init; }

    [JsonPropertyName("ОбъемЗнаменатель")]
    public decimal? ОбъемЗнаменатель { get; init; }

    [JsonPropertyName("ОбъемЕдиницаИзмерения_Key")]
    public Guid? ОбъемЕдиницаИзмерения_Key { get; init; }
}
