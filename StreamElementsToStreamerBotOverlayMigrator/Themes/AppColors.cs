using System.Windows.Media;

namespace StreamElementsToStreamerBotOverlayMigrator.Themes;

/// <summary>
/// Code-behind mirror of Themes/Colors.xaml.
/// Use these constants wherever a <see cref="Color"/> or <see cref="SolidColorBrush"/>
/// must be constructed at runtime (e.g. dynamic brush swaps in event handlers).
///
/// Keep in sync with Colors.xaml — both files are the single source of truth
/// split across XAML and C# worlds.
/// </summary>
public static class AppColors
{
    // ── Backgrounds ─────────────────────────────────────────────────────────
    public static readonly Color AppBg        = Color.FromRgb(0x0E, 0x10, 0x17);
    public static readonly Color SidebarBg    = Color.FromRgb(0x0A, 0x0C, 0x14);
    public static readonly Color PanelBg      = Color.FromRgb(0x13, 0x16, 0x1F);
    public static readonly Color Surface      = Color.FromRgb(0x1A, 0x1E, 0x2B);
    public static readonly Color SurfaceAlt   = Color.FromRgb(0x21, 0x26, 0x3A);

    // ── Borders ──────────────────────────────────────────────────────────────
    public static readonly Color Border           = Color.FromRgb(0x25, 0x2C, 0x40);
    public static readonly Color DropZoneBorder   = Color.FromRgb(0x3D, 0x4A, 0x6E);
    public static readonly Color DropZoneActiveBorder = Color.FromRgb(0x4F, 0x7E, 0xFF);  // same as AccentBlue
    public static readonly Color DropZoneActiveFill   = Color.FromArgb(0x1A, 0x4F, 0x7E, 0xFF);

    // ── Accent ───────────────────────────────────────────────────────────────
    public static readonly Color AccentBlue  = Color.FromRgb(0x4F, 0x7E, 0xFF);
    public static readonly Color AccentGreen = Color.FromRgb(0x3E, 0xCF, 0x8E);

    // ── Text ─────────────────────────────────────────────────────────────────
    public static readonly Color TextSecondary = Color.FromRgb(0x5A, 0x62, 0x80);
    public static readonly Color TextDim       = Color.FromRgb(0x3A, 0x40, 0x60);

    // ── Semantic ─────────────────────────────────────────────────────────────
    public static readonly Color Danger = Color.FromRgb(0xFF, 0x6B, 0x6B);

    // ── Convenience factory ───────────────────────────────────────────────────
    /// <summary>Creates a new <see cref="SolidColorBrush"/> from a palette color.</summary>
    public static SolidColorBrush Brush(Color color) => new(color);

    // ── Pre-built brushes for the most frequent runtime usages ───────────────
    // SetStatus() foregrounds
    public static SolidColorBrush StatusDefault => Brush(TextSecondary);
    public static SolidColorBrush StatusError   => Brush(Danger);
    public static SolidColorBrush StatusSuccess => Brush(AccentGreen);

    // SetDropZoneHighlight() colors
    public static SolidColorBrush DropZoneBorderIdle   => Brush(DropZoneBorder);
    public static SolidColorBrush DropZoneBorderActive => Brush(DropZoneActiveBorder);
    public static SolidColorBrush DropZoneFillIdle     => new(Colors.Transparent);
    public static SolidColorBrush DropZoneFillActive   => Brush(DropZoneActiveFill);

    // FileListBorder background states
    public static SolidColorBrush FileListBgFilled  => Brush(Surface);
    public static SolidColorBrush FileListBgEmpty   => new(Colors.Transparent);
}
