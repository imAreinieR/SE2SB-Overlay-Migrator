using StreamElementsToStreamerBotOverlayMigrator.Themes;

namespace StreamElementsToStreamerBotOverlayMigrator.Data;

public class AppSettings
{
    #region General Settings

    public Theme  Theme                     { get; set; } = Theme.Dark;

    #endregion General Settings

    #region StreamerBot Settings

    public string Host                      { get; set; } = "127.0.0.1";
    public int    Port                      { get; set; } = 8080;
    public string Endpoint                  { get; set; } = "/";
    public bool   EnableAuthentication      { get; set; } = false;
    public string Password                  { get; set; } = string.Empty;

    public bool   DefaultEnableTwitchEvents { get; set; } = true;

    #endregion StreamerBot Settings
}