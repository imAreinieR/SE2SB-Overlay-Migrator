using StreamElementsToStreamerBotOverlayMigrator.Controls;
using StreamElementsToStreamerBotOverlayMigrator.Data;
using StreamElementsToStreamerBotOverlayMigrator.Managers;
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
    private readonly Dictionary<string, FrameworkElement> _fieldControls = new ();
    private readonly List<WidgetDataField>                _allFields     = new ();
    private          Dictionary<string, JsonElement>?     _dataValues;
    private          bool                                 _isDirty;

    public WidgetConfigWindow(Widget widget)
    {
        InitializeComponent();

        _widget = widget;
        Title   = $"Configure — {widget.Name}";

        LoadDataJson();
        LoadFields();
    }

    private void LoadDataJson()
    {
        WidgetFile? dataFile = _widget
            .Files
            .FirstOrDefault(file => file.FileName.Equals("data.json", StringComparison.OrdinalIgnoreCase));

        if (dataFile is null)
            return;

        try
        {
            using JsonDocument jsonDocument = JsonDocument.Parse(dataFile.Content);

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
            .FirstOrDefault(file => file.FileName.Equals("fields.json", StringComparison.OrdinalIgnoreCase));

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
        JsonDocument? jsonDocument     = JsonDocument.Parse(json);
        var           widgetDataFields = new List<WidgetDataField>();

        foreach (JsonProperty jsonProperty in jsonDocument.RootElement.EnumerateObject())
        {
            WidgetDataField? widgetDataField = JsonSerializer.Deserialize<WidgetDataField>(jsonProperty.Value.GetRawText());

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
                Foreground = new SolidColorBrush(Color.FromRgb(0x5a, 0x62, 0x80))
            },
            IsChecked = false
        };

        var fieldsPanel = new StackPanel
        {
            Background = new SolidColorBrush(Color.FromRgb(0x13, 0x16, 0x1f)),
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
            BorderBrush     = new SolidColorBrush(Color.FromRgb(0x25, 0x2c, 0x40)),
            BorderThickness = new Thickness(0, 0, 0, 1)
        };
    }

    private FrameworkElement? BuildControl(WidgetDataField field)
    {
        string valueStr = field.Value?.ToString() ?? string.Empty;

        switch (field.Type.ToLowerInvariant())
        {
            case "text":
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
            case "number":
            {
                double.TryParse(valueStr, out double initial);

                var spinner = new NumericSpinner
                {
                    Value = initial,
                    Tag   = field.Key
                };
                spinner.ValueChanged += OnControlChanged;
                return spinner;
            }
            case "dropdown":
            {
                if (field.Options is null)
                    return null;

                var comboBox = new ComboBox
                {
                    Style = (Style) FindResource("FieldDropdown"),
                    Tag   = field.Key
                };

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
                return comboBox;
            }
            case "colorpicker":
            {
                Color initial = ParseColor(valueStr);

                var picker = new ColorSwatchPicker
                {
                    Color = initial,
                    Tag   = field.Key
                };
                picker.ColorChanged += OnControlChanged;
                return picker;
            }
            case "button":
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
            default:
                return null;
        }
    }

    private void OnControlChanged(object sender, EventArgs e)
    {
        if (_isDirty)
            return;

        _isDirty = true;

        if (SaveBtn is not null)
            SaveBtn.IsEnabled = true;

        SetStatus("Unsaved changes.");
    }

    #region Event Handlers

    private void FieldButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: fire test event through StreamerBot bridge in Live Preview
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
                _widget.Files.Add(new WidgetFile("data.json", json));

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
                case ComboBox comboBox:
                    output[field.Key] = (comboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? string.Empty;
                    break;
            }
        }

        return output.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    #endregion Event Handlers

    #region Helpers

    private void ShowNoFieldsMessage(string message)
        => GroupsPanel.Children.Add
        (
            new TextBlock
            {
                Text = message,
                Foreground = new SolidColorBrush(Color.FromRgb(0x5a, 0x62, 0x80)),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 13,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 40, 0, 0)
            }
        );

    private static Color ParseColor(string value)
    {
        try
        {
            if (value.StartsWith("rgb(") || value.StartsWith("rgba("))
            {
                int       start = value.IndexOf('(') + 1;
                string[]? parts = value[start..^1].Split(',');

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

            return (Color)ColorConverter.ConvertFromString(value);
        }
        catch
        {
            return Colors.White;
        }
    }

    private void SetStatus(string message, bool error = false, bool success = false)
    {
        StatusText.Text       = message;
        StatusText.Foreground = new SolidColorBrush
        (
            error
                ? Color.FromRgb(0xFF, 0x6B, 0x6B)
                : success
                    ? Color.FromRgb(0x3E, 0xCF, 0x8E)
                    : Color.FromRgb(0x58, 0x60, 0x80)
        );
    }

    #endregion Helpers
}