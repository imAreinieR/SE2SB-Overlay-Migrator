using System.Windows;

namespace ApplicationUpdater;

public partial class App: Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        string? url    = GetArg(e.Args, "--url");
        string? target = GetArg(e.Args, "--target");

        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(target))
        {
            MessageBox.Show
            (
                "Missing arguments. This updater should be launched by the main app.",
                "SE2SB Updater",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            Shutdown();
            return;
        }

        var window = new UpdaterWindow(url, target);
        window.Show();
    }

    static string? GetArg(string[] args, string key)
    {
        int idx = Array.IndexOf(args, key);
        return idx >= 0 && idx + 1 < args.Length
            ? args[idx + 1]
            : null;
    }
}