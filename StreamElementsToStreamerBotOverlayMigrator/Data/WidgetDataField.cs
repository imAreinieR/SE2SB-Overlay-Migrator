using System.Text.Json.Serialization;

namespace StreamElementsToStreamerBotOverlayMigrator.Data;

public class WidgetDataField
{
    [JsonPropertyName("type")]
    public string                      Type    { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string                      Label   { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public object?                     Value   { get; set; }

    [JsonPropertyName("group")]
    public string?                     Group   { get; set; }

    [JsonPropertyName("options")]
    public Dictionary<string, string>? Options { get; set; }

    public string                      Key     { get; set; } = string.Empty;
}