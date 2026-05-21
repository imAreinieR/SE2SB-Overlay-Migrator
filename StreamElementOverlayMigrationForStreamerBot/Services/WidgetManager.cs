using StreamElementsToStreamerBotMigrationTool.Data;
using StreamElementsToStreamerBotMigrationTool.DataServices;

namespace StreamElementsToStreamerBotMigrationTool.Managers;

public static class WidgetManager
{
    public static List<Widget> GetAll()
    {
        return WidgetManagerDb.GetAll();
    }

    public static Widget? GetByName(string name)
    {
        return WidgetManagerDb.Get(name);
    }

    public static void Save(Widget widget)
    {
        if (widget.Id == 0)
            WidgetManagerDb.Insert(widget);
        else
            WidgetManagerDb.Update(widget);
    }

    public static void Delete(Widget widget)
    {
        WidgetManagerDb.Delete(widget);
    }
}