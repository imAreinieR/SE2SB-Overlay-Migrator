namespace StreamElementsToStreamerBotMigrationTool.Data;

public class WidgetDataFieldGroup
{
    public string                Name   { get; set; } = string.Empty;
    public List<WidgetDataField> Fields { get; set; } = new ();
}