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
                FolderLocation TEXT    NOT NULL,
                SortOrder      INTEGER NOT NULL DEFAULT 0
            );";
        command.ExecuteNonQuery();

        AddSortOrderColumnIfMissing(connection);
    }

    private static void AddSortOrderColumnIfMissing(SqliteConnection connection)
    {
        SqliteCommand pragmaCommand = connection.CreateCommand();
        pragmaCommand.CommandText = "PRAGMA table_info(Widget);";

        bool hasSortOrderColumn = false;
        using (SqliteDataReader reader = pragmaCommand.ExecuteReader())
        {
            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), "SortOrder", StringComparison.OrdinalIgnoreCase))
                {
                    hasSortOrderColumn = true;
                    break;
                }
            }
        }

        if (hasSortOrderColumn)
            return;

        SqliteCommand alterCommand = connection.CreateCommand();
        alterCommand.CommandText = "ALTER TABLE Widget ADD COLUMN SortOrder INTEGER NOT NULL DEFAULT 0;";
        alterCommand.ExecuteNonQuery();

        SqliteCommand backfillCommand = connection.CreateCommand();
        backfillCommand.CommandText = "UPDATE Widget SET SortOrder = Id;";
        backfillCommand.ExecuteNonQuery();
    }

    public static List<Widget> GetAll(SqliteConnection connection)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, Name, FolderLocation, SortOrder
            FROM Widget
            ORDER BY SortOrder ASC, Id ASC;";

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
            SELECT Id, Name, FolderLocation, SortOrder
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
            INSERT INTO Widget (Name, FolderLocation, SortOrder)
            VALUES ($name, $folderLocation, (SELECT COALESCE(MAX(SortOrder), -1) + 1 FROM Widget));
            SELECT last_insert_rowid();";

        command.Parameters.AddWithValue("$name",           widget.Name);
        command.Parameters.AddWithValue("$folderLocation", widget.RootFolderLocation);

        widget.Id = Convert.ToInt32(command.ExecuteScalar());
        widget.SortOrder = GetSortOrder(connection, widget.Id);
        widget.AcceptChanges();
    }

    private static int GetSortOrder(SqliteConnection connection, int id)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT SortOrder FROM Widget WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public static void Update(SqliteConnection connection, Widget widget)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE Widget
            SET Name           = $name,
                FolderLocation = $folderLocation,
                SortOrder      = $sortOrder
            WHERE Id = $id;";

        command.Parameters.AddWithValue("$id",             widget.Id);
        command.Parameters.AddWithValue("$name",           widget.Name);
        command.Parameters.AddWithValue("$folderLocation", widget.RootFolderLocation);
        command.Parameters.AddWithValue("$sortOrder",      widget.SortOrder);

        command.ExecuteNonQuery();
        widget.AcceptChanges();
    }

    public static void UpdateSortOrder(SqliteConnection connection, SqliteTransaction? transaction, int id, int sortOrder)
    {
        SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
            UPDATE Widget
            SET SortOrder = $sortOrder
            WHERE Id = $id;";

        command.Parameters.AddWithValue("$id",        id);
        command.Parameters.AddWithValue("$sortOrder", sortOrder);

        command.ExecuteNonQuery();
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
            files:          new List<WidgetFile>(),
            sortOrder:      reader.GetInt32(3)
        );
}