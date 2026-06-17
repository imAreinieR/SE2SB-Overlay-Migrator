using Microsoft.Data.Sqlite;
using StreamElementsToStreamerBotOverlayMigrator.Data;
using StreamElementsToStreamerBotOverlayMigrator.DataServices;
using StreamElementsToStreamerBotOverlayMigrator.Services;
using System.Diagnostics;
using System.IO;

namespace StreamElementsToStreamerBotOverlayMigrator.Managers;

public static class WidgetManager
{
    private const           int    MaxDatabaseBackupCount = 5;
    private const           string DatabaseFileExtension  = ".db";

    private static readonly string DefaultRootFolderPath  = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "imA-SB-Widgets");
    private static readonly string DatabaseFilePath       = Path.Combine(AppContext.BaseDirectory, "database.db");
    private static readonly string ConnectionString       = $"Data Source={DatabaseFilePath}";

    static WidgetManager()
    {
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
    
    public static bool GenerateExportFiles(Widget widget, out string errorMessage)
        => WidgetFileImportAndExportService.GenerateExportFilesForWidget(widget, out errorMessage);

    public static void RestoreDatabaseBackupIfNeeded()
    {
        if (File.Exists(DatabaseFilePath))
            return;

        string backupFolder = Path.Combine(DefaultRootFolderPath, "Backups");

        if (!Directory.Exists(backupFolder))
            return;

        string? latestBackup = Directory
            .EnumerateFiles(backupFolder, $"*{DatabaseFileExtension}")
            .OrderByDescending(file => file)
            .FirstOrDefault();

        if (latestBackup == null)
            return;

        try
        {
            File.Copy(latestBackup, DatabaseFilePath);
        }
        catch (Exception)
        {}
    }

    public static void CreateDatabaseBackupIfNeeded()
    {
        if (!File.Exists(DatabaseFilePath))
            return;

        try
        {
            string backupFolder = Path.Combine(DefaultRootFolderPath, "Backups");
            Directory.CreateDirectory(backupFolder);

            string todayStamp = DateTime.Now.ToString("yyyy-MM-dd");

            bool backupExistsForToday = Directory
                .EnumerateFiles(backupFolder, $"*{DatabaseFileExtension}")
                .Any(file => Path.GetFileName(file).StartsWith(todayStamp));

            if (backupExistsForToday)
                return;

            string backupFileName = $"{todayStamp}{DatabaseFileExtension}";
            string backupPath = Path.Combine(backupFolder, backupFileName);
            File.Copy(DatabaseFilePath, backupPath, true);

            List<string> allBackups = Directory
                .EnumerateFiles(backupFolder, $"*{DatabaseFileExtension}")
                .OrderBy(file => file)
                .ToList();

            while (allBackups.Count > MaxDatabaseBackupCount)
            {
                File.Delete(allBackups[0]);
                allBackups.RemoveAt(0);
            }
        }
        catch (Exception)
        {}
    }
}