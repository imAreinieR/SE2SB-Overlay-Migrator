using StreamElementsToStreamerBotOverlayMigrator.Data;
using StreamElementsToStreamerBotOverlayMigrator.Dialogs;
using StreamElementsToStreamerBotOverlayMigrator.Services;
using StreamElementsToStreamerBotOverlayMigrator.Themes;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StreamElementsToStreamerBotOverlayMigrator;

public partial class SettingsWindow: Window
{
    private const string DefaultHost     = "127.0.0.1";
    private const string DefaultPort     = "8080";
    private const string DefaultEndpoint = "/";

    private readonly AppSettings              _settings;
    private          bool                     _suppressThemeChangeHandler;
    private          CancellationTokenSource? _testConnectionCts;
    private          bool                     _isTestingConnection;
    private          bool                     _isPasswordVisible;

    public bool ThemeChanged { get; private set; }

    public SettingsWindow()
    {
        InitializeComponent();

        _settings = SettingsService.Current;

        HostBox.TextChanged           += HostBox_TextChanged;
        PortBox.TextChanged           += PortBox_TextChanged;
        EndpointBox.TextChanged       += EndpointBox_TextChanged;
        PasswordBox.PasswordChanged   += PasswordBox_PasswordChanged;
        PasswordRevealBox.TextChanged += PasswordRevealBox_TextChanged;

        LoadGeneralTab();
        LoadStreamerBotTab();
    }

    #region UI Elements

    private void LoadGeneralTab()
    {
        _suppressThemeChangeHandler = true;
        ThemeCombo.SelectedIndex    = ThemeManager.IsDark(ThemeManager.Current) ? 0 : 1;
        ColorBlindToggle.IsChecked  = ThemeManager.IsColorBlind(ThemeManager.Current);
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
        UpdateFormValidity();
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

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateHost() || !ValidatePort())
            return;

        var port = int.Parse(PortBox.Text);

        _settings.Theme                = ThemeManager.Current;
        _settings.Host                 = HostBox.Text.Trim();
        _settings.Port                 = port;
        _settings.Endpoint             = string.IsNullOrWhiteSpace(EndpointBox.Text)
            ? "/"
            : EndpointBox.Text.Trim();
        _settings.EnableAuthentication = EnableAuthToggle.IsChecked == true;
        _settings.Password             = GetPasswordValue();

        SettingsService.Save(_settings);

        UpdateFormValidity();

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void RevertToDefault_Click(object sender, RoutedEventArgs e)
    {
        MessageBoxResult confirm = StyledMessageBox.Show
        (
            "This will reset every setting back to its default value.\nContinue?",
            "Revert to Default",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning
        );

        if (confirm != MessageBoxResult.Yes)
            return;

        if (ThemeManager.Current != Theme.Dark)
        {
            ThemeManager.Apply(Theme.Dark);
            ThemeChanged    = true;
            StatusText.Text = "Theme will update once this window closes.";
        }

        _settings.Theme                = ThemeManager.Current;
        _settings.Host                 = DefaultHost;
        _settings.Port                 = int.Parse(DefaultPort);
        _settings.Endpoint             = DefaultEndpoint;
        _settings.EnableAuthentication = false;
        _settings.Password             = string.Empty;

        SettingsService.Save(_settings);

        // Reset password reveal state before reloading the field from settings.
        _isPasswordVisible                  = false;
        PasswordRevealBox.Text              = string.Empty;
        PasswordRevealBox.Visibility        = Visibility.Collapsed;
        PasswordBox.Visibility              = Visibility.Visible;
        PasswordVisibilityToggleBtn.Content = "Show";

        // Reflect the reverted settings back into the UI.
        LoadGeneralTab();
        LoadStreamerBotTab();

        TestConnectionResultText.Text = string.Empty;

        if (!ThemeChanged)
            StatusText.Text = "Settings reverted to defaults and saved.";

        // The reverted values are now the saved values, so Save/Cancel go back to disabled.
        UpdateFormValidity();
    }

    private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressThemeChangeHandler || ThemeCombo.SelectedItem is not ComboBoxItem item)
            return;

        bool wantsDark   = item.Content?.ToString() == "Dark";
        bool currentDark = ThemeManager.IsDark(ThemeManager.Current);

        if (wantsDark == currentDark)
            return;

        ThemeManager.Toggle();

        ThemeChanged    = true;
        StatusText.Text = "Theme will update once this window closes.";

        UpdateFormValidity();
    }

    private void ColorBlindToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (_suppressThemeChangeHandler)
            return;

        ThemeManager.ToggleColorBlindMode();

        ThemeChanged    = true;
        StatusText.Text = "Theme will update once this window closes.";

        UpdateFormValidity();
    }

    private void EnableAuthToggle_Changed(object sender, RoutedEventArgs e)
    {
        UpdatePasswordFieldEnabled();
        UpdateFormValidity();
    }

    private void PasswordVisibilityToggle_Click(object sender, RoutedEventArgs e)
    {
        _isPasswordVisible = !_isPasswordVisible;

        if (_isPasswordVisible)
        {
            PasswordRevealBox.Text       = PasswordBox.Password;
            PasswordRevealBox.Visibility = Visibility.Visible;
            PasswordBox.Visibility       = Visibility.Collapsed;
            PasswordRevealBox.Focus();
            PasswordRevealBox.CaretIndex = PasswordRevealBox.Text.Length;
        }
        else
        {
            PasswordBox.Password         = PasswordRevealBox.Text;
            PasswordBox.Visibility       = Visibility.Visible;
            PasswordRevealBox.Visibility = Visibility.Collapsed;
            PasswordBox.Focus();
        }

        PasswordVisibilityToggleBtn.Content = _isPasswordVisible
            ? "Hide"
            : "Show";

        UpdateFormValidity();
    }

    private void HostBox_TextChanged(object sender, TextChangedEventArgs e)
        => UpdateFormValidity();

    private void PortBox_TextChanged(object sender, TextChangedEventArgs e)
        => UpdateFormValidity();

    private void EndpointBox_TextChanged(object sender, TextChangedEventArgs e)
        => UpdateFormValidity();

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        => UpdateFormValidity();

    private void PasswordRevealBox_TextChanged(object sender, TextChangedEventArgs e)
        => UpdateFormValidity();

    private void HostClear_Click(object sender, RoutedEventArgs e)
        => HostBox.Text = DefaultHost;

    private void PortClear_Click(object sender, RoutedEventArgs e)
        => PortBox.Text = DefaultPort;

    private void EndpointClear_Click(object sender, RoutedEventArgs e)
        => EndpointBox.Text = DefaultEndpoint;

    private async void TestConnection_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateHost() || !ValidatePort())
            return;

        int    port     = int.Parse(PortBox.Text);
        string host     = HostBox.Text.Trim();
        string endpoint = string.IsNullOrWhiteSpace(EndpointBox.Text)
            ? "/"
            : EndpointBox.Text.Trim();

        if (!endpoint.StartsWith('/'))
            endpoint = "/" + endpoint;

        Uri uri;
        try
        {
            uri = new Uri($"ws://{host}:{port}{endpoint}");
        }
        catch (UriFormatException)
        {
            SetTestResult(TestResultKind.Error, "Host, port, or endpoint isn't a valid address.");
            return;
        }

        _testConnectionCts?.Cancel();
        _testConnectionCts?.Dispose();

        _testConnectionCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var token = _testConnectionCts.Token;

        _isTestingConnection = true;
        UpdateFormValidity();
        SetTestResult(TestResultKind.Pending, "Connecting...");

        using var socket = new ClientWebSocket();

        try
        {
            await socket.ConnectAsync(uri, token);

            string requestId    = Guid.NewGuid().ToString("N");
            string requestJson  = JsonSerializer.Serialize(new { request = "GetInfo", id = requestId });
            byte[] requestBytes = Encoding.UTF8.GetBytes(requestJson);

            await socket.SendAsync(requestBytes, WebSocketMessageType.Text, true, token);

            GetInfoResult? info = await ReceiveGetInfoResponseAsync(socket, requestId, token);

            if (info is null)
            {
                SetTestResult(TestResultKind.Error, "Connected, but no response to GetInfo was received");
            }
            else if (!string.Equals(info.Value.Status, "ok", StringComparison.OrdinalIgnoreCase))
            {
                SetTestResult(TestResultKind.Error, $"Connected, but the server returned status \"{info.Value.Status}\"");
            }
            else if (info.Value.Name is null || !info.Value.Name.Contains("Streamer.bot", StringComparison.OrdinalIgnoreCase))
            {
                SetTestResult(TestResultKind.Error, $"Connected, but this doesn't look like StreamerBot (name: {info.Value.Name ?? "unknown"})");
            }
            else
            {
                SetTestResult(TestResultKind.Success, $"Connected — {info.Value.Name} v{info.Value.Version ?? "?"} ({info.Value.Os ?? "unknown OS"})");
            }

            try
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
            }
            catch
            {
                SetTestResult(TestResultKind.Error, $"Unexpected error when closing connection");
            }
        }
        catch (OperationCanceledException)
        {
            SetTestResult(TestResultKind.Error, "Connection timed out. Check the host, port, and that StreamerBot's WebSocket server is running.");
        }
        catch (WebSocketException websocketException)
        {
            SetTestResult(TestResultKind.Error, $"Could not connect: {websocketException.Message}");
        }
        catch (System.Exception exception)
        {
            SetTestResult(TestResultKind.Error, $"Unexpected error: {exception.Message}");
        }
        finally
        {
            _isTestingConnection = false;
            UpdateFormValidity();
        }
    }

    #endregion Event Handlers

    #region Helpers

    private string GetPasswordValue()
        => _isPasswordVisible
        ? PasswordRevealBox.Text
        : PasswordBox.Password;

    private void UpdatePasswordFieldEnabled()
    {
        bool enabled = EnableAuthToggle.IsChecked == true;

        PasswordBox.IsEnabled                 = enabled;
        PasswordRevealBox.IsEnabled           = enabled;
        PasswordVisibilityToggleBtn.IsEnabled = enabled;
    }

    private bool ValidateHost()
    {
        bool isValid = !string.IsNullOrWhiteSpace(HostBox.Text);
        ApplyFieldValidation(HostBox, HostErrorText, isValid, "Host cannot be empty.");
        return isValid;
    }

    private bool ValidatePort()
    {
        bool isValid = int.TryParse(PortBox.Text, out int port) && port > 0 && port <= 65535;
        ApplyFieldValidation(PortBox, PortErrorText, isValid, "Enter a valid port (1–65535).");
        return isValid;
    }

    private void ApplyFieldValidation(TextBox box, TextBlock errorText, bool isValid, string message)
    {
        if (isValid)
        {
            box.ClearValue(BorderBrushProperty);
            errorText.Text       = string.Empty;
            errorText.Visibility = Visibility.Collapsed;
        }
        else
        {
            box.BorderBrush      = (Brush) FindResource("ErrorBrush");
            errorText.Text       = message;
            errorText.Visibility = Visibility.Visible;
        }
    }

    private void UpdateFormValidity()
    {
        var isValid = ValidateHost() && ValidatePort();

        ApplyBtn.IsEnabled          = isValid;
        TestConnectionBtn.IsEnabled = isValid && !_isTestingConnection;
    }

    private enum TestResultKind { Pending, Success, Error }

    private void SetTestResult(TestResultKind kind, string message)
    {
        TestConnectionResultText.Text       = message;
        TestConnectionResultText.Foreground = kind switch
        {
            TestResultKind.Success => (Brush) FindResource("AccentGreenBrush"),
            TestResultKind.Error   => (Brush) FindResource("ErrorBrush"),
            _                      => (Brush) FindResource("TextDimBrush"),
        };
    }

    private static async Task<GetInfoResult?> ReceiveGetInfoResponseAsync(ClientWebSocket socket, string requestId, CancellationToken token)
    {
        var buffer = new byte[8192];

        while (!token.IsCancellationRequested)
        {
            using var stream = new MemoryStream();
            WebSocketReceiveResult result;

            do
            {
                result = await socket.ReceiveAsync(buffer, token);

                if (result.MessageType == WebSocketMessageType.Close)
                    return null;

                stream.Write(buffer, 0, result.Count);
            }
            while (!result.EndOfMessage);

            string json;
            try
            {
                json = Encoding.UTF8.GetString(stream.ToArray());
                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                if (!root.TryGetProperty("id", out JsonElement idProp) || idProp.GetString() != requestId)
                    continue;

                string status = root.TryGetProperty("status", out JsonElement statusProp)
                    ? statusProp.GetString() ?? string.Empty
                    : string.Empty;

                string? name    = null;
                string? version = null;
                string? os      = null;

                if (root.TryGetProperty("info", out var infoProp))
                {
                    name    = infoProp.TryGetProperty("name",    out var n) ? n.GetString() : null;
                    version = infoProp.TryGetProperty("version", out var v) ? v.GetString() : null;
                    os      = infoProp.TryGetProperty("os",      out var o) ? o.GetString() : null;
                }

                return new GetInfoResult(status, name, version, os);
            }
            catch (JsonException)
            {
            }
        }

        return null;
    }

    private readonly record struct GetInfoResult(string Status, string? Name, string? Version, string? Os);

    #endregion Helpers
}