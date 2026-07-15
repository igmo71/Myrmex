using System.Text.Json.Serialization;

namespace Myrmex.Integrations.OneC.Notifications;

internal sealed class OneCChangeNotificationRequest
{
    [JsonPropertyName("Ref_Key")]
    public string? RefKey { get; set; }

    [JsonPropertyName("DataVersion")]
    public string? DataVersion { get; set; }

    [JsonPropertyName("Number")]
    public string? Number { get; set; }

    [JsonPropertyName("Date")]
    public string? Date { get; set; }
}
