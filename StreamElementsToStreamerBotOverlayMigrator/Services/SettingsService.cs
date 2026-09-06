using Microsoft.Data.Sqlite;
using StreamElementsToStreamerBotOverlayMigrator.Data;
using System.IO;

namespace StreamElementsToStreamerBotOverlayMigrator.Services;

public static class SettingsService
{
    private static readonly string ConnectionString = DatabaseManager.GetDatabaseConnectionString();

    public static AppSettings Current { get; private set; } = new AppSettings();

    public static void Initialize()
    {
        DatabaseManager.RestoreDatabaseBackupIfNeeded();

        using SqliteConnection connection = new SqliteConnection(ConnectionString);
        connection.Open();

        Current = AppSettingService.Load(connection);
    }

    public static void Save()
    {
        using SqliteConnection connection = new SqliteConnection(ConnectionString);
        connection.Open();

        AppSettingService.Save(connection, Current);
    }

    public static void Save(AppSettings settings)
    {
        Current = settings;
        Save();
    }
}