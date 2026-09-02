using StreamElementsToStreamerBotOverlayMigrator.Data;
using StreamElementsToStreamerBotOverlayMigrator.Services;
using StreamElementsToStreamerBotOverlayMigrator.Themes;
using System.Windows;
using System.Windows.Controls;

namespace StreamElementsToStreamerBotOverlayMigrator;

public partial class SettingsWindow: Window
{
    private readonly AppSettings _settings;
    private          bool        _suppressThemeChangeHandler;

    public bool ThemeChanged { get; private set; }

    public SettingsWindow()
    {
        InitializeComponent();

        _settings = SettingsService.Current;

        LoadGeneralTab();
        LoadStreamerBotTab();
    }

    #region UI Elements

    private void LoadGeneralTab()
    {
        _suppressThemeChangeHandler = true;
        ThemeCombo.SelectedIndex    = ThemeManager.Current == Theme.Dark
            ? 0
            : 1;
        _suppressThemeChangeHandler = false;
    }

    private void LoadStreamerBotTab()
    {
        HostBox.Text               = _settings.Host;
        PortBox.Text               = _settings.Port.ToString();
        EndpointBox.Text           = _settings.Endpoint;
        EnableAuthToggle.IsChecked = _settings.EnableAuthentication;
        PasswordBox.Password       = _settings.Password;

        UpdatePasswordFieldEnabled();
    }

    #endregion UI Elements

    #region Event Handlers

    private void LeftTabGeneral_Checked(object sender, RoutedEventArgs e)
    {
        if (LeftTabStreamerBot is null)
            return;

        LeftTabStreamerBot.IsChecked = false;
        GeneralPanel.Visibility      = Visibility.Visible;
        StreamerBotPanel.Visibility  = Visibility.Collapsed;
    }

    private void LeftTabStreamerBot_Checked(object sender, RoutedEventArgs e)
    {
        if (LeftTabGeneral is null)
            return;

        LeftTabGeneral.IsChecked    = false;
        GeneralPanel.Visibility     = Visibility.Collapsed;
        StreamerBotPanel.Visibility = Visibility.Visible;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(PortBox.Text, out var port) || port <= 0 || port > 65535)
        {
            StatusText.Text = "Enter a valid port (1–65535).";
            return;
        }

        if (string.IsNullOrWhiteSpace(HostBox.Text))
        {
            StatusText.Text = "Host cannot be empty.";
            return;
        }

        _settings.Theme                 = ThemeManager.Current;
        _settings.Host                  = HostBox.Text.Trim();
        _settings.Port                  = port;
        _settings.Endpoint              = string.IsNullOrWhiteSpace(EndpointBox.Text) ? "/" : EndpointBox.Text.Trim();
        _settings.EnableAuthentication  = EnableAuthToggle.IsChecked == true;
        _settings.Password              = PasswordBox.Password;

        SettingsService.Save(_settings);

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressThemeChangeHandler || ThemeCombo.SelectedItem is not ComboBoxItem item)
            return;

        var wantsDark = item.Content?.ToString() == "Dark";
        var wantsChange = (wantsDark && ThemeManager.Current != Theme.Dark)
            || (!wantsDark && ThemeManager.Current == Theme.Dark);

        if (!wantsChange)
            return;

        ThemeManager.Toggle();

        ThemeChanged    = true;
        StatusText.Text = "Theme will update once this window closes.";
    }

    private void EnableAuthToggle_Changed(object sender, RoutedEventArgs e)
        => UpdatePasswordFieldEnabled();

    #endregion Event Handlers

    #region Helpers

    private void UpdatePasswordFieldEnabled()
        => PasswordBox.IsEnabled = EnableAuthToggle.IsChecked == true;

    #endregion Helpers
}