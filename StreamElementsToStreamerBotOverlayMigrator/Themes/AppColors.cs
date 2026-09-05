using System.Windows.Media;

namespace StreamElementsToStreamerBotOverlayMigrator.Themes;

/// <summary>
/// Code-behind mirror of the four Themes/Colors.*.xaml dictionaries.
/// Use these constants wherever a <see cref="Color"/> or <see cref="SolidColorBrush"/>
/// must be constructed at runtime (e.g. dynamic brush swaps in event handlers).
///
/// Always access colors through the static properties (e.g. <c>AppColors.Surface</c>)
/// rather than the nested Dark/Light/*ColorBlind structs directly — they automatically
/// return the correct palette for the active theme.
///
/// Keep in sync with all four Colors.*.xaml files.
/// </summary>
public static class AppColors
{
    // ── Dark palette ─────────────────────────────────────────────────────────
    private static class Dark
    {
        public static readonly Color AppBg                = Color.FromRgb (0x0E, 0x10, 0x17);
        public static readonly Color SidebarBg            = Color.FromRgb (0x0A, 0x0C, 0x14);
        public static readonly Color PanelBg              = Color.FromRgb (0x13, 0x16, 0x1F);
        public static readonly Color Surface              = Color.FromRgb (0x1A, 0x1E, 0x2B);
        public static readonly Color SurfaceAlt           = Color.FromRgb (0x21, 0x26, 0x3A);
        public static readonly Color Border               = Color.FromRgb (0x25, 0x2C, 0x40);
        public static readonly Color DropZoneBorder       = Color.FromRgb (0x3D, 0x4A, 0x6E);
        public static readonly Color DropZoneActiveBorder = Color.FromRgb (0x4F, 0x7E, 0xFF);
        public static readonly Color DropZoneActiveFill   = Color.FromArgb(0x1A, 0x4F, 0x7E, 0xFF);
        public static readonly Color AccentBlue           = Color.FromRgb (0x4F, 0x7E, 0xFF);
        public static readonly Color AccentGreen          = Color.FromRgb (0x3E, 0xCF, 0x8E);
        public static readonly Color TextSecondary        = Color.FromRgb (0x75, 0x81, 0xA5);
        public static readonly Color TextDim              = Color.FromRgb (0x3A, 0x40, 0x60);
        public static readonly Color Danger               = Color.FromRgb (0xFF, 0x6B, 0x6B);
        public static readonly Color Warning              = Color.FromRgb (0xF0, 0xA0, 0x4B);
    }

    // ── Dark, color-blind-safe palette ──────────────────────────────────────
    // Only the Danger / Warning / AccentGreen (success) triad changes — those are the
    // colors carrying red/yellow/green meaning. Everything else is identical to Dark,
    // so it's referenced directly rather than duplicated.
    private static class DarkColorBlind
    {
        public static readonly Color AccentGreen = Color.FromRgb(0x22, 0xC3, 0xB0); // teal, replaces green
        public static readonly Color Danger      = Color.FromRgb(0xFF, 0x5C, 0x8A); // rose/magenta, replaces red
        public static readonly Color Warning     = Color.FromRgb(0xFF, 0xD4, 0x3B); // gold, shifted off orange
    }

    // ── Light palette ────────────────────────────────────────────────────────
    private static class Light
    {
        public static readonly Color AppBg                = Color.FromRgb (0xF0, 0xF2, 0xF8);
        public static readonly Color SidebarBg            = Color.FromRgb (0xE6, 0xE9, 0xF2);
        public static readonly Color PanelBg              = Color.FromRgb (0xF5, 0xF6, 0xFA);
        public static readonly Color Surface              = Color.FromRgb (0xFF, 0xFF, 0xFF);
        public static readonly Color SurfaceAlt           = Color.FromRgb (0xED, 0xF0, 0xF7);
        public static readonly Color Border               = Color.FromRgb (0xD0, 0xD5, 0xE8);
        public static readonly Color DropZoneBorder       = Color.FromRgb (0x9A, 0xAA, 0xD0);
        public static readonly Color DropZoneActiveBorder = Color.FromRgb (0x3D, 0x6F, 0xFF);
        public static readonly Color DropZoneActiveFill   = Color.FromArgb(0x1A, 0x3D, 0x6F, 0xFF);
        public static readonly Color AccentBlue           = Color.FromRgb (0x3D, 0x6F, 0xFF);
        public static readonly Color AccentGreen          = Color.FromRgb (0x28, 0xA8, 0x70);
        public static readonly Color TextSecondary        = Color.FromRgb (0x5A, 0x62, 0x80);
        public static readonly Color TextDim              = Color.FromRgb (0x9A, 0xA0, 0xBC);
        public static readonly Color Danger               = Color.FromRgb (0xD9, 0x36, 0x36);
        public static readonly Color Warning              = Color.FromRgb (0xC0, 0x70, 0x10);
    }

    // ── Light, color-blind-safe palette ─────────────────────────────────────
    private static class LightColorBlind
    {
        public static readonly Color AccentGreen = Color.FromRgb(0x0C, 0x3C, 0x55); // deep teal, replaces green
        public static readonly Color Danger      = Color.FromRgb(0xD6, 0x33, 0x6C); // rose/magenta, replaces red
        public static readonly Color Warning     = Color.FromRgb(0xB0, 0x8A, 0x00); // gold, shifted off orange
    }

    // ── File-type tag palette ───────────────────────────────────────────────
    private static class FileType
    {
        public static readonly Color Html       = Color.FromRgb(0xFF,  0x63,  0x84);
        public static readonly Color Javascript = Color.FromRgb(0x36,  0xA2,  0xEB);
        public static readonly Color Css        = Color.FromRgb(0xFF,  0xCE,  0x56);
        public static readonly Color FieldJson  = Color.FromRgb(0x6C,  0x70,  0x7A);
        public static readonly Color DataJson   = Color.FromRgb(0xFF,  0xFF,  0xFF);
        public static readonly Color ImageAsset = Color.FromRgb(0xFF,  0x9F,  0x40);
        public static readonly Color AudioAsset = Color.FromRgb(0x4B,  0xC0,  0xC0);
        public static readonly Color VideoAsset = Color.FromRgb(0x99,  0x66,  0xFF);
        public static readonly Color Other      = Color.FromRgb(0x6C,  0x70,  0x7A);
    }

    // ── Theme-aware accessors ────────────────────────────────────────────────
    private static bool IsDark       => ThemeManager.IsDark(ThemeManager.Current);
    private static bool IsColorBlind => ThemeManager.IsColorBlind(ThemeManager.Current);

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
    public static Color TextSecondary        => IsDark ? Dark.TextSecondary        : Light.TextSecondary;
    public static Color TextDim              => IsDark ? Dark.TextDim              : Light.TextDim;

    // Danger / Warning / AccentGreen carry red-yellow-green meaning, so they resolve
    // across both the Dark/Light axis and the normal/ColorBlind axis.
    public static Color AccentGreen => (IsDark, IsColorBlind) switch
    {
        (true,  true)  => DarkColorBlind.AccentGreen,
        (true,  false) => Dark.AccentGreen,
        (false, true)  => LightColorBlind.AccentGreen,
        (false, false) => Light.AccentGreen,
    };

    public static Color Danger => (IsDark, IsColorBlind) switch
    {
        (true,  true)  => DarkColorBlind.Danger,
        (true,  false) => Dark.Danger,
        (false, true)  => LightColorBlind.Danger,
        (false, false) => Light.Danger,
    };

    public static Color Warning => (IsDark, IsColorBlind) switch
    {
        (true,  true)  => DarkColorBlind.Warning,
        (true,  false) => Dark.Warning,
        (false, true)  => LightColorBlind.Warning,
        (false, false) => Light.Warning,
    };

    // ── Convenience factory ──────────────────────────────────────────────────
    /// <summary>Creates a new <see cref="SolidColorBrush"/> from a palette color.</summary>
    public static SolidColorBrush Brush(Color color) => new (color);

    // ── Pre-built brushes for the most frequent runtime usages ───────────────
    // SetStatus() foregrounds
    public static SolidColorBrush StatusDefault         => Brush(TextSecondary);
    public static SolidColorBrush StatusError           => Brush(Danger);
    public static SolidColorBrush StatusSuccess         => Brush(AccentGreen);
    public static SolidColorBrush StatusWarning         => Brush(Warning);

    // Widget.StatusColor (file-set / generation state)
    public static SolidColorBrush WidgetStatusInvalid   => StatusError;
    public static SolidColorBrush WidgetStatusPending   => StatusWarning;
    public static SolidColorBrush WidgetStatusGenerated => StatusSuccess;

    // WidgetFile.WidgetFileTypeColor
    public static SolidColorBrush FileTypeHtml          => Brush(FileType.Html);
    public static SolidColorBrush FileTypeJavascript    => Brush(FileType.Javascript);
    public static SolidColorBrush FileTypeCss           => Brush(FileType.Css);
    public static SolidColorBrush FileTypeFieldJson     => Brush(FileType.FieldJson);
    public static SolidColorBrush FileTypeDataJson      => Brush(FileType.DataJson);
    public static SolidColorBrush FileTypeImageAsset    => Brush(FileType.ImageAsset);
    public static SolidColorBrush FileTypeAudioAsset    => Brush(FileType.AudioAsset);
    public static SolidColorBrush FileTypeVideoAsset    => Brush(FileType.VideoAsset);
    public static SolidColorBrush FileTypeOther         => Brush(FileType.Other);

    // SetDropZoneHighlight() colors
    public static SolidColorBrush DropZoneBorderIdle    => Brush(DropZoneBorder);
    public static SolidColorBrush DropZoneBorderActive  => Brush(DropZoneActiveBorder);
    public static SolidColorBrush DropZoneFillIdle      => new (Colors.Transparent);
    public static SolidColorBrush DropZoneFillActive    => Brush(DropZoneActiveFill);

    // FileListBorder background states
    public static SolidColorBrush FileListBgFilled      => Brush(Surface);
    public static SolidColorBrush FileListBgEmpty       => new (Colors.Transparent);
}