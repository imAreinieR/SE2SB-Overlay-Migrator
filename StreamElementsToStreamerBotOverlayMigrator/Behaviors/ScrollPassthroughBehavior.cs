using System.Windows;
using System.Windows.Input;

namespace StreamElementsToStreamerBotOverlayMigrator.Behaviors;

/// <summary>
/// Attached property that prevents a ListBox from swallowing MouseWheel events,
/// allowing them to bubble up to the parent ScrollViewer.
/// </summary>
public static class ScrollPassthrough
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached
    (
        "IsEnabled",
        typeof(bool),
        typeof(ScrollPassthrough),
        new PropertyMetadata(false, OnIsEnabledChanged)
    );

    public static bool GetIsEnabled(DependencyObject obj)
        => (bool) obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value)
        => obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element)
            return;

        if ((bool) e.NewValue)
            element.PreviewMouseWheel += OnPreviewMouseWheel;
        else
            element.PreviewMouseWheel -= OnPreviewMouseWheel;
    }

    private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled || sender is not UIElement element)
            return;

        e.Handled = true;

        var args = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
        {
            RoutedEvent = UIElement.MouseWheelEvent
        };

        element.RaiseEvent(args);
    }
}