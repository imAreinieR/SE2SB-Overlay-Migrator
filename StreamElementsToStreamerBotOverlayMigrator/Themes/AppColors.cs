using System.Windows.Media;

namespace StreamElementsToStreamerBotOverlayMigrator.Themes;

/// <summary>
/// Code-behind mirror of Themes/Colors.xaml and Themes/Colors.Light.xaml.
/// Use these constants wherever a <see cref="Color"/> or <see cref="SolidColorBrush"/>
/// must be constructed at runtime (e.g. dynamic brush swaps in event handlers).
///
/// Always access colors through the static properties (e.g. <c>AppColors.Surface</c>)
/// rather than the nested Dark/Light structs directly — they automatically return
/// the correct palette for the active theme.
///
/// Keep in sync with both Colors.xaml files.
/// </summary>
public static class AppColors
{
    // ── Dark palette ─────────────────────────────────────────────────────────
    private static class Dark
    {
        public static readonly Color AppBg                = Color.FromRgb(0x0E, 0x10, 0x17);
        public static readonly Color SidebarBg            = Color.FromRgb(0x0A, 0x0C, 0x14);
        public static readonly Color PanelBg              = Color.FromRgb(0x13, 0x16, 0x1F);
        public static readonly Color Surface              = Color.FromRgb(0x1A, 0x1E, 0x2B);
        public static readonly Color SurfaceAlt           = Color.FromRgb(0x21, 0x26, 0x3A);
        public static readonly Color Border               = Color.FromRgb(0x25, 0x2C, 0x40);
        public static readonly Color DropZoneBorder       = Color.FromRgb(0x3D, 0x4A, 0x6E);
        public static readonly Color DropZoneActiveBorder = Color.FromRgb(0x4F, 0x7E, 0xFF);
        public static readonly Color DropZoneActiveFill   = Color.FromArgb(0x1A, 0x4F, 0x7E, 0xFF);
        public static readonly Color AccentBlue           = Color.FromRgb(0x4F, 0x7E, 0xFF);
        public static readonly Color AccentGreen          = Color.FromRgb(0x3E, 0xCF, 0x8E);
        public static readonly Color TextSecondary        = Color.FromRgb(0x5A, 0x62, 0x80);
        public static readonly Color TextDim              = Color.FromRgb(0x3A, 0x40, 0x60);
        public static readonly Color Danger               = Color.FromRgb(0xFF, 0x6B, 0x6B);
    }

    // ── Light palette ────────────────────────────────────────────────────────
    private static class Light
    {
        public static readonly Color AppBg                = Color.FromRgb(0xF0, 0xF2, 0xF8);
        public static readonly Color SidebarBg            = Color.FromRgb(0xE6, 0xE9, 0xF2);
        public static readonly Color PanelBg              = Color.FromRgb(0xF5, 0xF6, 0xFA);
        public static readonly Color Surface              = Color.FromRgb(0xFF, 0xFF, 0xFF);
        public static readonly Color SurfaceAlt           = Color.FromRgb(0xED, 0xF0, 0xF7);
        public static readonly Color Border               = Color.FromRgb(0xD0, 0xD5, 0xE8);
        public static readonly Color DropZoneBorder       = Color.FromRgb(0x9A, 0xAA, 0xD0);
        public static readonly Color DropZoneActiveBorder = Color.FromRgb(0x3D, 0x6F, 0xFF);
        public static readonly Color DropZoneActiveFill   = Color.FromArgb(0x1A, 0x3D, 0x6F, 0xFF);
        public static readonly Color AccentBlue           = Color.FromRgb(0x3D, 0x6F, 0xFF);
        public static readonly Color AccentGreen          = Color.FromRgb(0x28, 0xA8, 0x70);
        public static readonly Color TextSecondary        = Color.FromRgb(0x5A, 0x62, 0x80);
        public static readonly Color TextDim              = Color.FromRgb(0x9A, 0xA0, 0xBC);
        public static readonly Color Danger               = Color.FromRgb(0xD9, 0x36, 0x36);
    }

    // ── Theme-aware accessors ────────────────────────────────────────────────
    private static bool IsDark => ThemeManager.Current == Theme.Dark;

    public static Color AppBg                => IsDark ? Dark.AppBg                : Light.AppBg;
    public static Color SidebarBg            => IsDark ? Dark.SidebarBg            : Light.SidebarBg;
    public static Color PanelBg              => IsDark ? Dark.PanelBg              : Light.PanelBg;
    public static Color Surface              => IsDark ? Dark.Surface              : Light.Surface;
    public static Color SurfaceAlt           => IsDark ? Dark.SurfaceAlt           : Light.SurfaceAlt;
    public static Color Border               => IsDark ? Dark.Border               : Light.Border;
    public static Color DropZoneBorder       => IsDark ? Dark.DropZoneBorder       : Light.DropZoneBorder;
    public static Color DropZoneActiveBorder => IsDark ? Dark.DropZoneActiveBorder : Light.DropZoneActiveBorder;
    public static Color DropZoneActiveFill   => IsDark ? Dark.DropZoneActiveFill   : Light.DropZoneActiveFill;
    public static Color AccentBlue           => IsDark ? Dark.AccentBlue           : Light.AccentBlue;
    public static Color AccentGreen          => IsDark ? Dark.AccentGreen          : Light.AccentGreen;
    public static Color TextSecondary        => IsDark ? Dark.TextSecondary        : Light.TextSecondary;
    public static Color TextDim              => IsDark ? Dark.TextDim              : Light.TextDim;
    public static Color Danger               => IsDark ? Dark.Danger               : Light.Danger;

    // ── Convenience factory ──────────────────────────────────────────────────
    /// <summary>Creates a new <see cref="SolidColorBrush"/> from a palette color.</summary>
    public static SolidColorBrush Brush(Color color) => new(color);

    // ── Pre-built brushes for the most frequent runtime usages ───────────────
    // SetStatus() foregrounds
    public static SolidColorBrush StatusDefault        => Brush(TextSecondary);
    public static SolidColorBrush StatusError          => Brush(Danger);
    public static SolidColorBrush StatusSuccess        => Brush(AccentGreen);

    // SetDropZoneHighlight() colors
    public static SolidColorBrush DropZoneBorderIdle   => Brush(DropZoneBorder);
    public static SolidColorBrush DropZoneBorderActive => Brush(DropZoneActiveBorder);
    public static SolidColorBrush DropZoneFillIdle     => new(Colors.Transparent);
    public static SolidColorBrush DropZoneFillActive   => Brush(DropZoneActiveFill);

    // FileListBorder background states
    public static SolidColorBrush FileListBgFilled     => Brush(Surface);
    public static SolidColorBrush FileListBgEmpty      => new(Colors.Transparent);
}
