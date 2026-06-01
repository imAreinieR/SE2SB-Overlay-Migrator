using Microsoft.Web.WebView2.Core;
using StreamElementsToStreamerBotOverlayMigrator.Common;
using StreamElementsToStreamerBotOverlayMigrator.Common.ExtensionMethods;
using StreamElementsToStreamerBotOverlayMigrator.Controls;
using StreamElementsToStreamerBotOverlayMigrator.Data;
using StreamElementsToStreamerBotOverlayMigrator.Managers;
using StreamElementsToStreamerBotOverlayMigrator.Services;
using StreamElementsToStreamerBotOverlayMigrator.Templates;
using StreamElementsToStreamerBotOverlayMigrator.Themes;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace StreamElementsToStreamerBotOverlayMigrator;

public partial class WidgetConfigWindow: Window
{
    private readonly Widget                               _widget;
    private readonly Dictionary<string, FrameworkElement> _fieldControls       = new ();
    private readonly List<WidgetDataField>                _allFields           = new ();
    private readonly Dictionary<string, string>           _fileNameAndContents = new();
    private          Dictionary<string, JsonElement>?     _dataValues;
    private          bool                                 _isDirty;
    private          bool                                 _webViewReady        = false;

    private record LogEntryEvent(LogEntry entry);
    private record LogEntry(string source, string level, string text, double timestamp, string? url, int? lineNumber);

    public WidgetConfigWindow(Widget widget)
    {
        InitializeComponent();

        _widget = widget;
        Title   = $"Configure — {widget.Name}";

        LoadDataJson();
        LoadFields();
        InitWebViewAsync();
    }

    private async void InitWebViewAsync()
    {
        try
        {
            await PreviewWebView.EnsureCoreWebView2Async();
            _webViewReady = true;

            PreviewWebView.Visibility     = Visibility.Visible;
            PreviewPlaceholder.Visibility = Visibility.Collapsed;

            await PreviewWebView.CoreWebView2.CallDevToolsProtocolMethodAsync("Log.enable", "{}");
            await PreviewWebView.CoreWebView2.CallDevToolsProtocolMethodAsync("Runtime.enable", "{}");

            var receiver = PreviewWebView.CoreWebView2.GetDevToolsProtocolEventReceiver("Log.entryAdded");
            receiver.DevToolsProtocolEventReceived += OnReceivedBrowserConsoleLog;

            var consoleReceiver = PreviewWebView.CoreWebView2.GetDevToolsProtocolEventReceiver("Runtime.consoleAPICalled");
            consoleReceiver.DevToolsProtocolEventReceived += OnReceivedConsoleApiCall;

            PreviewWebView.CoreWebView2.AddWebResourceRequestedFilter
            (
                "https://app.local/*",
                CoreWebView2WebResourceContext.All
            );
            PreviewWebView.CoreWebView2.WebResourceRequested += OnWebResourceRequested;

            ReloadPreview();
        }
        catch (Exception exception)
        {
            PreviewPlaceholder.Text = $"WebView2 unavailable: {exception.Message}";
            SetStatus($"[ERROR] WebView2 init failed: {exception.Message}", error: true);
        }
    }

    private void OnReceivedBrowserConsoleLog(object? sender, CoreWebView2DevToolsProtocolEventReceivedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.ParameterObjectAsJson))
            return; 

        LogEntryEvent? logEntryEvent = JsonSerializer.Deserialize<LogEntryEvent>(e.ParameterObjectAsJson);

        if (logEntryEvent?.entry is not null)
            Dispatcher.Invoke(() => AppendLog($"[{logEntryEvent.entry.level}] {logEntryEvent.entry.text}"));
    }

    private void OnReceivedConsoleApiCall(object? sender, CoreWebView2DevToolsProtocolEventReceivedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.ParameterObjectAsJson))
            return;

        using JsonDocument doc = JsonDocument.Parse(e.ParameterObjectAsJson);
        JsonElement root = doc.RootElement;

        string type = root.TryGetProperty("type", out JsonElement type_) ? type_.GetString() ?? "log" : "log";

        string message = string.Empty;
        if (root.TryGetProperty("args", out JsonElement args))
        {
            message = string.Join
            (
                " ",
                args
                    .EnumerateArray()
                    .Select
                    (
                        arg =>
                        {
                            if (arg.TryGetProperty("value", out JsonElement value))
                                return value.ToString();
                            if (arg.TryGetProperty("description", out JsonElement description))
                                return description.GetString() ?? string.Empty;
                            return string.Empty;
                        }
                    )
            );
        }

        Dispatcher.Invoke(() => AppendLog($"[{type.ToUpper()}] {message}"));
    }

    private void ReloadPreview()
    {
        if (!_webViewReady)
            return;

        _fileNameAndContents.Clear();

        string dataJson = BuildDataJson();
        _fileNameAndContents["streamerBotEvents.js"] = TemplateFiles.StreamerBotEventHandlersFile;

        _widget
            .Files
            .ToList()
            .ForEach
            (
                widgetFile =>
                {
                    string fileContent = WidgetFileImportAndExportService.GenerateFile(widgetFile, dataJson);

                    if (widgetFile.WidgetFileType == WidgetFileType.Html)
                        fileContent = InjectBaseUrlProxy(fileContent);

                    _fileNameAndContents[widgetFile.GetFileNameForWidgetFileType()] = fileContent;
                }
            );

        if (!_fileNameAndContents.TryGetValue("index.html", out string? htmlContent))
        {
            PreviewWebView.NavigateToString("<body style='background:#0e1017;color:#5a6280;font-family:Consolas;padding:32px'>No HTML file found in widget.</body>");
            SetStatus("[WARN] No HTML file found – preview unavailable.", error: true);
            return;
        }

        PreviewWebView.NavigateToString(htmlContent);
        SetStatus("[INFO] Preview loaded.");
    }

    private static string InjectBaseUrlProxy(string html)
    {
        string injectedScript = $"<base href=\"https://app.local/\" />\n";

        int index = html.IndexOf("<head>", StringComparison.OrdinalIgnoreCase);
        return index >= 0
            ? html.Insert(index, injectedScript)
            : injectedScript + html;
    }

    private void OnWebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
        var    uri               = new Uri(e.Request.Uri);
        string filePath          = Path.Combine("wwwroot", uri.AbsolutePath.TrimStart('/'));
        string requestedFileName = Path.GetFileName(filePath);

        if (!_fileNameAndContents.TryGetValue(requestedFileName, out string? content))
            return;

        // NOTE: Do NOT dispose the stream here — WebView2 reads it asynchronously after this handler returns.
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        e.Response = PreviewWebView.CoreWebView2.Environment.CreateWebResourceResponse
        (
            stream,
            200, "OK",
            $"Content-Type: {GetMimeType(filePath)}"
        );
    }

    private static string GetMimeType(string path) => Path.GetExtension(path) switch
    {
        ".css"  => "text/css",
        ".js"   => "application/javascript",
        ".html" => "text/html",
        ".png"  => "image/png",
        ".svg"  => "image/svg+xml",
        _       => "application/octet-stream"
    };

    #region UI Elements

    private StackPanel BuildGroupPanel(WidgetDataFieldGroup widgetDataFieldGroup)
    {
        var container = new StackPanel();

        var header = new ToggleButton
        {
            Style   = (Style) FindResource("GroupHeaderBtn"),
            Content = new TextBlock
            {
                Text       = widgetDataFieldGroup.Name.ToUpper(),
                FontFamily = new FontFamily("IBM Plex Mono, Consolas"),
                FontSize   = 10,
                Foreground = AppColors.Brush(AppColors.TextSecondary)
            },
            IsChecked = false
        };

        var fieldsPanel = new StackPanel
        {
            Background = AppColors.Brush(AppColors.PanelBg),
            Margin     = new Thickness(0, 0, 0, 4)
        };

        foreach (WidgetDataField widgetDataField in widgetDataFieldGroup.Fields)
        {
            Border? row = BuildFieldRow(widgetDataField);

            if (row is not null)
                fieldsPanel.Children.Add(row);
        }

        header.Checked   += (_, _) => fieldsPanel.Visibility = Visibility.Collapsed;
        header.Unchecked += (_, _) => fieldsPanel.Visibility = Visibility.Visible;

        container.Children.Add(header);
        container.Children.Add(fieldsPanel);

        return container;
    }

    private Border? BuildFieldRow(WidgetDataField widgetDataField)
    {
        FrameworkElement? control = BuildControl(widgetDataField);

        if (control is null)
            return null;

        _fieldControls[widgetDataField.Key] = control;

        var row = new StackPanel
        {
            Margin = new Thickness(14, 10, 14, 10)
        };

        row.Children.Add(new TextBlock
        {
            Text  = widgetDataField.Label,
            Style = (Style) FindResource("FieldLabel")
        });

        row.Children.Add(control);

        return new Border
        {
            Child           = row,
            BorderBrush     = AppColors.Brush(AppColors.Border),
            BorderThickness = new Thickness(0, 0, 0, 1)
        };
    }

    private FrameworkElement? BuildControl(WidgetDataField field)
    {
        string valueStr = field.Value?.ToString() ?? string.Empty;

        switch (field.Type.ToLowerInvariant())
        {
            case "text":
                var textBox = new TextBox
                {
                    Style = (Style) FindResource("FieldInput"),
                    Text  = valueStr,
                    Tag   = field.Key
                };
                textBox.TextChanged += OnControlChanged;
                return textBox;
            case "checkbox":
                bool isChecked = valueStr.Equals("true", StringComparison.OrdinalIgnoreCase)
                    || valueStr == "1";
                var checkBox = new CheckBox
                {
                    Style     = (Style) FindResource("FieldToggle"),
                    IsChecked = isChecked,
                    Tag       = field.Key
                };
                checkBox.Checked   += OnControlChanged;
                checkBox.Unchecked += OnControlChanged;
                return checkBox;
            case "colorpicker":
                var picker = new ColorSwatchPicker
                {
                    Color = ParseColor(valueStr),
                    Tag   = field.Key
                };
                picker.ColorChanged += OnControlChanged;
                return picker;
            case "number":
                var spinner = new NumericSpinner
                {
                    Value = double.TryParse(valueStr, out double initialNumber)
                        ? initialNumber
                        : 1,
                    Tag   = field.Key
                };
                spinner.ValueChanged += OnControlChanged;
                return spinner;
            case "slider":
                var sliderField = new SliderField
                {
                    Minimum = field.Min  ?? 0,
                    Maximum = field.Max  ?? 100,
                    Step    = field.Step ?? 1,
                    Value   = double.TryParse(valueStr, out double initialSlider)
                        ? initialSlider
                        : field.Min ?? 0,
                    Tag     = field.Key
                };
                sliderField.ValueChanged += OnControlChanged;
                return sliderField;
            case "dropdown":
                var comboBox = new StyledDropdown
                {
                    Tag = field.Key
                };

                if (field.Options is not null)
                {
                    foreach ((string key, string label) in field.Options)
                        comboBox.Items.Add
                        (
                            new ComboBoxItem
                            {
                                Content = label,
                                Tag = key
                            }
                        );

                    foreach (ComboBoxItem item in comboBox.Items)
                    {
                        if (item.Tag?.ToString() == valueStr)
                        {
                            comboBox.SelectedItem = item;
                            break;
                        }
                    }

                    comboBox.SelectionChanged += OnControlChanged;
                }

                return comboBox;
            case "image-input":
            case "video-input":
            case "sound-input":
                var unsupportedLabel = new TextBlock
                {
                    Text       = $"'{field.Type}' is not yet supported in the config UI.",
                    Style      = (Style) FindResource("FieldLabel"),
                    FontStyle  = FontStyles.Italic,
                    Foreground = AppColors.StatusError
                };
                return unsupportedLabel;
            case "googlefont":
                if (!GoogleFonts.AvailableFonts.Contains(valueStr))
                    valueStr = GoogleFonts.AvailableFonts.First();

                var fontComboBox = new StyledDropdown
                {
                    Tag = field.Key
                };

                foreach (string fontName in GoogleFonts.AvailableFonts)
                    fontComboBox.Items.Add
                    (
                        new ComboBoxItem
                        {
                            Content = fontName,
                            Tag     = fontName
                        }
                    );

                foreach (ComboBoxItem item in fontComboBox.Items)
                {
                    if (item.Tag?.ToString() == valueStr)
                    {
                        fontComboBox.SelectedItem = item;
                        break;
                    }
                }

                fontComboBox.SelectionChanged += OnControlChanged;
                return fontComboBox;
            case "button":
                var button = new Button
                {
                    Style   = (Style) FindResource("FieldButton"),
                    Content = field.Label,
                    Tag     = field.Key
                };
                button.Click += FieldButton_Click;
                return button;
            default:
                return null;
        }
    }

    #endregion UI Elements

    #region Event Handlers

    private void OnControlChanged(object? sender, EventArgs e)
    {
        if (!_isDirty)
        {
            _isDirty          = true;
            SaveBtn.IsEnabled = true;
            SetStatus("Unsaved changes.");
        }

        ReloadPreview();
    }

    private async void FieldButton_Click(object sender, RoutedEventArgs e)
    {
        string fieldName = (sender as FrameworkElement)?.Tag?.ToString() ?? string.Empty;

        await PreviewWebView.ExecuteScriptAsync($@"
            window.dispatchEvent(new CustomEvent('onEventReceived', {{
                detail: {{
                    event: {{
                        listener: 'widget-button',
                        field: '{fieldName}'
                    }}
                }}
            }}));
        ");
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string json = BuildDataJson();

            WidgetFile? dataFile = _widget
                .Files
                .FirstOrDefault(widgetFile => widgetFile.FileName.Equals("data.json", StringComparison.OrdinalIgnoreCase));

            if (dataFile is not null)
                dataFile.Content = json;
            else
                _widget.AddWidgetFile(new WidgetFile("data.json", json));

            WidgetManager.Save(_widget);

            _isDirty          = false;
            SaveBtn.IsEnabled = false;

            SetStatus("Saved.", success: true);
        }
        catch (Exception exception)
        {
            SetStatus($"Save failed: {exception.Message}", error: true);
        }
    }

    private void TabPreview_Checked(object sender, RoutedEventArgs e)
    {
        if (TabLogs is null || PreviewPanel is null || LogsPanel is null)
            return;

        TabLogs.IsChecked       = false;
        PreviewPanel.Visibility = Visibility.Visible;
        LogsPanel.Visibility    = Visibility.Collapsed;
    }

    private void TabLogs_Checked(object sender, RoutedEventArgs e)
    {
        if (TabPreview is null || LogsPanel is null || PreviewPanel is null)
            return;

        TabPreview.IsChecked    = false;
        LogsPanel.Visibility    = Visibility.Visible;
        PreviewPanel.Visibility = Visibility.Collapsed;
    }

    #endregion Event Handlers

    public void AppendLog(string message)
    {
        string line = $"[{DateTime.Now:HH:mm:ss}] {message}";

        if (LogsText.Text == "No log entries yet.")
            LogsText.Text = line;
        else
            LogsText.Text += $"\n{line}";

        LogsScrollViewer.ScrollToEnd();
    }

    #region Helpers

    private void LoadDataJson()
    {
        WidgetFile? dataFile = _widget
            .Files
            .FirstOrDefault(file => file.WidgetFileType == WidgetFileType.DataJson);

        if (dataFile is null)
            return;

        try
        {
            using JsonDocument jsonDocument = JsonDocument.Parse
            (
                dataFile.Content,
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = true
                }
            );

            _dataValues = jsonDocument
                .RootElement
                .EnumerateObject()
                .ToDictionary
                (
                    jsonProperty => jsonProperty.Name,
                    jsonProperty => jsonProperty.Value.Clone()
                );
        }
        catch
        {
            _dataValues = null;
        }
    }

    private void LoadFields()
    {
        WidgetFile? fieldsFile = _widget
            .Files
            .FirstOrDefault(file => file.WidgetFileType == WidgetFileType.FieldJson);

        if (fieldsFile is null)
        {
            ShowNoFieldsMessage("No fields.json found in this widget.");
            return;
        }

        List<WidgetDataFieldGroup> groups;
        try
        {
            groups = ParseFieldGroups(fieldsFile.Content);
        }
        catch (Exception exception)
        {
            ShowNoFieldsMessage($"Could not parse fields.json: {exception.Message}");
            return;
        }

        if (!groups.Any())
        {
            ShowNoFieldsMessage("No configurable fields found.");
            return;
        }

        foreach (WidgetDataFieldGroup group in groups)
            GroupsPanel.Children.Add(BuildGroupPanel(group));
    }

    private List<WidgetDataFieldGroup> ParseFieldGroups(string json)
    {
        var jsonDocumentOptions = new JsonDocumentOptions
        {
            AllowTrailingCommas = true
        };

        var jsonSerializerOptions = new JsonSerializerOptions
        {
            AllowTrailingCommas = true
        };

        var widgetDataFields = new List<WidgetDataField>();
        using JsonDocument jsonDocument = JsonDocument.Parse(json, jsonDocumentOptions);

        foreach (JsonProperty jsonProperty in jsonDocument.RootElement.EnumerateObject())
        {
            WidgetDataField? widgetDataField = JsonSerializer.Deserialize<WidgetDataField>(jsonProperty.Value.GetRawText(), jsonSerializerOptions);

            if (widgetDataField is null || widgetDataField.Type.Equals("hidden", StringComparison.OrdinalIgnoreCase))
                continue;

            widgetDataField.Key = jsonProperty.Name;

            if (_dataValues is not null && _dataValues.TryGetValue(jsonProperty.Name, out JsonElement dataElement))
            {
                widgetDataField.Value = dataElement.ValueKind switch
                {
                    JsonValueKind.String => dataElement.GetString(),
                    JsonValueKind.Number => dataElement.GetDouble(),
                    _                    => dataElement.ToString()
                };
            }
            else if (jsonProperty.Value.TryGetProperty("value", out JsonElement defaultElement))
            {
                widgetDataField.Value = defaultElement.ValueKind switch
                {
                    JsonValueKind.String => defaultElement.GetString(),
                    JsonValueKind.Number => defaultElement.GetDouble(),
                    _                    => defaultElement.ToString()
                };
            }

            widgetDataFields.Add(widgetDataField);
            _allFields.Add(widgetDataField);
        }

        return widgetDataFields
            .GroupBy(widgetDataField => widgetDataField.Group ?? "General")
            .Select
            (
                group => new WidgetDataFieldGroup
                {
                    Name   = group.Key,
                    Fields = group.ToList()
                }
            )
            .ToList();
    }

    private string BuildDataJson()
    {
        var output = new JsonObject();

        if (_dataValues is not null)
        {
            foreach ((string key, JsonElement element) in _dataValues)
                output[key] = JsonNode.Parse(element.GetRawText());
        }

        foreach (WidgetDataField field in _allFields)
        {
            if (!_fieldControls.TryGetValue(field.Key, out FrameworkElement? control))
                continue;

            switch (control)
            {
                case TextBox textBox:
                    output[field.Key] = textBox.Text;
                    break;
                case NumericSpinner spinner:
                    output[field.Key] = spinner.Value;
                    break;
                case ColorSwatchPicker picker:
                    output[field.Key] = picker.RgbaString;
                    break;
                case StyledDropdown comboBox:
                    output[field.Key] = (comboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? string.Empty;
                    break;
                case CheckBox checkBox:
                    output[field.Key] = checkBox.IsChecked == true;
                    break;
                case SliderField sliderField:
                    output[field.Key] = sliderField.Value;
                    break;
            }
        }

        return output.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    private void ShowNoFieldsMessage(string message)
        => GroupsPanel.Children.Add
        (
            new TextBlock
            {
                Text                = message,
                Foreground          = AppColors.StatusDefault,
                FontFamily          = new FontFamily("Segoe UI"),
                FontSize            = 13,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin              = new Thickness(0, 40, 0, 0)
            }
        );

    private static Color ParseColor(string value)
    {
        try
        {
            if (value.StartsWith("rgb(") || value.StartsWith("rgba("))
            {
                int      start = value.IndexOf('(') + 1;
                string[] parts = value[start..^1].Split(',');

                if (parts.Length >= 3)
                {
                    byte r = (byte) double.Parse(parts[0].Trim());
                    byte g = (byte) double.Parse(parts[1].Trim());
                    byte b = (byte) double.Parse(parts[2].Trim());
                    byte a = parts.Length >= 4
                        ? (byte) Math.Round(double.Parse(parts[3].Trim()) * 255)
                        : (byte) 255;
                    return Color.FromArgb(a, r, g, b);
                }
            }

            return (Color) ColorConverter.ConvertFromString(value);
        }
        catch
        {
            return Colors.White;
        }
    }

    private void SetStatus(string message, bool error = false, bool success = false)
    {
        StatusText.Text       = message;
        StatusText.Foreground = error
            ? AppColors.StatusError
            : success
                ? AppColors.StatusSuccess
                : AppColors.StatusDefault;
    }

    #endregion Helpers
}