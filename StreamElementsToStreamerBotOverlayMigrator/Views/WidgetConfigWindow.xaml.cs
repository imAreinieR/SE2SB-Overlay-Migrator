using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using StreamElementsToStreamerBotOverlayMigrator.Common;
using StreamElementsToStreamerBotOverlayMigrator.Common.ExtensionMethods;
using StreamElementsToStreamerBotOverlayMigrator.Controls;
using StreamElementsToStreamerBotOverlayMigrator.Data;
using StreamElementsToStreamerBotOverlayMigrator.Managers;
using StreamElementsToStreamerBotOverlayMigrator.Services;
using StreamElementsToStreamerBotOverlayMigrator.Templates;
using StreamElementsToStreamerBotOverlayMigrator.Themes;
using System.ComponentModel;
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
    private sealed class ConfigGroupUi
    {
        public StackPanel                                   Container             = null!;
        public ToggleButton                                 Header                = null!;
        public StackPanel                                   FieldsPanel           = null!;
        public List<(Border Row, string Label, string Key)> Rows                  = null!;
        public bool                                         WasCollapsedBeforeSearch;
    }

    private const    string                               SimulatedUsername       = "AmiElements";
    private const    string                               SimulatedUsernameColor  = "#7785ff";
    private const    string                               SimulatedUserId         = "123456";
    private const    string                               SimulatedGifterUsername = "CatGod";
    private const    string                               FunnyNumberAmount       = "67";

    private static readonly Color                         PreviewCanvasBackgroundColor = Colors.Black;
    private readonly Widget                               _widget;
    private readonly Dictionary<string, FrameworkElement> _fieldControls          = new ();
    private readonly List<WidgetDataField>                _allFields              = new ();
    private readonly Dictionary<string, string>           _fileNameAndContents    = new ();
    private readonly List<AssetFieldControl>              _assetFieldControls     = new ();
    private readonly List<ConfigGroupUi>                  _configGroups           = new ();
    private          Dictionary<string, JsonElement>?     _dataValues;
    private          bool                                 _isDirty;
    private          bool                                 _webViewReady           = false;
    private          bool                                 _isConfigSearchActive   = false;

    private          StyledDropdown                       _settingsCanvasSize     = null!;
    private          NumericSpinner                       _settingsWidgetWidth    = null!;
    private          NumericSpinner                       _settingsWidgetHeight   = null!;
    private          NumericSpinner                       _settingsWidgetX        = null!;
    private          NumericSpinner                       _settingsWidgetY        = null!;

    private static readonly (string Label, int Width, int Height)[] CanvasResolutions =
    [
        ("640 × 360  (360p)",     640,  360),
        ("854 × 480  (480p)",     854,  480),
        ("1280 × 720  (720p)",   1280,  720),
        ("1920 × 1080  (1080p)", 1920, 1080),
        ("2560 × 1440  (1440p)", 2560, 1440),
        ("3840 × 2160  (4K)",    3840, 2160),
    ];

    private record LogEntryEvent(LogEntry entry);
    private record LogEntry(string source, string level, string text, double timestamp, string? url, int? lineNumber);

    public WidgetConfigWindow(Widget widget)
    {
        InitializeComponent();

        _widget = widget;
        Title   = $"Configure — {widget.Name}";

        LoadConfigurationData();
        LoadConfigurationFields();
        UpdateExpandCollapseAllButtonState();

        SetupSimulateGroups();
        BuildSettingsControls();

        PreviewCanvasBackground.Fill = new SolidColorBrush(PreviewCanvasBackgroundColor);

        Loaded += WidgetConfigWindow_Loaded;
    }

    private void WidgetConfigWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= WidgetConfigWindow_Loaded;
        InitializeWebViewAsync();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        TeardownWidget();
        base.OnClosing(e);
    }

    private async void InitializeWebViewAsync()
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

    private void ReloadPreview()
    {
        if (!_webViewReady)
            return;

        _fileNameAndContents.Clear();

        string dataJson = BuildDataJson();

        _fileNameAndContents["sessionData.js"] = TemplateFiles.SessionDataFile;
        _fileNameAndContents["streamerBotApiAndEventBridge.js"] = DummyWidgetOnLoadEvent()
            + Environment.NewLine
            + TemplateFiles.ApiAndEventBridgeFile;

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

        var rows = new List<(Border Row, string Label, string Key)>();

        foreach (WidgetDataField widgetDataField in widgetDataFieldGroup.Fields)
        {
            Border? row = BuildFieldRow(widgetDataField);

            if (row is not null)
            {
                fieldsPanel.Children.Add(row);
                rows.Add((row, widgetDataField.Label, widgetDataField.Key));
            }
        }

        header.Checked   += (_, _) => { fieldsPanel.Visibility = Visibility.Collapsed; UpdateExpandCollapseAllButtonState(); };
        header.Unchecked += (_, _) => { fieldsPanel.Visibility = Visibility.Visible;   UpdateExpandCollapseAllButtonState(); };

        container.Children.Add(header);
        container.Children.Add(fieldsPanel);

        _configGroups.Add(new ConfigGroupUi
        {
            Container   = container,
            Header      = header,
            FieldsPanel = fieldsPanel,
            Rows        = rows
        });

        return container;
    }

    private void ExpandCollapseAllBtn_Click(object sender, RoutedEventArgs e)
    {
        if (!_configGroups.Any())
            return;

        bool shouldCollapse = _configGroups.Any(group => group.Header.IsChecked != true);

        foreach (ConfigGroupUi group in _configGroups)
            group.Header.IsChecked = shouldCollapse;

        UpdateExpandCollapseAllButtonState();
    }

    private void UpdateExpandCollapseAllButtonState()
    {
        if (!_configGroups.Any())
            return;

        bool allCollapsed = _configGroups.All(group => group.Header.IsChecked == true);

        ExpandCollapseAllBtn.Content = allCollapsed ? "▸" : "▾";
        ExpandCollapseAllBtn.ToolTip = allCollapsed ? "Expand all" : "Collapse all";
    }

    private void ConfigSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        string query = ConfigSearchBox.Text.Trim();

        ConfigSearchPlaceholder.Visibility = query.Length == 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        ApplyConfigSearchFilter(query);
    }

    private void ApplyConfigSearchFilter(string query)
    {
        bool isSearching = query.Length > 0;

        if (isSearching && !_isConfigSearchActive)
        {
            foreach (ConfigGroupUi group in _configGroups)
                group.WasCollapsedBeforeSearch = group.Header.IsChecked == true;
        }

        foreach (ConfigGroupUi group in _configGroups)
        {
            bool groupHasMatch = false;

            foreach ((Border row, string label, string key) in group.Rows)
            {
                bool matches = !isSearching
                    || label.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || GetFieldValueText(key).Contains(query, StringComparison.OrdinalIgnoreCase);

                row.Visibility = matches ? Visibility.Visible : Visibility.Collapsed;

                if (matches)
                    groupHasMatch = true;
            }

            group.Container.Visibility = !isSearching || groupHasMatch
                ? Visibility.Visible
                : Visibility.Collapsed;

            if (isSearching)
            {
                if (groupHasMatch && group.Header.IsChecked == true)
                    group.Header.IsChecked = false;
            }
            else if (_isConfigSearchActive)
            {
                group.Header.IsChecked = group.WasCollapsedBeforeSearch;
            }
        }

        _isConfigSearchActive = isSearching;
        UpdateExpandCollapseAllButtonState();
    }

    private string GetFieldValueText(string key)
    {
        if (!_fieldControls.TryGetValue(key, out FrameworkElement? control))
            return string.Empty;

        return control switch
        {
            TextBox           textBox     => textBox.Text,
            NumericSpinner    spinner     => spinner.Value.ToString(),
            ColorSwatchPicker picker      => picker.RgbaString,
            StyledDropdown    comboBox    => (comboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty,
            CheckBox          checkBox    => checkBox.IsChecked == true ? "true" : "false",
            SliderField       sliderField => sliderField.Value.ToString(),
            _                             => string.Empty
        };
    }

    private Border? BuildFieldRow(WidgetDataField widgetDataField)
    {
        FrameworkElement? control = BuildControl(widgetDataField);

        if (control is null)
            return null;

        _fieldControls[widgetDataField.Key] = control is AssetFieldControl assetFieldControl
            ? assetFieldControl.Dropdown
            : control;

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

        try
        {
            return field.Type.ToLowerInvariant() switch
            {
                "text"        => BuildTextControl       (field, valueStr),
                "checkbox"    => BuildCheckboxControl   (field, valueStr),
                "colorpicker" => BuildColorPickerControl(field, valueStr),
                "number"      => BuildNumberControl     (field, valueStr),
                "slider"      => BuildSliderControl     (field, valueStr),
                "dropdown"    => BuildDropdownControl   (field, valueStr),
                "image-input" => BuildImageInputControl (field, valueStr),
                "video-input" => BuildVideoInputControl (field, valueStr),
                "sound-input" => BuildAudioInputControl (field, valueStr),
                "googlefont"  => BuildGoogleFontControl (field, valueStr),
                "button"      => BuildButtonControl     (field),
                _             => BuildTextControl       (field, valueStr)
            };
        }
        catch (Exception exception)
        {
            SetStatus($"[ERROR] Failed to build control for '{field.Key}': {exception.Message}", error: true);
            return BuildUnsupportedControl(field, "failed to build");
        }
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

    private void SetupSimulateGroups()
    {
        (ToggleButton header, StackPanel fields)[] groups =
        [
            (SimGroupChat,    SimGroupChatFields),
            (SimGroupFollows, SimGroupFollowsFields),
            (SimGroupSubs,    SimGroupSubsFields),
            (SimGroupRaids,   SimGroupRaidsFields),
        ];

        foreach ((ToggleButton header, StackPanel fields) in groups)
        {
            header.Checked   += (_, _) => fields.Visibility = Visibility.Collapsed;
            header.Unchecked += (_, _) => fields.Visibility = Visibility.Visible;
        }
    }

    private void BuildSettingsControls()
    {
        _settingsCanvasSize = new StyledDropdown();

        foreach ((string label, int width, int height) in CanvasResolutions)
            _settingsCanvasSize.Items.Add
            (
                new ComboBoxItem
                {
                    Content = label, Tag = $"{width}x{height}"
                }
            );

        _settingsCanvasSize.SelectedIndex     = 3;
        _settingsCanvasSize.SelectionChanged += SettingsCanvasSize_Changed;
        SettingsCanvasSizeRow.Child           = MakeSettingsRow("Canvas Size", _settingsCanvasSize);

        _settingsWidgetWidth                  = new NumericSpinner { Value = 800, Minimum = 1 };
        _settingsWidgetWidth.ValueChanged    += SettingsWidget_Changed;
        SettingsWidgetWidthRow.Child          = MakeSettingsRow("Widget Width", _settingsWidgetWidth);

        _settingsWidgetHeight                 = new NumericSpinner { Value = 600, Minimum = 1 };
        _settingsWidgetHeight.ValueChanged   += SettingsWidget_Changed;
        SettingsWidgetHeightRow.Child         = MakeSettingsRow("Widget Height", _settingsWidgetHeight);

        _settingsWidgetX                      = new NumericSpinner { Value = 0 };
        _settingsWidgetX.ValueChanged        += SettingsWidget_Changed;
        SettingsWidgetXRow.Child              = MakeSettingsRow("Widget X Position", _settingsWidgetX);

        _settingsWidgetY                      = new NumericSpinner { Value = 0 };
        _settingsWidgetY.ValueChanged        += SettingsWidget_Changed;
        SettingsWidgetYRow.Child              = MakeSettingsRow("Widget Y Position", _settingsWidgetY);

        (ToggleButton header, StackPanel fields)[] groups =
        [
            (SettingsGroupCanvas, SettingsGroupCanvasFields),
            (SettingsGroupWidget, SettingsGroupWidgetFields),
        ];

        foreach ((ToggleButton header, StackPanel fields) in groups)
        {
            header.Checked   += (_, _) => fields.Visibility = Visibility.Collapsed;
            header.Unchecked += (_, _) => fields.Visibility = Visibility.Visible;
        }
    }

    private StackPanel MakeSettingsRow(string label, FrameworkElement control)
        => new StackPanel
        {
            Children =
            {
                new TextBlock
                {
                    Text  = label,
                    Style = (Style) FindResource("FieldLabel")
                },
                control
            }
        };

    private TextBox BuildTextControl(WidgetDataField field, string valueStr)
    {
        var textBox = new TextBox
        {
            Style = (Style) FindResource("FieldInput"),
            Text  = valueStr,
            Tag   = field.Key
        };
        textBox.TextChanged += OnControlChanged;

        return textBox;
    }

    private CheckBox BuildCheckboxControl(WidgetDataField field, string valueStr)
    {
        bool isChecked = valueStr.Equals("true", StringComparison.OrdinalIgnoreCase)
            || valueStr == "1";

        var checkBox = new CheckBox
        {
            Style     = (Style) FindResource("FieldToggle"),
            IsChecked = isChecked,
            Tag       = field.Key
        };
        checkBox.Checked += OnControlChanged;
        checkBox.Unchecked += OnControlChanged;

        return checkBox;
    }

    private ColorSwatchPicker BuildColorPickerControl(WidgetDataField field, string valueStr)
    {
        var picker = new ColorSwatchPicker
        {
            Color = ParseColor(valueStr),
            Tag   = field.Key
        };
        picker.ColorChanged += OnControlChanged;

        return picker;
    }

    private NumericSpinner BuildNumberControl(WidgetDataField field, string valueStr)
    {
        var spinner = new NumericSpinner
        {
            Value = double.TryParse(valueStr, out double initialNumber)
                ? initialNumber
                : 1,
            Tag   = field.Key
        };
        spinner.ValueChanged += OnControlChanged;

        return spinner;
    }

    private SliderField BuildSliderControl(WidgetDataField field, string valueStr)
    {
        if (field.Min is double mn && field.Max is double mx && mn > mx)
            throw new InvalidOperationException($"Field '{field.Key}': Min ({mn}) > Max ({mx}).");

        var sliderField = new SliderField
        {
            Tag = field.Key
        };

        sliderField.SetRange(field.Min ?? 0, field.Max ?? 100, field.Step ?? 1);
        sliderField.Value = double.TryParse(valueStr, out double initialSlider)
            ? initialSlider
            : field.Min ?? 0;
        sliderField.ValueChanged += OnControlChanged;

        return sliderField;
    }

    private StyledDropdown BuildDropdownControl(WidgetDataField field, string valueStr)
    {
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
                        Tag     = key
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
    }

    private FrameworkElement? BuildImageInputControl(WidgetDataField field, string valueStr)
        => BuildAssetInputControl(field, valueStr, WidgetFileType.ImageAsset);

    private FrameworkElement? BuildAudioInputControl(WidgetDataField field, string valueStr)
        => BuildAssetInputControl(field, valueStr, WidgetFileType.AudioAsset);

    private FrameworkElement? BuildVideoInputControl(WidgetDataField field, string valueStr)
        => BuildAssetInputControl(field, valueStr, WidgetFileType.VideoAsset);

    private FrameworkElement? BuildAssetInputControl(WidgetDataField field, string valueStr, WidgetFileType assetWidgetFileType)
    {
        if (!assetWidgetFileType.IsAssetFile())
            return BuildUnsupportedControl(field);

        var comboBox = new StyledDropdown
        {
            Tag = field.Key
        };

        PopulateAssetDropdown(comboBox, assetWidgetFileType, valueStr);

        var setButton = new Button
        {
            Style   = (Style) FindResource("FieldButton"),
            Content = assetWidgetFileType switch
            {
                WidgetFileType.ImageAsset => "Set Image",
                WidgetFileType.AudioAsset => "Set Audio",
                WidgetFileType.VideoAsset => "Set Video",
                _ => "Set File"
            }
        };

        var assetFieldControl = new AssetFieldControl(comboBox, setButton, assetWidgetFileType);
        _assetFieldControls.Add(assetFieldControl);

        assetFieldControl.PlaybackError += (_, message) => SetStatus($"[ERROR] Couldn't play asset: {message}", error: true);
        setButton.Click                 += (_, _)       => ImportAssetFile(assetFieldControl, assetWidgetFileType, field);
        comboBox.SelectionChanged       += (_, _)       => UpdateAssetSelection(assetFieldControl, assetWidgetFileType);
        comboBox.SelectionChanged       += OnControlChanged;

        UpdateAssetSelection(assetFieldControl, assetWidgetFileType);

        return assetFieldControl;
    }

    private void PopulateAssetDropdown(StyledDropdown comboBox, WidgetFileType assetWidgetFileType, string? selectTag)
    {
        comboBox.Items.Clear();

        IEnumerable<string> assets = _widget
            .Files
            .Where(file => file.WidgetFileType == assetWidgetFileType)
            .Select(file => file.FileName);

        foreach (string fileName in assets)
            comboBox.Items.Add
            (
                new ComboBoxItem
                {
                    Content = fileName,
                    Tag     = $"{assetWidgetFileType.GetSubFolderForWidgetFileType()}/{fileName}"
                }
            );

        if (string.IsNullOrEmpty(selectTag))
            return;

        foreach (ComboBoxItem item in comboBox.Items)
        {
            if (item.Tag?.ToString() == selectTag)
            {
                comboBox.SelectedItem = item;
                break;
            }
        }
    }

    private void UpdateAssetSelection(AssetFieldControl assetFieldControl, WidgetFileType assetWidgetFileType)
    {
        string? fileName = (assetFieldControl.Dropdown.SelectedItem as ComboBoxItem)?.Content?.ToString();

        WidgetFile? widgetFile = string.IsNullOrEmpty(fileName)
            ? null
            : _widget
                .Files
                .FirstOrDefault(file => file.WidgetFileType == assetWidgetFileType && file.FileName == fileName);

        assetFieldControl.LoadAsset(widgetFile);
    }

    private void ImportAssetFile(AssetFieldControl assetFieldControl, WidgetFileType assetWidgetFileType, WidgetDataField field)
    {
        var dialog = new OpenFileDialog
        {
            Title  = $"Select {field.Label}",
            Filter = assetWidgetFileType switch
            {
                WidgetFileType.ImageAsset => SupportedFileTypes.BuildFileDialogFilter("Image files", SupportedFileTypes.ImageExtensions),
                WidgetFileType.AudioAsset => SupportedFileTypes.BuildFileDialogFilter("Audio files", SupportedFileTypes.AudioExtensions),
                WidgetFileType.VideoAsset => SupportedFileTypes.BuildFileDialogFilter("Video files", SupportedFileTypes.VideoExtensions),
                _                         => "All files (*.*)|*.*"
            }
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            WidgetFileImportAndExportService.AddWidgetFilesToFromPaths
            (
                _widget,
                new[] { dialog.FileName }
            );

            string fileName = Path.GetFileName(dialog.FileName);
            string newTag = $"{assetWidgetFileType.GetSubFolderForWidgetFileType()}/{fileName}";

            PopulateAssetDropdown(assetFieldControl.Dropdown, assetWidgetFileType, newTag);

            SetStatus($"[INFO] Imported '{fileName}'.", success: true);
        }
        catch (Exception exception)
        {
            SetStatus($"[ERROR] Failed to import asset: {exception.Message}", error: true);
        }
    }

    private StyledDropdown BuildGoogleFontControl(WidgetDataField field, string valueStr)
    {
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
    }

    private Button BuildButtonControl(WidgetDataField field)
    {
        var button = new Button
        {
            Style   = (Style) FindResource("FieldButton"),
            Content = field.Label,
            Tag     = field.Key
        };
        button.Click += FieldButton_Click;
        return button;
    }

    private TextBlock BuildUnsupportedControl(WidgetDataField field, string reason = "is not yet supported in the config UI")
        => new TextBlock
        {
            Text       = $"'{field.Type}' {reason}.",
            Style      = (Style) FindResource("FieldLabel"),
            FontStyle  = FontStyles.Italic,
            Foreground = AppColors.StatusError
        };

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

    private void LeftTabConfig_Checked(object sender, RoutedEventArgs e)
    {
        if (LeftTabSimulate is null || LeftTabSettings is null)
            return;

        LeftTabSimulate.IsChecked    = false;
        LeftTabSettings.IsChecked    = false;
        LeftPanelConfig.Visibility   = Visibility.Visible;
        LeftPanelSimulate.Visibility = Visibility.Collapsed;
        LeftPanelSettings.Visibility = Visibility.Collapsed;
    }

    private void LeftTabSimulate_Checked(object sender, RoutedEventArgs e)
    {
        if (LeftTabConfig is null || LeftTabSettings is null)
            return;

        LeftTabConfig.IsChecked      = false;
        LeftTabSettings.IsChecked    = false;
        LeftPanelConfig.Visibility   = Visibility.Collapsed;
        LeftPanelSimulate.Visibility = Visibility.Visible;
        LeftPanelSettings.Visibility = Visibility.Collapsed;
    }

    private void LeftTabSettings_Checked(object sender, RoutedEventArgs e)
    {
        if (LeftTabConfig is null || LeftTabSimulate is null)
            return;

        LeftTabConfig.IsChecked      = false;
        LeftTabSimulate.IsChecked    = false;
        LeftPanelConfig.Visibility   = Visibility.Collapsed;
        LeftPanelSimulate.Visibility = Visibility.Collapsed;
        LeftPanelSettings.Visibility = Visibility.Visible;
    }

    private void SettingsCanvasSize_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_settingsCanvasSize.SelectedItem is not ComboBoxItem item)
            return;

        string tag = item.Tag?.ToString() ?? string.Empty;
        string[] parts = tag.Split('x');

        if (parts.Length != 2)
            return;

        if (!int.TryParse(parts[0], out int width) || !int.TryParse(parts[1], out int height))
            return;

        PreviewCanvas.Width  = width;
        PreviewCanvas.Height = height;
    }

    private void SettingsWidget_Changed(object? sender, EventArgs e)
    {
        double width  = _settingsWidgetWidth?.Value  ?? 800;
        double height = _settingsWidgetHeight?.Value ?? 600;
        double x      = _settingsWidgetX?.Value      ?? 0;
        double y      = _settingsWidgetY?.Value      ?? 0;

        // TODO: apply widget size and position to the preview WebView
    }

    #endregion Event Handlers

    #region WebView2 EventHandlers

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

    private void OnWebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
        var    uri               = new Uri(e.Request.Uri);
        string filePath          = Path.Combine("wwwroot", uri.LocalPath.TrimStart('/'));
        string requestedFileName = Path.GetFileName(filePath);

        if (!_fileNameAndContents.TryGetValue(requestedFileName, out string? content))
            return;

        byte[] bytes = SupportedFileTypes.IsTextBasedExtension(requestedFileName)
            ? Encoding.UTF8.GetBytes(content)
            : Convert.FromBase64String(content);

        // NOTE: Do NOT dispose the stream here — WebView2 reads it asynchronously after this handler returns.
        var stream = new MemoryStream(bytes);
        e.Response = PreviewWebView.CoreWebView2.Environment.CreateWebResourceResponse
        (
            stream,
            200, "OK",
            $"Content-Type: {GetMimeType(filePath)}"
        );
    }

    #endregion WebView2 EventHandlers

    #region SE Event Simulation Event Handlers

    private async void SimulateChatMessage_Click(object sender, RoutedEventArgs e)
    {
        await DispatchWidgetEvent
        (
            "message",
            new
            {
                data = new
                {
                    time         = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    tags         = new
                    {
                        badges      = Array.Empty<object>(),
                        color       = SimulatedUsernameColor,
                        displayName = SimulatedUsername,
                        emotes      = Array.Empty<object>(),
                        flags       = string.Empty,
                        id          = Guid.NewGuid().ToString(),
                        mod         = "0",
                        roomId      = string.Empty,
                        subscriber  = "0",
                        tmiSentTs   = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                        turbo       = "0",
                        userId      = SimulatedUserId,
                        userType    = string.Empty
                    },
                    nick         = SimulatedUsername,
                    userId       = SimulatedUserId,
                    displayName  = SimulatedUsername,
                    displayColor = SimulatedUsernameColor,
                    badges       = Array.Empty<object>(),
                    channel      = SimulatedUsername,
                    text         = "Hello World~",
                    isAction     = false,
                    emotes       = Array.Empty<object>(),
                    msgId        = Guid.NewGuid().ToString(),
                }
            }
        );
    }

    private async void SimulateFollow_Click(object sender, RoutedEventArgs e)
    {
        await DispatchWidgetEvent
        (
            "follower-latest",
            new
            {
                avatar      = JavascriptFunctionCallForAvatarUrl(SimulatedUsername),
                displayName = SimulatedUsername,
                username    = SimulatedUsername,
                name        = SimulatedUsername,
                providerId  = SimulatedUserId,
            }
        );
    }

    private async void SimulateSub_Click(object sender, RoutedEventArgs e)
    {
        await DispatchWidgetEvent
        (
            "subscriber-latest",
            new
            {
                amount      = 1,
                avatar      = JavascriptFunctionCallForAvatarUrl(SimulatedUsername),
                displayName = SimulatedUsername,
                username    = SimulatedUsername,
                name        = SimulatedUsername,
                providerId  = SimulatedUserId,
                tier        = "1000",
                gifted      = false,
                message     = "Much Sub, Such Wow",
            }
        );
    }

    private async void SimulateReSub_Click(object sender, RoutedEventArgs e)
    {
        await DispatchWidgetEvent
        (
            "subscriber-latest",
            new
            {
                amount      = FunnyNumberAmount,
                avatar      = JavascriptFunctionCallForAvatarUrl(SimulatedUsername),
                displayName = SimulatedUsername,
                username    = SimulatedUsername,
                name        = SimulatedUsername,
                providerId  = SimulatedUserId,
                tier        = "1000",
                gifted      = false,
                message     = "Much Sub, Such Wow",
            }
         );
    }

    private async void SimulateGiftedSub_Click(object sender, RoutedEventArgs e)
    {
        await DispatchWidgetEvent
        (
            "subscriber-latest",
            new
            {
                amount                = 1,
                avatar                = JavascriptFunctionCallForAvatarUrl(SimulatedUsername),
                displayName           = SimulatedUsername,
                username              = SimulatedUsername,
                name                  = SimulatedUsername,
                providerId            = SimulatedUserId,
                tier                  = "1000",
                sender                = SimulatedGifterUsername,
                gifted                = true,
                message               = "Much Sub, Such Wow",
                bulkGifted            = false,
                isCommunityGift       = true,
                playedAsCommunityGift = false
            }
        );
    }

    private async void SimulateGiftedSubs_Click(object sender, RoutedEventArgs e)
    {
        await DispatchWidgetEvent
        (
            "subscriber-latest",
            new
            {
                amount                = FunnyNumberAmount,
                avatar                = JavascriptFunctionCallForAvatarUrl(SimulatedUsername),
                displayName           = SimulatedUsername,
                username              = SimulatedUsername,
                name                  = SimulatedUsername,
                providerId            = SimulatedUserId,
                tier                  = "1000",
                sender                = SimulatedGifterUsername,
                gifted                = true,
                message               = "Much Sub, Such Wow",
                bulkGifted            = true,
                isCommunityGift       = true,
                playedAsCommunityGift = true
            }
        );
    }

    private async void SimulateBits_Click(object sender, RoutedEventArgs e)
    {
        await DispatchWidgetEvent
        (
            "cheer-latest",
            new
            {
                amount      = FunnyNumberAmount,
                avatar      = JavascriptFunctionCallForAvatarUrl(SimulatedUsername),
                displayName = SimulatedUsername,
                username    = SimulatedUsername,
                name        = SimulatedUsername,
                providerId  = SimulatedUserId,
                message     = "Cheer100 Nice stream!",
            }
        );
    }

    private async void SimulateChannelPoints_Click(object sender, RoutedEventArgs e)
    {
        DateTime now = DateTime.UtcNow;

        await DispatchWidgetEvent
        (
            "event",
            new
            {
                type               = "channelPointsRedemption",
                provider           = "twitch",
                channel            = SimulatedUserId,
                flagged            = false,
                createdAt          = DateTimeForJavascriptCode(now),
                data               = new
                {
                    amount      = 500,
                    username    = SimulatedUsername,
                    displayName = SimulatedUsername,
                    providerId  = SimulatedUserId,
                    redemption  = "Headpats",
                    quantity    = 0,
                    avatar      = JavascriptFunctionCallForAvatarUrl(SimulatedUsername),
                },
                _id                = GenerateGuidForJavascriptCode(),
                expiresAt          = DateTimeForJavascriptCode(now.AddDays(28)),
                updatedAt          = DateTimeForJavascriptCode(now),
                activityId         = GenerateGuidForJavascriptCode(),
                sessionEventsCount = 1,
            }
        );
    }

    private async void SimulateRaid_Click(object sender, RoutedEventArgs e)
    {
        await DispatchWidgetEvent
        (
            "raid-latest",
            new
            {
                amount      = FunnyNumberAmount,
                avatar      = JavascriptFunctionCallForAvatarUrl(SimulatedUsername),
                displayName = SimulatedUsername,
                username    = SimulatedUsername,
                name        = SimulatedUsername,
                providerId  = SimulatedUserId,
            }
        );
    }

    private async Task DispatchWidgetEvent(string listener, object payload)
    {
        if (!_webViewReady)
            return;

        try
        {
            string json = JsonSerializer.Serialize(payload);

            await PreviewWebView.ExecuteScriptAsync
            (
                $@"window.dispatchEvent(new CustomEvent('onEventReceived', {{
                    detail: {{
                    listener: '{listener}',
                    event: {json}
                  }}
                }}));"
            );
        }
        catch (Exception exception)
        {
            SetStatus($"Simulate failed: {exception.Message}", error: true);
        }
    }

    private void TeardownWidget()
    {
        foreach (AssetFieldControl assetFieldControl in _assetFieldControls)
        {
            try
            {
                assetFieldControl.Dispose();
            }
            catch
            {}
        }

        _assetFieldControls.Clear();

        if (!_webViewReady)
            return;

        PreviewWebView.Source = new Uri("about:blank");
        PreviewWebView.Dispose();
    }

    #endregion SE Event Simulation Event Handlers

    #region Helpers

    public void AppendLog(string message)
    {
        string line = $"[{DateTime.Now:HH:mm:ss}] {message}";

        if (LogsText.Text == "No log entries yet.")
            LogsText.Text = line;
        else
            LogsText.Text += $"\n{line}";

        LogsScrollViewer.ScrollToEnd();
    }

    private void LoadConfigurationData()
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

    private void LoadConfigurationFields()
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
            groups = ParseConfigurationFieldGroups(fieldsFile.Content);
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

    private List<WidgetDataFieldGroup> ParseConfigurationFieldGroups(string json)
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

    private static string InjectBaseUrlProxy(string html)
    {
        string injectedScript = $"<base href=\"https://app.local/\" />\n";

        int index = html.IndexOf("<head>", StringComparison.OrdinalIgnoreCase);
        return index >= 0
            ? html.Insert(index, injectedScript)
            : injectedScript + html;
    }

    private static string GetMimeType(string path)
        => Path.GetExtension(path).ToLowerInvariant() switch
        {
            // Text / web
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".mjs" => "application/javascript",
            ".html" => "text/html",
            ".htm" => "text/html",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".txt" => "text/plain",
            ".wasm" => "application/wasm",

            // Images
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".ico" => "image/x-icon",
            ".avif" => "image/avif",
            ".tif" => "image/tiff",
            ".tiff" => "image/tiff",

            // Video
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".ogv" => "video/ogg",
            ".mov" => "video/quicktime",
            ".avi" => "video/x-msvideo",
            ".mkv" => "video/x-matroska",

            // Audio
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".ogg" => "audio/ogg",
            ".oga" => "audio/ogg",
            ".m4a" => "audio/mp4",
            ".aac" => "audio/aac",
            ".flac" => "audio/flac",
            ".weba" => "audio/webm",

            // Fonts (common in wwwroot bundles, cheap to include)
            ".woff" => "font/woff",
            ".woff2" => "font/woff2",
            ".ttf" => "font/ttf",
            ".otf" => "font/otf",

            _ => "application/octet-stream"
        };

    private void SetStatus(string message, bool error = false, bool success = false)
    {
        StatusText.Text       = message;
        StatusText.Foreground = error
            ? AppColors.StatusError
            : success
                ? AppColors.StatusSuccess
                : AppColors.StatusDefault;
    }

    private string GenerateGuidForJavascriptCode()
        => Guid.NewGuid().ToString("N");

    private string DateTimeForJavascriptCode(DateTime datetime)
        => datetime.ToUniversalTime().ToString("o");

    private string JavascriptFunctionCallForAvatarUrl(string username)
        => $"fetchAvatarUrl('{username}')";

    private string DummyWidgetOnLoadEvent()
        => $@"const dummySeEvent = new CustomEvent('onWidgetLoad', {{
               detail: {{
                   session:  {{}},
                   recents:  {{}},
                   currency: {{}},
                   channel:
                   {{
                       username:   '{SimulatedUsername}',
                       apiToken:   '',
                       id:         '',
                       providerId: '{SimulatedUserId}',
                       avatar:     {JavascriptFunctionCallForAvatarUrl(SimulatedUsername)},
                   }},
                   fieldData: {BuildDataJson()},
                   overlay:
                   {{
                       isEditorMode: false,
                       muted:        false,
                   }}
               }}
           }});
           console.log('[LIVE-PREVIEW] Dispatching dummy onWidgetLoad event...');
           window.dispatchEvent(dummySeEvent);";

    #endregion Helpers
}