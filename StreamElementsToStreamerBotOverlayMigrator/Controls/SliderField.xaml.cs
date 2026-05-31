using System.Windows;
using System.Windows.Controls;

namespace StreamElementsToStreamerBotOverlayMigrator.Controls;

public partial class SliderField: UserControl
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register
    (
        nameof(Value),
        typeof(double),
        typeof(SliderField),
        new FrameworkPropertyMetadata
        (
            0d,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnValueChanged
        )
    );

    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(SliderField), new PropertyMetadata(0d, OnRangeChanged));

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(SliderField), new PropertyMetadata(100d, OnRangeChanged));

    public static readonly DependencyProperty StepProperty    = DependencyProperty.Register(nameof(Step), typeof(double),    typeof(SliderField), new PropertyMetadata(1d, OnRangeChanged));

    public double Value
    {
        get => (double) GetValue(ValueProperty);
        set => SetValue(ValueProperty, Math.Clamp(value, Minimum, Maximum));
    }

    public double Minimum
    {
        get => (double) GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => (double) GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double Step
    {
        get => (double) GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }

    public event EventHandler? ValueChanged;

    private bool _updating;

    public SliderField()
    {
        InitializeComponent();
        Loaded += (_, _) => SyncSlider();
    }

    private void TrackSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updating)
            return;

        _updating = true;
        Value     = TrackSlider.Value;
        _updating = false;

        SyncReadout();
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not SliderField control)
            return;

        control.SyncSlider();
        control.ValueChanged?.Invoke(control, EventArgs.Empty);
    }

    private static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not SliderField control)
            return;

        control.SyncSlider();
    }

    private void SyncSlider()
    {
        if (TrackSlider is null)
            return;

        _updating = true;

        TrackSlider.Minimum           = Minimum;
        TrackSlider.Maximum           = Maximum;
        TrackSlider.TickFrequency     = Step;
        TrackSlider.IsSnapToTickEnabled = Step >= 1;
        TrackSlider.Value             = Math.Clamp(Value, Minimum, Maximum);

        _updating = false;

        SyncReadout();
    }

    private void SyncReadout()
    {
        if (Readout is null)
            return;

        Readout.Text = Value.ToString("G");
    }
}