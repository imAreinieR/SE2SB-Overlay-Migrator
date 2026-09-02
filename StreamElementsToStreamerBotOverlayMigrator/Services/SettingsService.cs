using Microsoft.Data.Sqlite;
using StreamElementsToStreamerBotOverlayMigrator.Data;
using System.IO;

namespace StreamElementsToStreamerBotOverlayMigrator.Services;

public static class SettingsService
{
    private static readonly string DatabaseFilePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "database.db"));
    private static readonly string ConnectionString = $"Data Source={DatabaseFilePath}";

    public static AppSettings Current { get; private set; } = new AppSettings();

    public static void Initialize()
    {
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