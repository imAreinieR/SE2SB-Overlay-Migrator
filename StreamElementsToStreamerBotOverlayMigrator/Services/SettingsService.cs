using StreamElementsToStreamerBotOverlayMigrator.Data;
using System.IO;
using System.Text.Json;

namespace StreamElementsToStreamerBotOverlayMigrator.Services;

public static class SettingsService
{
    private static readonly string SettingsFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SE Overlay Migration Tool");
    private static readonly string SettingsFilePath   = Path.Combine(SettingsFolderPath, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
                return new AppSettings();

            var json = File.ReadAllText(SettingsFilePath);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(SettingsFolderPath);

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsFilePath, json);
    }
}