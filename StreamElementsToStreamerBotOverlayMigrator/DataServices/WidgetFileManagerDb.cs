using Microsoft.Data.Sqlite;
using StreamElementsToStreamerBotOverlayMigrator.Common;
using StreamElementsToStreamerBotOverlayMigrator.Data;

namespace StreamElementsToStreamerBotOverlayMigrator.DataServices;

public static class WidgetFileManagerDb
{
    public static void CreateTableIfNotExists(SqliteConnection connection)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS WidgetFile (
                Id             INTEGER PRIMARY KEY,
                WidgetId       INTEGER NOT NULL,
                FileName       TEXT    NOT NULL,
                Content        TEXT    NOT NULL,
                WidgetFileType INTEGER NOT NULL,
                FOREIGN KEY (WidgetId) REFERENCES Widget(Id)
            );";
        command.ExecuteNonQuery();
    }

    public static List<WidgetFile> GetByWidgetId(SqliteConnection connection, int widgetId)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, WidgetId, FileName, Content, WidgetFileType
            FROM WidgetFile
            WHERE WidgetId = $widgetId;";

        command.Parameters.AddWithValue("$widgetId", widgetId);

        return ReadWidgetFiles(command);
    }

    public static List<WidgetFile> GetAll(SqliteConnection connection)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, WidgetId, FileName, Content, WidgetFileType
            FROM WidgetFile;";

        return ReadWidgetFiles(command);
    }

    public static void Insert(SqliteConnection connection, WidgetFile widgetFile)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO WidgetFile (WidgetId, FileName, Content, WidgetFileType)
            VALUES ($widgetId, $fileName, $content, $widgetFileType);";

        AddWidgetFileParameters(command, widgetFile);
        widgetFile.Id = command.ExecuteNonQuery();
    }

    public static void Update(SqliteConnection connection, WidgetFile widgetFile)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE WidgetFile
            SET FileName       = $fileName,
                Content        = $content,
                WidgetFileType = $widgetFileType
            WHERE Id = $id;";

        command.Parameters.AddWithValue("$id", widgetFile.Id);
        AddWidgetFileParameters(command, widgetFile);
        command.ExecuteNonQuery();
    }

    public static void Delete(SqliteConnection connection, WidgetFile widgetFile)
    {
        if (widgetFile.Id == 0)
            return;

        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
            DELETE FROM WidgetFile
            WHERE Id = $id;";

        command.Parameters.AddWithValue("$id", widgetFile.Id);
        command.ExecuteNonQuery();
    }

    private static List<WidgetFile> ReadWidgetFiles(SqliteCommand command)
    {
        var widgetFiles = new List<WidgetFile>();
        using SqliteDataReader reader = command.ExecuteReader();

        while (reader.Read())
            widgetFiles.Add(ReadWidgetFile(reader));

        return widgetFiles;
    }

    private static void AddWidgetFileParameters(SqliteCommand command, WidgetFile widgetFile)
    {
        command.Parameters.AddWithValue("$widgetId",       widgetFile.WidgetId);
        command.Parameters.AddWithValue("$fileName",       widgetFile.FileName);
        command.Parameters.AddWithValue("$content",        widgetFile.Content);
        command.Parameters.AddWithValue("$widgetFileType", (int) widgetFile.WidgetFileType);
    }

    private static WidgetFile ReadWidgetFile(SqliteDataReader reader)
        => new WidgetFile
        (
            id:             reader.GetInt32(0),
            widgetId:       reader.GetInt32(1),
            fileName:       reader.GetString(2),
            fileContent:    reader.GetString(3),
            widgetFileType: (WidgetFileType) reader.GetInt32(4)
        );
}