using Microsoft.Data.Sqlite;
using StreamElementsToStreamerBotMigrationTool.Data;
using System.IO;

namespace StreamElementsToStreamerBotMigrationTool.DataServices;

public static class WidgetManagerDb
{
    private static readonly string _connectionString = $"Data Source={Path.Combine(AppContext.BaseDirectory, "database.db")}";

    static WidgetManagerDb()
        => CreateTableIfNotExists();

    public static void CreateTableIfNotExists()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Widget (
                    Id             INTEGER PRIMARY KEY,
                    Name           TEXT    NOT NULL,
                    FolderLocation TEXT    NOT NULL
                );";
            command.ExecuteNonQuery();
        }
    }

    public static List<Widget> GetAll()
    {
        var widgets = new List<Widget>();

        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Name, FolderLocation
                FROM Widget;";

            using SqliteDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                Widget widget = ReadWidget(reader);
                widget.Files = WidgetFileManagerDb.GetByWidgetId(widget.Id);
                widgets.Add(widget);
            }
        }

        return widgets;
    }

    public static Widget? Get(string name)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Name, FolderLocation
                FROM Widget
                WHERE Name = $name;";

            command.Parameters.AddWithValue("$name", name);

            using SqliteDataReader reader = command.ExecuteReader();

            if (reader.Read())
            {
                Widget widget = ReadWidget(reader);
                widget.Files = WidgetFileManagerDb.GetByWidgetId(widget.Id);
                return widget;
            }

            return null;
        }
    }

    public static void Insert(Widget widget)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            Insert(connection, widget);
        }
    }

    private static void Insert(SqliteConnection connection, Widget widget)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Widget (Name, FolderLocation)
            VALUES ($name, $folderLocation);";

        command.Parameters.AddWithValue("$name",           widget.Name);
        command.Parameters.AddWithValue("$folderLocation", widget.FolderLocation);

        command.ExecuteNonQuery();

        widget.Id = (int)(long) command.ExecuteScalar()!;

        foreach (WidgetFile file in widget.Files)
        {
            file.WidgetId = widget.Id;
            WidgetFileManagerDb.Insert(file);
        }
    }

    public static void Update(Widget widget)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Widget
                SET Name           = $name,
                    FolderLocation = $folderLocation
                WHERE Id = $id;";

            command.Parameters.AddWithValue("$id",             widget.Id);
            command.Parameters.AddWithValue("$name",           widget.Name);
            command.Parameters.AddWithValue("$folderLocation", widget.FolderLocation);

            command.ExecuteNonQuery();
        }
    }

    public static void Delete(Widget widget)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM Widget
                WHERE Id = $id;";

            command.Parameters.AddWithValue("$id", widget.Id);

            command.ExecuteNonQuery();
        }
    }

    private static Widget ReadWidget(SqliteDataReader reader)
        => new Widget
        (
            id:             reader.GetInt32(0),
            name:           reader.GetString(1),
            folderLocation: reader.GetString(2),
            files:          new List<WidgetFile>()
        );
}