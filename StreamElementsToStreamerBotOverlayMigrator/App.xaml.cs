using StreamElementsToStreamerBotOverlayMigrator.Themes;
using System.Windows;

namespace StreamElementsToStreamerBotOverlayMigrator;

public partial class App: Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ThemeManager.DetectFromLoadedDictionary();
    }
}