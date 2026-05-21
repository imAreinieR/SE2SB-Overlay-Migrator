using Microsoft.Data.Sqlite;
using StreamElementsToStreamerBotMigrationTool.Data;
using StreamElementsToStreamerBotMigrationTool.DataServices;
using StreamElementsToStreamerBotMigrationTool.Services;
using System.Collections.ObjectModel;
using System.IO;

namespace StreamElementsToStreamerBotMigrationTool.Managers;

public static class WidgetManager
{
    private static readonly string _connectionString = $"Data Source={Path.Combine(AppContext.BaseDirectory, "database.db")}";

    static WidgetManager()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        WidgetManagerDb.CreateTableIfNotExists(connection);
        WidgetFileManagerDb.CreateTableIfNotExists(connection);
    }

    public static List<Widget> GetAll()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        List<Widget> widgets = WidgetManagerDb.GetAll(connection);

        foreach (Widget widget in widgets)
            widget.Files = new ObservableCollection<WidgetFile>
            (
                WidgetFileManagerDb.GetByWidgetId(connection, widget.Id)
            );

        return widgets;
    }

    public static Widget? GetByName(string name)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var widget = WidgetManagerDb.GetByName(connection, name);

        if (widget != null)
            widget.Files = new ObservableCollection<WidgetFile>
            (
                WidgetFileManagerDb.GetByWidgetId(connection, widget.Id)
            );

        return widget;
    }

    public static void Save(Widget widget)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        if (widget.Id == 0)
            WidgetManagerDb.Insert(connection, widget);
        else
            WidgetManagerDb.Update(connection, widget);
    }

    public static void Delete(Widget widget)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        foreach (WidgetFile file in widget.Files)
            WidgetFileManagerDb.DeleteById(connection, file.Id);

        WidgetManagerDb.Delete(connection, widget);
    }
    
    public static bool GenerateExportFiles(Widget widget, out string errorMessage)
        => WidgetFileImportAndExportService.GenerateExportFilesForWidget(widget, out errorMessage);
}