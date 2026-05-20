using StreamElementsToStreamerBotMigrationTool.Common;

namespace StreamElementsToStreamerBotMigrationTool.Data;

public class WidgetFile
{
    public string FileName { get; set; }
    public string Content { get; set; }
    public WidgetFileType WidgetFileType { get; set; }

    public WidgetFile(string fileName, string fileContent)
    {
        FileName = fileName;
        Content = fileContent;
        WidgetFileType = DetermineWidgetFileType(fileName);
    }

    private WidgetFileType DetermineWidgetFileType(string fileName)
    {
        if (fileName.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            return WidgetFileType.Html;
        if (fileName.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
            return WidgetFileType.Javascript;
        if (fileName.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
            return WidgetFileType.Css;
        if (fileName.Equals("fields.json", StringComparison.OrdinalIgnoreCase))
            return WidgetFileType.FieldJson;
        if (fileName.Equals("data.json", StringComparison.OrdinalIgnoreCase))
            return WidgetFileType.DataJson;
        return WidgetFileType.Other;
    }
}