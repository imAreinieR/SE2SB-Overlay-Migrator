using StreamElementsToStreamerBotOverlayMigrator.Themes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace StreamElementsToStreamerBotOverlayMigrator.Dialogs;

public partial class StyledMessageBox: Window
{
    public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

    private StyledMessageBox(string message, string title, MessageBoxButton button, MessageBoxImage icon)
    {
        InitializeComponent();

        TitleText.Text   = title;
        MessageText.Text = message;

        SetIcon(icon);
        BuildButtons(button);

        Owner = Application.Current?.MainWindow;
    }

    // ── Static entry points (match MessageBox.Show signatures) ─────────────

    public static MessageBoxResult Show(string messageBoxText)
        => Show(messageBoxText, string.Empty, MessageBoxButton.OK, MessageBoxImage.None);

    public static MessageBoxResult Show(string messageBoxText, string caption)
        => Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.None);

    public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button)
        => Show(messageBoxText, caption, button, MessageBoxImage.None);

    public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
    {
        var dialog = new StyledMessageBox(messageBoxText, caption, button, icon);
        dialog.ShowDialog();
        return dialog.Result;
    }

    // ── Icon ─────────────────────────────────────────────────────────────

    private void SetIcon(MessageBoxImage icon)
    {
        (Brush background, string glyph) = icon switch
        {
            MessageBoxImage.Warning     => (AppColors.Brush(AppColors.Warning),    "!"),
            MessageBoxImage.Error       => (AppColors.Brush(AppColors.Danger),     "\u2715"),
            MessageBoxImage.Question    => (AppColors.Brush(AppColors.AccentBlue), "?"),
            MessageBoxImage.Information => (AppColors.Brush(AppColors.AccentBlue), "i"),
            _                           => (Brushes.Transparent, string.Empty)
        };

        if (string.IsNullOrEmpty(glyph))
        {
            IconBorder.Visibility = Visibility.Collapsed;
            return;
        }

        IconBorder.Background = background;
        IconGlyph.Text        = glyph;
    }

    // ── Buttons ──────────────────────────────────────────────────────────

    private void BuildButtons(MessageBoxButton button)
    {
        switch (button)
        {
            case MessageBoxButton.OK:
                AddButton("OK",     MessageBoxResult.OK,     isPrimary: true, isDefault:  true);
                break;
            case MessageBoxButton.OKCancel:
                AddButton("Cancel", MessageBoxResult.Cancel, isPrimary: false, isCancel:  true);
                AddButton("OK",     MessageBoxResult.OK,     isPrimary: true,  isDefault: true);
                break;
            case MessageBoxButton.YesNo:
                AddButton("No",     MessageBoxResult.No,     isPrimary: false, isCancel:  true);
                AddButton("Yes",    MessageBoxResult.Yes,    isPrimary: true,  isDefault: true);
                break;
            case MessageBoxButton.YesNoCancel:
                AddButton("Cancel", MessageBoxResult.Cancel, isPrimary: false, isCancel:  true);
                AddButton("No",     MessageBoxResult.No,     isPrimary: false);
                AddButton("Yes",    MessageBoxResult.Yes,    isPrimary: true,  isDefault: true);
                break;
        }
    }

    private void AddButton(string text, MessageBoxResult result, bool isPrimary, bool isDefault = false, bool isCancel = false)
    {
        var button = new Button
        {
            Content   = text,
            Style     = (Style) FindResource(isPrimary ? "MsgBoxPrimaryBtn" : "MsgBoxSecondaryBtn"),
            IsDefault = isDefault,
            IsCancel  = isCancel,
            Margin    = new Thickness(8, 0, 0, 0)
        };

        button.Click += (_, _) =>
        {
            Result = result;
            Close();
        };

        ButtonPanel.Children.Add(button);
    }

    // ── Chrome behavior ──────────────────────────────────────────────────

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Result = MessageBoxResult.Cancel;
        Close();
    }
}