using StreamElementsToStreamerBotMigrationTool.Data;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace StreamElementsToStreamerBotMigrationTool;

public partial class WidgetConfigWindow: Window
{
    private readonly Widget                               _widget;
    private readonly Dictionary<string, FrameworkElement> _fieldControls = new ();

    public WidgetConfigWindow(Widget widget)
    {
        InitializeComponent();

        _widget = widget;
        Title   = $"Configure — {widget.Name}";

        LoadFields();
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

    private static List<WidgetDataFieldGroup> ParseFieldGroups(string json)
    {
        JsonDocument? doc    = JsonDocument.Parse(json);
        var           fields = new List<WidgetDataField>();

        foreach (JsonProperty prop in doc.RootElement.EnumerateObject())
        {
            WidgetDataField? field = JsonSerializer.Deserialize<WidgetDataField>(prop.Value.GetRawText());

            if (field is null|| field.Type.Equals("hidden", StringComparison.OrdinalIgnoreCase))
                continue;

            field.Key = prop.Name;

            if (prop.Value.TryGetProperty("value", out JsonElement jsonElement))
            {
                field.Value = jsonElement.ValueKind switch
                {
                    JsonValueKind.String
                        => jsonElement.GetString(),
                    JsonValueKind.Number
                        => jsonElement.GetDouble(),
                    _
                        => jsonElement.ToString()
                };
            }

            fields.Add(field);
        }

        return fields
            .GroupBy(file => file.Group ?? "General")
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

    private StackPanel BuildGroupPanel(WidgetDataFieldGroup group)
    {
        var container = new StackPanel();

        var header = new ToggleButton
        {
            Style   = (Style) FindResource("GroupHeaderBtn"),
            Content = new TextBlock
            {
                Text       = group.Name.ToUpper(),
                FontFamily = new FontFamily("IBM Plex Mono, Consolas"),
                FontSize   = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(0x5a, 0x62, 0x80)),
            },
            IsChecked = false
        };

        var fieldsPanel = new StackPanel
        {
            Background = new SolidColorBrush(Color.FromRgb(0x13, 0x16, 0x1f)),
            Margin     = new Thickness(0, 0, 0, 4)
        };

        foreach (WidgetDataField field in group.Fields)
        {
            Border? row = BuildFieldRow(field);
            if (row != null)
                fieldsPanel.Children.Add(row);
        }

        header.Checked   += (_, _) => fieldsPanel.Visibility = Visibility.Collapsed;
        header.Unchecked += (_, _) => fieldsPanel.Visibility = Visibility.Visible;

        container.Children.Add(header);
        container.Children.Add(fieldsPanel);
        return container;
    }

    private Border? BuildFieldRow(WidgetDataField field)
    {
        FrameworkElement? control = BuildControl(field);
        if (control is null)
            return null;

        _fieldControls[field.Key] = control;

        var row = new StackPanel
        {
            Margin = new Thickness(14, 10, 14, 10)
        };

        row.Children.Add(new TextBlock
        {
            Text  = field.Label,
            Style = (Style) FindResource("FieldLabel")
        });

        if (field.Type.Equals("colorpicker", StringComparison.OrdinalIgnoreCase))
        {
            var colorRow = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };
            colorRow.Children.Add(control);

            var hexBox = new TextBox
            {
                Style  = (Style)FindResource("FieldInput"),
                Text   = field.Value?.ToString() ?? "",
                Margin = new Thickness(8, 0, 0, 0),
                Tag    = field.Key + "__hex"
            };
            colorRow.Children.Add(hexBox);
            row.Children.Add(colorRow);
        }
        else
        {
            row.Children.Add(control);
        }

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
                    Style = (Style)FindResource("FieldInput"),
                    Text  = valueStr,
                    Tag   = field.Key
                };
                return textBox;
            }
            case "number":
            {
                var numberBox = new TextBox
                {
                    Style = (Style)FindResource("FieldInput"),
                    Text  = valueStr,
                    Tag   = field.Key
                };
                return numberBox;
            }
            case "dropdown":
            {
                if (field.Options is null)
                    return null;

                var comboBox = new ComboBox
                {
                    Style = (Style)FindResource("FieldDropdown"),
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

                return comboBox;
            }
            case "colorpicker":
            {
                var colorPicker = new Button
                {
                    Style  = (Style) FindResource("ColorSwatchBtn"),
                    Tag    = field.Key,
                    Width  = 30,
                    Height = 30
                };

                colorPicker.Background = ParseColorBrush(valueStr) ?? new SolidColorBrush(Colors.Black);
                colorPicker.Click += ColorSwatch_Click;

                return colorPicker;
            }
            case "button":
            {
                var button = new Button
                {
                    Style   = (Style)FindResource("FieldButton"),
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

    #region Event Handlers

    private void ColorSwatch_Click(object sender, RoutedEventArgs e)
    {
        // TODO: open WPF color picker dialog, update swatch background + hex box
    }

    private void FieldButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: fire test event through StreamerBot bridge
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        // TODO: read values from _fieldControls, write to data.json
        SetStatus("Saved.", success: true);
    }

    #endregion Event Handlers

    #region Helpers

    private void ShowNoFieldsMessage(string message)
    {
        GroupsPanel.Children.Add(new TextBlock
        {
            Text              = message,
            Foreground        = new SolidColorBrush(Color.FromRgb(0x5a, 0x62, 0x80)),
            FontFamily        = new FontFamily("Segoe UI"),
            FontSize          = 13,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin            = new Thickness(0, 40, 0, 0)
        });
    }

    private static SolidColorBrush? ParseColorBrush(string value)
    {
        try
        {
            if (value.StartsWith("rgba("))
            {
                var parts = value[5..^1].Split(',');
                if (parts.Length >= 3)
                {
                    byte r = (byte) double.Parse(parts[0].Trim());
                    byte g = (byte) double.Parse(parts[1].Trim());
                    byte b = (byte) double.Parse(parts[2].Trim());
                    byte a = parts.Length == 4
                        ? (byte) Math.Round(double.Parse(parts[3].Trim()) * 255)
                        : (byte) 255;

                    return new SolidColorBrush(Color.FromArgb(a, r, g, b));
                }
            }

            var color = (Color) ColorConverter.ConvertFromString(value);
            return new SolidColorBrush(color);
        }
        catch
        {
            return null;
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