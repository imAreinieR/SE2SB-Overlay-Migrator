using Microsoft.Data.Sqlite;
using StreamElementsToStreamerBotMigrationTool.Common;
using StreamElementsToStreamerBotMigrationTool.Data;
using System.IO;

namespace StreamElementsToStreamerBotMigrationTool.DataServices;

public static class WidgetFileManagerDb
{
    private static readonly string _connectionString = $"Data Source={Path.Combine(AppContext.BaseDirectory, "database.db")}";

    static WidgetFileManagerDb()
        => CreateTableIfNotExists();

    public static void CreateTableIfNotExists()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

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
    }

    public static List<WidgetFile> GetByWidgetId(int widgetId)
    {
        var widgetFiles = new List<WidgetFile>();

        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = @"
            SELECT Id, WidgetId, FileName, Content, WidgetFileType
            FROM WidgetFile
            WHERE WidgetId = $widgetId;";

            command.Parameters.AddWithValue("$widgetId", widgetId);

            using SqliteDataReader reader = command.ExecuteReader();
            while (reader.Read())
                widgetFiles.Add(ReadWidgetFile(reader));
        }

        return widgetFiles;
    }

    public static List<WidgetFile> GetAll()
    {
        var widgetFiles = new List<WidgetFile>();

        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, FileName, Content, WidgetFileType
                FROM WidgetFile;";

            using SqliteDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                widgetFiles.Add(ReadWidgetFile(reader));
            }
        }

        return widgetFiles;
    }

    public static void Insert(WidgetFile widgetFile)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            Insert(connection, widgetFile);
        }
    }

    private static void Insert(SqliteConnection connection, WidgetFile widgetFile)
    {
        SqliteCommand insertCommand = connection.CreateCommand();
        insertCommand.CommandText = @"
            INSERT INTO WidgetFile (WidgetId, FileName, Content, WidgetFileType)
            VALUES ($widgetId, $fileName, $content, $widgetFileType);";

        insertCommand.Parameters.AddWithValue("$widgetId",       widgetFile.WidgetId);
        insertCommand.Parameters.AddWithValue("$fileName",       widgetFile.FileName);
        insertCommand.Parameters.AddWithValue("$content",        widgetFile.Content);
        insertCommand.Parameters.AddWithValue("$widgetFileType", (int) widgetFile.WidgetFileType);

        insertCommand.ExecuteNonQuery();
    }

    public static void Update(int id, WidgetFile widgetFile)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            SqliteCommand command = connection.CreateCommand();
            command.CommandText =@"
                UPDATE WidgetFile
                SET FileName       = $fileName,
                    Content        = $content,
                    WidgetFileType = $widgetFileType
                WHERE Id = $id;";

            command.Parameters.AddWithValue("$id",             id);
            command.Parameters.AddWithValue("$fileName",       widgetFile.FileName);
            command.Parameters.AddWithValue("$content",        widgetFile.Content);
            command.Parameters.AddWithValue("$widgetFileType", (int) widgetFile.WidgetFileType);

            command.ExecuteNonQuery();
        }
    }

    public static void DeleteById(int id)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM WidgetFile
                WHERE Id = $id;";

            command.Parameters.AddWithValue("$id", id);

            command.ExecuteNonQuery();
        }
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