using System.Globalization;
using System.Reflection;
using Microsoft.Data.Sqlite;
using StreamElementsToStreamerBotOverlayMigrator.Data;
using StreamElementsToStreamerBotOverlayMigrator.DataServices;

namespace StreamElementsToStreamerBotOverlayMigrator.Services;

public static class AppSettingService
{
    public static List<Setting> ToSettings(AppSettings appSettings)
    {
        var settings = new List<Setting>();

        foreach (PropertyInfo property in GetSettingProperties())
        {
            object? value = property.GetValue(appSettings);

            settings.Add
            (
                new Setting
                (
                    name:  property.Name,
                    type:  property.PropertyType.Name,
                    value: ConvertToString(value, property.PropertyType)
                )
            );
        }

        return settings;
    }

    public static void PopulateFromSettings(AppSettings appSettings, IEnumerable<Setting> settings)
    {
        Dictionary<string, PropertyInfo> propertiesByName =
            GetSettingProperties().ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        foreach (Setting setting in settings)
        {
            if (!propertiesByName.TryGetValue(setting.Name, out PropertyInfo? property))
                continue;

            object? value = ConvertFromString(setting.Value, property.PropertyType);
            property.SetValue(appSettings, value);
        }
    }

    public static AppSettings FromSettings(IEnumerable<Setting> settings)
    {
        var appSettings = new AppSettings();
        PopulateFromSettings(appSettings, settings);
        return appSettings;
    }

    public static void Save(SqliteConnection connection, AppSettings appSettings)
    {
        SettingDbManager.CreateTableIfNotExists(connection);
        SettingDbManager.UpsertAll(connection, ToSettings(appSettings));
    }

    public static AppSettings Load(SqliteConnection connection)
    {
        SettingDbManager.CreateTableIfNotExists(connection);
        List<Setting> settings = SettingDbManager.GetAll(connection);
        return FromSettings(settings);
    }

    private static IEnumerable<PropertyInfo> GetSettingProperties()
        => typeof(AppSettings)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite);

    private static string ConvertToString(object? value, Type type)
    {
        if (value is null)
            return string.Empty;

        if (type.IsEnum)
            return value.ToString() ?? string.Empty;

        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static object? ConvertFromString(string value, Type type)
    {
        if (type.IsEnum)
            return Enum.Parse(type, value, ignoreCase: true);

        if (type == typeof(bool))
            return bool.Parse(value);

        if (type == typeof(string))
            return value;

        return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
    }
}