using Microsoft.Data.Sqlite;
using StreamElementsToStreamerBotOverlayMigrator.Data;
using StreamElementsToStreamerBotOverlayMigrator.DataServices;
using StreamElementsToStreamerBotOverlayMigrator.Services;
using System.IO;

namespace StreamElementsToStreamerBotOverlayMigrator.Managers;

public static class WidgetManager
{
    private static readonly string ConnectionString = DatabaseManager.GetDatabaseConnectionString();

    static WidgetManager()
    {
        DatabaseManager.RestoreDatabaseBackupIfNeeded();

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        WidgetManagerDb.CreateTableIfNotExists(connection);
        WidgetFileManagerDb.CreateTableIfNotExists(connection);
    }

    public static List<Widget> GetAll()
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        List<Widget> widgets = WidgetManagerDb.GetAll(connection);

        foreach (Widget widget in widgets)
        {
            widget.AddWidgetFiles(WidgetFileManagerDb.GetByWidgetId(connection, widget.Id));
            widget.AcceptChanges();
        }

        return widgets;
    }

    public static Widget? GetByName(string name)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        Widget? widget = WidgetManagerDb.GetByName(connection, name);

        if (widget != null)
        {
            widget.AddWidgetFiles(WidgetFileManagerDb.GetByWidgetId(connection, widget.Id));
            widget.AcceptChanges();
        }

        return widget;
    }

    public static Widget? GetById(int id)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        Widget? widget = WidgetManagerDb
            .GetAll(connection)
            .FirstOrDefault(existingWidget => existingWidget.Id == id);

        if (widget != null)
        {
            widget.AddWidgetFiles(WidgetFileManagerDb.GetByWidgetId(connection, widget.Id));
            widget.AcceptChanges();
        }

        return widget;
    }

    public static void Save(Widget widget)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        if (widget.Id == 0)
        {
            WidgetManagerDb.Insert(connection, widget);

            foreach (WidgetFile widgetFile in widget.Files)
            {
                widgetFile.WidgetId = widget.Id;
                WidgetFileManagerDb.Insert(connection, widgetFile);
            }
        }
        else
        {
            List<WidgetFile> existingWidgetFiles = WidgetFileManagerDb
                .GetByWidgetId(connection, widget.Id)
                .ToList();

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

            foreach (WidgetFile widgetFile in widget.Files.Where(file => file.Id <= 0))
            {
                widgetFile.WidgetId = widget.Id;
                WidgetFileManagerDb.Insert(connection, widgetFile);
            }

            WidgetManagerDb.Update(connection, widget);
        }
    }

    public static void SaveOrder(IEnumerable<Widget> widgets)
    {
        List<Widget> orderedWidgets = widgets.ToList();

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using SqliteTransaction transaction = connection.BeginTransaction();

        for (int index = 0; index < orderedWidgets.Count; index++)
        {
            Widget widget = orderedWidgets[index];

            if (widget.Id == 0)
                continue;

            widget.SortOrder = index;
            WidgetManagerDb.UpdateSortOrder(connection, transaction, widget.Id, index);
        }

        transaction.Commit();
    }

    public static void Delete(Widget widget)
    {
        if (widget.Id == 0)
            return;

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        foreach (WidgetFile file in widget.Files)
            WidgetFileManagerDb.Delete(connection, file);

        WidgetManagerDb.Delete(connection, widget);

        try
        {
            Directory.Delete(widget.FolderLocation, true);
        }
        catch (Exception)
        {}
    }

    public static Widget Clone(Widget widget)
    {
        var newWidget = new Widget(widget.Name + " (copy)", widget.RootFolderLocation);

        foreach (WidgetFile file in widget.Files)
            newWidget.Files.Add(new WidgetFile(file.FileName, file.Content));

        return newWidget;
    }
    
    public static bool GenerateExportFiles(Widget widget, out string errorMessage)
        => WidgetFileImportAndExportService.GenerateExportFilesForWidget(widget, out errorMessage);

    public static bool ExportRawFiles(Widget widget, string destinationZipPath, out string errorMessage)
        => WidgetFileImportAndExportService.ExportRawFilesAsZip(widget, destinationZipPath, out errorMessage);
}