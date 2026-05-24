using System.Windows;
using System.Windows.Controls;

namespace StreamElementsToStreamerBotOverlayMigrator.Controls;

public partial class NumericSpinner: UserControl
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register
    (
        nameof(Value),
        typeof(double),
        typeof(NumericSpinner),
        new FrameworkPropertyMetadata
        (
            0d,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnValueChanged
        )
    );

    public static readonly DependencyProperty StepProperty =
        DependencyProperty.Register(nameof(Step), typeof(double), typeof(NumericSpinner), new PropertyMetadata(1d));

    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(NumericSpinner), new PropertyMetadata(double.MinValue));

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(NumericSpinner), new PropertyMetadata(double.MaxValue));

    public double Value
    {
        get => (double) GetValue(ValueProperty);
        set => SetValue(ValueProperty, Math.Clamp(value, Minimum, Maximum));
    }

    public double Step
    {
        get => (double) GetValue(StepProperty);
        set => SetValue(StepProperty, value);
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

    public event EventHandler? ValueChanged;

    private bool _updating;

    public NumericSpinner()
    {
        InitializeComponent();
        Loaded += (_, _) => SyncTextBox();
    }

    private void UpBtn_Click(object sender, RoutedEventArgs e)
        => Value = Math.Clamp(Value + Step, Minimum, Maximum);

    private void DownBtn_Click(object sender, RoutedEventArgs e)
        => Value = Math.Clamp(Value - Step, Minimum, Maximum);

    private void ValueBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating)
            return;

        if (double.TryParse(ValueBox.Text, out double parsed))
        {
            _updating = true;
            Value     = Math.Clamp(parsed, Minimum, Maximum);
            _updating = false;

            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private static void OnValueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        if (o is not NumericSpinner spinner)
            return;

        spinner.SyncTextBox();
        spinner.ValueChanged?.Invoke(spinner, EventArgs.Empty);
    }

    private void SyncTextBox()
    {
        if (ValueBox is null)
            return;

        _updating     = true;
        ValueBox.Text = Value.ToString("G");
        _updating     = false;
    }
}