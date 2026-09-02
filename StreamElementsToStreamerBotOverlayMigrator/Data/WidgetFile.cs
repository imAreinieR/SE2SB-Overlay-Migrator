using StreamElementsToStreamerBotOverlayMigrator.Common;
using StreamElementsToStreamerBotOverlayMigrator.Themes;
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
        if (SupportedFileTypes.ImageExtensions.Any(extension => fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))
            return WidgetFileType.ImageAsset;
        if (SupportedFileTypes.AudioExtensions.Any(extension => fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))
            return WidgetFileType.AudioAsset;
        if (SupportedFileTypes.VideoExtensions.Any(extension => fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))
            return WidgetFileType.VideoAsset;

        return WidgetFileType.Other;
    }

    private WidgetFileType DetermineJsonFileType(string fileContent)
    {
        try
        {
            using JsonDocument jsonDocument = JsonDocument.Parse
            (
                fileContent,
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = true
                }
            );
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
        => WidgetFileType switch
        {
            WidgetFileType.Html       => AppColors.FileTypeHtml,
            WidgetFileType.Javascript => AppColors.FileTypeJavascript,
            WidgetFileType.Css        => AppColors.FileTypeCss,
            WidgetFileType.FieldJson  => AppColors.FileTypeFieldJson,
            WidgetFileType.DataJson   => AppColors.FileTypeDataJson,
            WidgetFileType.ImageAsset => AppColors.FileTypeImageAsset,
            WidgetFileType.AudioAsset => AppColors.FileTypeAudioAsset,
            WidgetFileType.VideoAsset => AppColors.FileTypeVideoAsset,
            _                         => AppColors.FileTypeOther
        };
}