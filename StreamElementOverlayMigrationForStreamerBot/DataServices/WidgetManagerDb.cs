using Microsoft.Data.Sqlite;
using StreamElementsToStreamerBotMigrationTool.Data;
using System.Collections.ObjectModel;
using System.IO;

namespace StreamElementsToStreamerBotMigrationTool.DataServices;

public static class WidgetManagerDb
{
    private static readonly string _connectionString = $"Data Source={Path.Combine(AppContext.BaseDirectory, "database.db")}";

    static WidgetManagerDb()
        => CreateTableIfNotExists();

    public static void CreateTableIfNotExists()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        CreateTableIfNotExists(connection);
    }

    public static void CreateTableIfNotExists(SqliteConnection connection)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Widget (
                Id             INTEGER PRIMARY KEY,
                Name           TEXT    NOT NULL,
                FolderLocation TEXT    NOT NULL
            );";
        command.ExecuteNonQuery();
    }

    public static List<Widget> GetAll()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return GetAll(connection);
    }

    public static List<Widget> GetAll(SqliteConnection connection)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, Name, FolderLocation
            FROM Widget;";

        var widgets = new List<Widget>();
        using (SqliteDataReader reader = command.ExecuteReader())
        {
            while (reader.Read())
                widgets.Add(ReadWidget(reader));
        }

        foreach (Widget widget in widgets)
            widget.Files = new ObservableCollection<WidgetFile>
            (
                WidgetFileManagerDb.GetByWidgetId(connection, widget.Id)
            );

        return widgets;
    }

    public static Widget? Get(string name)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return Get(connection, name);
    }

    public static Widget? Get(SqliteConnection connection, string name)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, Name, FolderLocation
            FROM Widget
            WHERE Name = $name;";

        command.Parameters.AddWithValue("$name", name);

        Widget? widget = null;
        using (SqliteDataReader reader = command.ExecuteReader())
        {
            if (reader.Read())
                widget = ReadWidget(reader);
        }

        if (widget is not null)
            widget.Files = new ObservableCollection<WidgetFile>
            (
                WidgetFileManagerDb.GetByWidgetId(connection, widget.Id)
            );

        return widget;
    }

    public static void Insert(Widget widget)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        Insert(connection, widget);
    }

    public static void Insert(SqliteConnection connection, Widget widget)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Widget (Name, FolderLocation)
            VALUES ($name, $folderLocation);";

        command.Parameters.AddWithValue("$name",           widget.Name);
        command.Parameters.AddWithValue("$folderLocation", widget.FolderLocation);

        widget.Id = command.ExecuteNonQuery();

        foreach (WidgetFile file in widget.Files)
        {
            file.WidgetId = widget.Id;
            WidgetFileManagerDb.Insert(connection, file);
        }
    }

    public static void Update(Widget widget)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        Update(connection, widget);
    }

    public static void Update(SqliteConnection connection, Widget widget)
    {
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

    public static void Delete(Widget widget)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        Delete(connection, widget);
    }

    public static void Delete(SqliteConnection connection, Widget widget)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
            DELETE FROM Widget
            WHERE Id = $id;";

        command.Parameters.AddWithValue("$id", widget.Id);

        command.ExecuteNonQuery();
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