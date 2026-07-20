using System.Text.Json.Serialization;

namespace Myrmex.Integrations.OneC.Warehouses;

internal sealed class WarehouseSourceRecord
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
}
