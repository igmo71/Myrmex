using System.Text.Json.Serialization;

namespace Myrmex.Integrations.OneC.ReceivingOrders;

internal sealed class ReceivingOrderSourceLineRecord
{
    [JsonPropertyName("Ref_Key")] public Guid Ref_Key { get; init; }
    [JsonPropertyName("LineNumber")] public int LineNumber { get; init; }
    [JsonPropertyName("Номенклатура_Key")] public Guid Номенклатура_Key { get; init; }
    [JsonPropertyName("Упаковка_Key")] public Guid? Упаковка_Key { get; init; }
    [JsonPropertyName("КоличествоУпаковок")] public decimal? КоличествоУпаковок { get; init; }
    [JsonPropertyName("Количество")] public decimal Количество { get; init; }
}
