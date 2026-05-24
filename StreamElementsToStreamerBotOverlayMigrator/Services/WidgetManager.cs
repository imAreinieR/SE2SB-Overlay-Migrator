using Microsoft.Data.Sqlite;
using StreamElementsToStreamerBotOverlayMigrator.Data;
using StreamElementsToStreamerBotOverlayMigrator.DataServices;
using StreamElementsToStreamerBotOverlayMigrator.Services;
using System.Collections.ObjectModel;
using System.IO;

namespace StreamElementsToStreamerBotOverlayMigrator.Managers;

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
        {
            WidgetManagerDb.Insert(connection, widget);
        }
        else
        {
            List<WidgetFile> existingWidgetFiles = WidgetFileManagerDb
                .GetByWidgetId(connection, widget.Id)
                .ToList();

            foreach (WidgetFile widgetFile in widget.Files.Where(file => file.Id <= 0))
            {
                widgetFile.WidgetId = widget.Id;
                WidgetFileManagerDb.Insert(connection, widgetFile);
            }

            List<int> currentWidgetFileIds = widget
                .Files
                .Where(file => file.Id > 0)
                .Select(file => file.Id)
                .ToList();

            foreach (WidgetFile widgetFile in existingWidgetFiles.Where(file => !currentWidgetFileIds.Contains(file.Id)))
                WidgetFileManagerDb.Delete(connection, widgetFile);

            List<int> existingWidgetFileIds = existingWidgetFiles
                .Select(file => file.Id)
                .ToList(); 

            foreach (WidgetFile widgetFile in widget.Files.Where(file => existingWidgetFileIds.Contains(file.Id)))
                WidgetFileManagerDb.Update(connection, widgetFile);

            WidgetManagerDb.Update(connection, widget);
        }
    }

    public static void Delete(Widget widget)
    {
        if (widget.Id == 0)
            return;

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        foreach (WidgetFile file in widget.Files)
            WidgetFileManagerDb.Delete(connection, file);

        WidgetManagerDb.Delete(connection, widget);
    }
    
    public static bool GenerateExportFiles(Widget widget, out string errorMessage)
        => WidgetFileImportAndExportService.GenerateExportFilesForWidget(widget, out errorMessage);
}