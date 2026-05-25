using StreamElementsToStreamerBotOverlayMigrator.Common;
using System.Text.Json;

namespace StreamElementsToStreamerBotOverlayMigrator.Data;

public class WidgetFile
{
    public int            Id             { get; set; }
    public int            WidgetId       { get; set; }
    public string         FileName       { get; set; }
    public string         Content        { get; set; }
    public WidgetFileType WidgetFileType { get; set; }

    public WidgetFile(string fileName, string fileContent)
    {
        FileName       = fileName;
        Content        = fileContent;
        WidgetFileType = DetermineWidgetFileType(fileName, fileContent);
    }

    public WidgetFile(int id, int widgetId, string fileName, string fileContent, WidgetFileType widgetFileType)
    {
        Id             = id;
        WidgetId       = widgetId;
        FileName       = fileName;
        Content        = fileContent;
        WidgetFileType = widgetFileType;
    }

    private WidgetFileType DetermineWidgetFileType(string fileName, string fileContent)
    {
        if (fileName.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            return WidgetFileType.Html;
        if (fileName.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
            return WidgetFileType.Javascript;
        if (fileName.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
            return WidgetFileType.Css;

        if (fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase) && fileContent != null)
            return DetermineJsonFileType(fileContent);

        return WidgetFileType.Other;
    }

    private WidgetFileType DetermineJsonFileType(string fileContent)
    {
        try
        {
            using var jsonDocument = JsonDocument.Parse(fileContent);
            JsonElement jsonRootElement = jsonDocument.RootElement;

            if (jsonRootElement.ValueKind != JsonValueKind.Object)
                return WidgetFileType.Other;

            foreach (JsonProperty jsonProperty in jsonRootElement.EnumerateObject())
            {
                if
                (
                    jsonProperty.Value.ValueKind == JsonValueKind.Object
                        && jsonProperty.Value.TryGetProperty("type", out _)
                        && jsonProperty.Value.TryGetProperty("value", out _)
                )
                    return WidgetFileType.FieldJson;
            }

            return WidgetFileType.DataJson;
        }
        catch (JsonException)
        {
            return WidgetFileType.Other;
        }
    }

    public System.Windows.Media.SolidColorBrush WidgetFileTypeColor
        => new System.Windows.Media.SolidColorBrush
        (
            WidgetFileType switch
            {
                WidgetFileType.Html       => System.Windows.Media.Color.FromArgb(255, 255,  99, 132),
                WidgetFileType.Javascript => System.Windows.Media.Color.FromArgb(255,  54, 162, 235),
                WidgetFileType.Css        => System.Windows.Media.Color.FromArgb(255, 255, 206,  86),
                WidgetFileType.FieldJson  => System.Windows.Media.Color.FromArgb(255,  75, 192, 192),
                WidgetFileType.DataJson   => System.Windows.Media.Color.FromArgb(255, 153, 102, 255),
                _                         => System.Windows.Media.Color.FromArgb(255, 201, 203, 207)
            }
        );
}