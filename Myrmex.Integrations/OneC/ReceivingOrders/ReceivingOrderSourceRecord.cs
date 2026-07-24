using System.Text.Json.Serialization;

namespace Myrmex.Integrations.OneC.ReceivingOrders;

internal sealed class ReceivingOrderSourceRecord
{
    [JsonPropertyName("Ref_Key")] public Guid Ref_Key { get; init; }
    [JsonPropertyName("DataVersion")] public byte[] DataVersion { get; init; } = [];
    [JsonPropertyName("DeletionMark")] public bool DeletionMark { get; init; }
    [JsonPropertyName("Number")] public string? Number { get; init; }
    [JsonPropertyName("Date")] public DateTime Date { get; init; }
    [JsonPropertyName("Posted")] public bool Posted { get; init; }
    [JsonPropertyName("Склад_Key")] public Guid Склад_Key { get; init; }
    [JsonPropertyName("Статус")] public string? Статус { get; init; }
    [JsonPropertyName("Товары")] public IReadOnlyList<ReceivingOrderSourceLineRecord> Товары { get; init; } = [];
}
