using StreamElementsToStreamerBotMigrationTool.Data;

namespace StreamElementsToStreamerBotMigrationTool.Common.ExtensionMethods;

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
}