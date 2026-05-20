using StreamElementsToStreamerBotMigrationTool.Common;

namespace StreamElementsToStreamerBotMigrationTool.Data;

internal class WidgetFile
{
    public WidgetFileType WidgetFileType { get; set; }
    public string         Content        { get; set; }

    public WidgetFile(WidgetFileType widgetFileType, string content = "")
    {
        WidgetFileType = widgetFileType;
        Content        = content;
    }
}