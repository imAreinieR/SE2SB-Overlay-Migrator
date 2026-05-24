using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StreamElementsToStreamerBotOverlayMigrator.Controls;

public partial class ColorSwatchPicker: UserControl
{
    public static readonly DependencyProperty ColorProperty = DependencyProperty.Register
    (
        nameof(Color),
        typeof(Color),
        typeof(ColorSwatchPicker),
        new FrameworkPropertyMetadata
        (
            Colors.White,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnColorChanged
        )
    );

    public Color Color
    {
        get => (Color) GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public string RgbaString
    {
        get
        {
            Color color  = Color;
            double alpha = Math.Round(color.A / 255.0, 2);
            return $"rgba({color.R},{color.G},{color.B},{alpha})";
        }
    }

    public event EventHandler? ColorChanged;

    public ColorSwatchPicker()
    {
        InitializeComponent();
        Loaded += (_, _) => SyncSwatch();
    }

    private void SwatchButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ColorPickerDialog(Color)
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() != true)
            return;

        Color = dialog.SelectedColor;
    }

    private static void OnColorChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is not ColorSwatchPicker picker)
            return;

        picker.SyncSwatch();
        picker.ColorChanged?.Invoke(picker, EventArgs.Empty);
    }

    private void SyncSwatch()
    {
        if (SwatchButton is null)
            return;

        SwatchButton.Background = new SolidColorBrush(Color);
        RgbaBox.Text            = RgbaString;
    }
}