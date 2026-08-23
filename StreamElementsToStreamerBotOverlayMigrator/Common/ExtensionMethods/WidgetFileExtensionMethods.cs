using StreamElementsToStreamerBotOverlayMigrator.Data;

namespace StreamElementsToStreamerBotOverlayMigrator.Common.ExtensionMethods;

public static partial class ExtensionMethods
{
    public static string GetFileNameForWidgetFileType(this WidgetFile widgetFile)
        => widgetFile.WidgetFileType switch
        {
            WidgetFileType.Html
                => "index.html",
            WidgetFileType.Javascript
                => "index.js",
            WidgetFileType.Css
                => "index.css",
            WidgetFileType.DataJson
                => "config.js",
            _
                => widgetFile.FileName
        };

    public static bool IsTextBasedFile(this WidgetFileType widgetFileType)
        => widgetFileType     == WidgetFileType.Html
            || widgetFileType == WidgetFileType.Css
            || widgetFileType == WidgetFileType.Javascript
            || widgetFileType == WidgetFileType.FieldJson
            || widgetFileType == WidgetFileType.DataJson;

    public static bool IsAssetFile(this WidgetFileType widgetFileType)
        => widgetFileType     == WidgetFileType.ImageAsset
            || widgetFileType == WidgetFileType.AudioAsset
            || widgetFileType == WidgetFileType.VideoAsset;
}