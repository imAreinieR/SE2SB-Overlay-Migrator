using Microsoft.Web.WebView2.Wpf;
using System.Windows;

namespace StreamElementsToStreamerBotOverlayMigrator.Controls;

/// <summary>
/// Hosts a classic WebView2 control in a borderless, owned window that tracks the
/// screen bounds of a placeholder element inside a parent window. Avoids WPF airspace
/// issues without requiring WebView2CompositionControl (and therefore without needing
/// a Windows-versioned TargetFramework).
/// </summary>
public sealed class FloatingWebViewHost: IDisposable
{
    private readonly Window           _owner;
    private readonly FrameworkElement _placeholder;
    private readonly Window           _floatingWindow;

    public WebView2 WebView { get; }

    public FloatingWebViewHost(Window owner, FrameworkElement placeholder)
    {
        _owner       = owner       ?? throw new ArgumentNullException(nameof(owner));
        _placeholder = placeholder ?? throw new ArgumentNullException(nameof(placeholder));

        WebView = new WebView2();

        _floatingWindow = new Window
        {
            WindowStyle   = WindowStyle.None,
            ShowInTaskbar = false,
            ResizeMode    = ResizeMode.NoResize,
            Content       = WebView
        };

        _owner.LocationChanged     += (_, _) => Reposition();
        _owner.SizeChanged         += (_, _) => Reposition();
        _owner.StateChanged        += (_, _) => OnOwnerStateChanged();
        _placeholder.SizeChanged   += (_, _) => Reposition();
        _placeholder.LayoutUpdated += (_, _) => Reposition();

        _owner.Loaded += (_, _) => Show();
        _owner.Closed += (_, _) => Dispose();
    }

    public void Show()
    {
        if (_floatingWindow.Owner is null)
            _floatingWindow.Owner = _owner;

        Reposition();
        _floatingWindow.Show();
    }

    private void OnOwnerStateChanged()
    {
        if (_owner.WindowState == WindowState.Minimized)
        {
            _floatingWindow.Hide();
        }
        else
        {
            _floatingWindow.Show();
            Reposition();
        }
    }

    private void Reposition()
    {
        if (_owner.WindowState == WindowState.Minimized)
            return;

        if (PresentationSource.FromVisual(_placeholder) is null)
            return;

        Point topLeft = _placeholder.PointToScreen(new Point(0, 0));

        _floatingWindow.Left   = topLeft.X;
        _floatingWindow.Top    = topLeft.Y;
        _floatingWindow.Width  = _placeholder.ActualWidth;
        _floatingWindow.Height = _placeholder.ActualHeight;
    }

    public void Dispose()
    {
        WebView.Dispose();
        _floatingWindow.Close();
    }
}