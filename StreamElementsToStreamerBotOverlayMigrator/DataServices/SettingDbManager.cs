using Microsoft.Data.Sqlite;
using StreamElementsToStreamerBotOverlayMigrator.Data;

namespace StreamElementsToStreamerBotOverlayMigrator.DataServices;

public static class SettingDbManager
{
    public static void CreateTableIfNotExists(SqliteConnection connection)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Setting (
                Id    INTEGER PRIMARY KEY,
                Name  TEXT    NOT NULL UNIQUE,
                Type  TEXT    NOT NULL,
                Value TEXT    NOT NULL
            );";
        command.ExecuteNonQuery();
    }

    public static List<Setting> GetAll(SqliteConnection connection)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, Name, Type, Value
            FROM Setting
            ORDER BY Id ASC;";

        var settings = new List<Setting>();
        using (SqliteDataReader reader = command.ExecuteReader())
        {
            while (reader.Read())
                settings.Add(ReadSetting(reader));
        }

        return settings;
    }

    public static Setting? GetByName(SqliteConnection connection, string name)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, Name, Type, Value
            FROM Setting
            WHERE Name = $name;";

        command.Parameters.AddWithValue("$name", name);

        Setting? setting = null;
        using (SqliteDataReader reader = command.ExecuteReader())
        {
            if (reader.Read())
                setting = ReadSetting(reader);
        }

        return setting;
    }

    public static void Insert(SqliteConnection connection, Setting setting)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Setting (Name, Type, Value)
            VALUES ($name, $type, $value);
            SELECT last_insert_rowid();";

        command.Parameters.AddWithValue("$name",  setting.Name);
        command.Parameters.AddWithValue("$type",  setting.Type);
        command.Parameters.AddWithValue("$value", setting.Value);

        setting.Id = Convert.ToInt32(command.ExecuteScalar());
    }

    public static void Update(SqliteConnection connection, Setting setting)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE Setting
            SET Name  = $name,
                Type  = $type,
                Value = $value
            WHERE Id = $id;";

        command.Parameters.AddWithValue("$id",    setting.Id);
        command.Parameters.AddWithValue("$name",  setting.Name);
        command.Parameters.AddWithValue("$type",  setting.Type);
        command.Parameters.AddWithValue("$value", setting.Value);

        command.ExecuteNonQuery();
    }

    public static void Upsert(SqliteConnection connection, Setting setting)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Setting (Name, Type, Value)
            VALUES ($name, $type, $value)
            ON CONFLICT(Name) DO UPDATE SET
                Type  = excluded.Type,
                Value = excluded.Value;
            SELECT Id FROM Setting WHERE Name = $name;";

        command.Parameters.AddWithValue("$name",  setting.Name);
        command.Parameters.AddWithValue("$type",  setting.Type);
        command.Parameters.AddWithValue("$value", setting.Value);

        setting.Id = Convert.ToInt32(command.ExecuteScalar());
    }

    public static void UpsertAll(SqliteConnection connection, IEnumerable<Setting> settings)
    {
        using SqliteTransaction transaction = connection.BeginTransaction();

        foreach (Setting setting in settings)
        {
            SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                INSERT INTO Setting (Name, Type, Value)
                VALUES ($name, $type, $value)
                ON CONFLICT(Name) DO UPDATE SET
                    Type  = excluded.Type,
                    Value = excluded.Value;";

            command.Parameters.AddWithValue("$name",  setting.Name);
            command.Parameters.AddWithValue("$type",  setting.Type);
            command.Parameters.AddWithValue("$value", setting.Value);

            command.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public static void Delete(SqliteConnection connection, Setting setting)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
            DELETE FROM Setting
            WHERE Id = $id;";

        command.Parameters.AddWithValue("$id", setting.Id);
        command.ExecuteNonQuery();
    }

    private static Setting ReadSetting(SqliteDataReader reader)
        => new Setting
        (
            id:    reader.GetInt32(0),
            name:  reader.GetString(1),
            type:  reader.GetString(2),
            value: reader.GetString(3)
        );
}