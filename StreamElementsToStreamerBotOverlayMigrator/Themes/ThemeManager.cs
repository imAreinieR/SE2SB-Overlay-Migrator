using System.Windows;

namespace StreamElementsToStreamerBotOverlayMigrator.Themes;

public static class ThemeManager
{
    private const string DarkSource            = "Themes/Colors.Dark.xaml";
    private const string LightSource           = "Themes/Colors.Light.xaml";
    private const string DarkColorBlindSource  = "Themes/Colors.Dark.ColorBlind.xaml";
    private const string LightColorBlindSource = "Themes/Colors.Light.ColorBlind.xaml";

    public static Theme Current { get; private set; } = Theme.Dark;

    public static void Apply(Theme theme)
    {
        string source = theme switch
        {
            Theme.Light           => LightSource,
            Theme.DarkColorBlind  => DarkColorBlindSource,
            Theme.LightColorBlind => LightColorBlindSource,
            _                     => DarkSource,
        };

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
        => Apply(Current switch
        {
            Theme.Dark            => Theme.Light,
            Theme.Light           => Theme.Dark,
            Theme.DarkColorBlind  => Theme.LightColorBlind,
            Theme.LightColorBlind => Theme.DarkColorBlind,
            _                     => Theme.Dark,
        });

    public static void ToggleColorBlindMode()
        => Apply(Current switch
        {
            Theme.Dark            => Theme.DarkColorBlind,
            Theme.DarkColorBlind  => Theme.Dark,
            Theme.Light           => Theme.LightColorBlind,
            Theme.LightColorBlind => Theme.Light,
            _                     => Current,
        });

    public static bool IsDark(Theme theme)       => theme is Theme.Dark           or Theme.DarkColorBlind;
    public static bool IsColorBlind(Theme theme) => theme is Theme.DarkColorBlind or Theme.LightColorBlind;

    /// <summary>
    /// Reads whichever color dictionary is already loaded in App.xaml and sets
    /// <see cref="Current"/> to match. Call this from App.xaml.cs OnStartup so
    /// AppColors stays in sync without needing to call Apply() on launch.
    /// </summary>
    public static void DetectFromLoadedDictionary()
    {
        string? source = Application.Current.Resources.MergedDictionaries
            .Select(d => d.Source?.OriginalString)
            .FirstOrDefault(s => s?.Contains("Colors.") == true);

        bool isLight      = source?.Contains("Light") == true;
        bool isColorBlind = source?.Contains("ColorBlind") == true;

        Current = (isLight, isColorBlind) switch
        {
            (true,  true)  => Theme.LightColorBlind,
            (true,  false) => Theme.Light,
            (false, true)  => Theme.DarkColorBlind,
            (false, false) => Theme.Dark,
        };
    }
}

public enum Theme
{
    Dark,
    Light,
    DarkColorBlind,
    LightColorBlind
}
