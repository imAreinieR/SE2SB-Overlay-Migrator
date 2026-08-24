using Microsoft.Data.Sqlite;
using StreamElementsToStreamerBotOverlayMigrator.Data;

namespace StreamElementsToStreamerBotOverlayMigrator.DataServices;

public static class WidgetManagerDb
{
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

        return widgets;
    }

    public static Widget? GetByName(SqliteConnection connection, string name)
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

        return widget;
    }

    public static void Insert(SqliteConnection connection, Widget widget)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Widget (Name, FolderLocation)
            VALUES ($name, $folderLocation);
            SELECT last_insert_rowid();";

        command.Parameters.AddWithValue("$name",           widget.Name);
        command.Parameters.AddWithValue("$folderLocation", widget.RootFolderLocation);

        widget.Id = Convert.ToInt32(command.ExecuteScalar());
        widget.AcceptChanges();
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
        command.Parameters.AddWithValue("$folderLocation", widget.RootFolderLocation);

        command.ExecuteNonQuery();
        widget.AcceptChanges();
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