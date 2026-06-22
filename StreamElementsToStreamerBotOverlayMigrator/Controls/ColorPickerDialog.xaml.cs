using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace StreamElementsToStreamerBotOverlayMigrator.Controls;

public partial class ColorPickerDialog: Window
{
    // ── Public result ─────────────────────────────────────────────────────────

    /// <summary>The colour chosen by the user. Only valid when DialogResult == true.</summary>
    public Color SelectedColor { get; private set; }

    // ── Internal HSV state ────────────────────────────────────────────────────

    private double _hue        = 0;    // 0–360
    private double _saturation = 1;    // 0–1
    private double _value      = 1;    // 0–1
    private byte   _alpha      = 255;  // 0–255

    private bool   _updating;

    public ColorPickerDialog(Color initial)
    {
        InitializeComponent();

        // Store initial values — don't touch named UI elements yet, layout hasn't run
        RgbToHsv(initial.R, initial.G, initial.B, out _hue, out _saturation, out _value);
        _alpha = initial.A;

        // Defer all UI seeding until the window is fully loaded and laid out
        Loaded += (_, _) =>
        {
            _updating         = true;
            HueSlider.Value   = _hue;
            AlphaSlider.Value = _alpha;
            _updating         = false;

            UpdateHueSurface();
            UpdateSvCursor();
            UpdatePreviewAndInputs();
        };
    }

    // ── SV canvas ─────────────────────────────────────────────────────────────

    private void SvCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        SvCanvas.CaptureMouse();
        UpdateSvFromMouse(e.GetPosition(SvCanvas));
    }

    private void SvCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        => SvCanvas.ReleaseMouseCapture();

    private void SvCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            UpdateSvFromMouse(e.GetPosition(SvCanvas));
    }

    private void UpdateSvFromMouse(Point p)
    {
        _saturation = Math.Clamp(p.X / SvCanvas.ActualWidth, 0, 1);
        _value      = Math.Clamp(1 - p.Y / SvCanvas.ActualHeight, 0, 1);

        UpdateSvCursor();
        UpdatePreviewAndInputs();
    }

    private void UpdateSvCursor()
    {
        double x = _saturation * SvCanvas.ActualWidth - SvCursor.Width / 2;
        double y = (1 - _value) * SvCanvas.ActualHeight - SvCursor.Height / 2;

        Canvas.SetLeft(SvCursor, x);
        Canvas.SetTop (SvCursor, y);
    }

    // ── Hue slider ────────────────────────────────────────────────────────────

    private void HueSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updating)
            return;

        _hue = HueSlider.Value;
        UpdateHueSurface();
        UpdatePreviewAndInputs();
    }

    private void UpdateHueSurface()
    {
        Color hueColor = HsvToColor(_hue, 1, 1, 255);
        HueSurface.Background = new SolidColorBrush(hueColor);
    }

    // ── Alpha slider ──────────────────────────────────────────────────────────

    private void AlphaSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updating)
            return;

        _alpha = (byte) Math.Round(AlphaSlider.Value);

        // Update the alpha slider track: current colour fading to transparent
        Color opaque           = HsvToColor(_hue, _saturation, _value, 255);
        Color transparent      = Color.FromArgb(0, opaque.R, opaque.G, opaque.B);
        AlphaSlider.Background = new LinearGradientBrush
        (
            transparent,
            opaque,
            new Point(0, 0),
            new Point(1, 0)
        );

        UpdatePreviewAndInputs();
    }

    // ── Text inputs ───────────────────────────────────────────────────────────

    private void HexBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating)
            return;

        string hex = HexBox.Text.TrimStart('#');
        if (hex.Length != 6 && hex.Length != 8)
            return;

        try
        {
            byte a = 255;
            byte r;
            byte g;
            byte b;

            if (hex.Length == 8)
            {
                a = Convert.ToByte(hex[0..2], 16);
                r = Convert.ToByte(hex[2..4], 16);
                g = Convert.ToByte(hex[4..6], 16);
                b = Convert.ToByte(hex[6..8], 16);
            }
            else
            {
                r = Convert.ToByte(hex[0..2], 16);
                g = Convert.ToByte(hex[2..4], 16);
                b = Convert.ToByte(hex[4..6], 16);
            }

            _alpha = a;
            RgbToHsv(r, g, b, out _hue, out _saturation, out _value);

            _updating         = true;
            HueSlider.Value   = _hue;
            AlphaSlider.Value = _alpha;
            _updating         = false;

            UpdateHueSurface();
            UpdateSvCursor();
            UpdatePreviewAndInputs(skipHex: true);
        }
        catch
        {}
    }

    private void RgbaBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if
        (
            _updating
                || !byte.TryParse(RBox.Text, out byte r)
                || !byte.TryParse(GBox.Text, out byte g)
                || !byte.TryParse(BBox.Text, out byte b)
                || !byte.TryParse(ABox.Text, out byte a)
        )
            return;

        _alpha = a;
        RgbToHsv(r, g, b, out _hue, out _saturation, out _value);

        _updating         = true;
        HueSlider.Value   = _hue;
        AlphaSlider.Value = _alpha;
        _updating         = false;

        UpdateHueSurface();
        UpdateSvCursor();
        UpdatePreviewAndInputs(skipRgba: true);
    }

    // ── Unified preview + input refresh ──────────────────────────────────────

    private void UpdatePreviewAndInputs(bool skipHex = false, bool skipRgba = false)
    {
        // Guard against being called before InitializeComponent finishes wiring named elements
        if (PreviewSwatch is null)
            return;

        Color currentColor       = HsvToColor(_hue, _saturation, _value, _alpha);
        SelectedColor            = currentColor;
        PreviewSwatch.Background = new SolidColorBrush(currentColor);

        // Keep alpha slider track in sync
        Color opaque           = HsvToColor(_hue, _saturation, _value, 255);
        Color transparent      = Color.FromArgb(0, opaque.R, opaque.G, opaque.B);
        AlphaSlider.Background = new LinearGradientBrush
        (
            transparent,
            opaque,
            new Point(0, 0),
            new Point(1, 0)
        );

        _updating = true;

        if (!skipHex)
            HexBox.Text = _alpha == 255
                ? $"#{currentColor.R:X2}{currentColor.G:X2}{currentColor.B:X2}"
                : $"#{currentColor.A:X2}{currentColor.R:X2}{currentColor.G:X2}{currentColor.B:X2}";

        if (!skipRgba)
        {
            RBox.Text = currentColor.R.ToString();
            GBox.Text = currentColor.G.ToString();
            BBox.Text = currentColor.B.ToString();
            ABox.Text = currentColor.A.ToString();
        }

        _updating = false;
    }

    // ── Footer buttons ────────────────────────────────────────────────────────

    private void Apply_Click(object sender, RoutedEventArgs e)
        => DialogResult = true;

    private void Cancel_Click(object sender, RoutedEventArgs e)
        => DialogResult = false;

    // ── HSV ↔ RGB helpers ─────────────────────────────────────────────────────

    private static Color HsvToColor(double h, double s, double v, byte a)
    {
        double r;
        double g;
        double b;

        if (s == 0)
        {
            r = g = b = v;
        }
        else
        {
            h        /= 60;
            int    i  = (int) h;
            double f  = h - i;
            double p  = v * (1 - s);
            double q  = v * (1 - s * f);
            double t  = v * (1 - s * (1 - f));

            (r, g, b) = i switch
            {
                0 => (v, t, p),
                1 => (q, v, p),
                2 => (p, v, t),
                3 => (p, q, v),
                4 => (t, p, v),
                _ => (v, p, q)
            };
        }

        return Color.FromArgb(a, (byte) (r * 255), (byte) (g * 255), (byte) (b * 255));
    }

    private static void RgbToHsv(byte r, byte g, byte b, out double h, out double s, out double v)
    {
        double rd    = r / 255.0;
        double gd    = g / 255.0;
        double bd    = b / 255.0;
        double max   = Math.Max(rd, Math.Max(gd, bd));
        double min   = Math.Min(rd, Math.Min(gd, bd));
        double delta = max - min;

        v = max;
        s = max == 0 ? 0 : delta / max;

        if (delta == 0)
        {
            h = 0;
            return;
        }

        if (max == rd)
            h = 60 * (((gd - bd) / delta) % 6);
        else if (max == gd)
            h = 60 * (((bd - rd) / delta) + 2);
        else
            h = 60 * (((rd - gd) / delta) + 4);

        if (h < 0)
            h += 360;
    }
}