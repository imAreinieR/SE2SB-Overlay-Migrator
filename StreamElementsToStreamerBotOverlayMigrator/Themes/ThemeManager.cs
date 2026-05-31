using System.Windows;

namespace StreamElementsToStreamerBotOverlayMigrator.Themes;

/// <summary>
/// Switches between Dark (Colors.Dark.xaml) and Light (Colors.Light.xaml) at runtime
/// by finding and replacing the color dictionary in Application.Resources.MergedDictionaries.
///
/// Usage:
///   ThemeManager.Apply(Theme.Light);
///   ThemeManager.Apply(Theme.Dark);
///   ThemeManager.Toggle();
///   ThemeManager.DetectFromLoadedDictionary(); // call on startup in App.xaml.cs
/// </summary>
public static class ThemeManager
{
    private const string DarkSource  = "Themes/Colors.Dark.xaml";
    private const string LightSource = "Themes/Colors.Light.xaml";

    public static Theme Current { get; private set; } = Theme.Dark;

    public static void Apply(Theme theme)
    {
        string source = theme == Theme.Light ? LightSource : DarkSource;

        ResourceDictionary resources = Application.Current.Resources;

        ResourceDictionary? existing = resources.MergedDictionaries
            .FirstOrDefault(d => d.Source?.OriginalString.Contains("Colors.") == true);

        if (existing is not null)
            resources.MergedDictionaries.Remove(existing);

        resources.MergedDictionaries.Insert(0, new ResourceDictionary
        {
            Source = new Uri(source, UriKind.Relative)
        });

        Current = theme;
    }

    public static void Toggle()
        => Apply(Current == Theme.Dark ? Theme.Light : Theme.Dark);

    /// <summary>
    /// Reads whichever color dictionary is already loaded in App.xaml and sets
    /// <see cref="Current"/> to match. Call this from App.xaml.cs OnStartup so
    /// AppColors stays in sync without needing to call Apply() on launch.
    /// </summary>
    public static void DetectFromLoadedDictionary()
    {
        Current = Application.Current.Resources.MergedDictionaries
            .Any(d => d.Source?.OriginalString.Contains("Light") == true)
            ? Theme.Light
            : Theme.Dark;
    }
}

public enum Theme { Dark, Light }
