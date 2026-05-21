using Microsoft.Win32;
using StreamElementsToStreamerBotMigrationTool.Common;
using StreamElementsToStreamerBotMigrationTool.Data;
using StreamElementsToStreamerBotMigrationTool.Managers;
using StreamElementsToStreamerBotMigrationTool.Templates;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StreamElementsToStreamerBotMigrationTool;

public partial class MainWindow: Window
{
    private readonly List<string>                 _relevantFileExtensions = new() { ".html", ".js", ".css", ".json" };
    private readonly ObservableCollection<Widget> _widgets;

    private Widget? _selectedWidget;

    private static readonly string DeployPath = Path.Combine
    (
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "imA-SB-Widgets"
    );

    public MainWindow()
    {
        InitializeComponent();

        WidgetList.ItemsSource = _widgets = new ObservableCollection<Widget>(WidgetManager.GetAll());
    }

    #region Event Handlers

    private void NewWidget_Click(object sender, RoutedEventArgs e)
    {
        string name = $"Widget {_widgets.Count + 1}";
        var widget = new Widget(name, DeployPath);

        _widgets.Add(widget);
        SelectWidget(widget);
    }

    private void RemoveWidget_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not Widget widget)
            return;

        _widgets.Remove(widget);
        WidgetManager.Delete(widget);

        if (_selectedWidget == widget)
        {
            if (_widgets.Any())
            {
                SelectWidget(_widgets.First());
            }
            else
            {
                _selectedWidget      = null;
                FileList.ItemsSource = null;
                ClearDetail();
            }
        }
    }

    private void SaveWidget_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedWidget is null)
            return;

        WidgetManager.Save(_selectedWidget);
        SetStatus("Widget saved.");
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedWidget is null)
            return;

        var dialog = new OpenFileDialog
        {
            Title       = "Select widget files",
            Multiselect = true,
            Filter      = "Widget files (*.html;*.js;*.css;*.json)|*.html;*.js;*.css;*.json|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
            return;

        foreach (string path in dialog.FileNames)
        {
            string? name = Path.GetFileName(path);

            if (_selectedWidget.Files.Any(file => file.FileName == name))
                continue;

            _selectedWidget.Files.Add(new WidgetFile(name, File.ReadAllText(path)));
        }

        OnFilesChanged();
    }

    private void RemoveFile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not WidgetFile file || _selectedWidget is null)
            return;

        _selectedWidget.Files.Remove(file);
    }

    private void CopyPath_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(DeployPathBox.Text))
            return;

        Clipboard.SetText(DeployPathBox.Text);
        SetStatus("Path copied to clipboard.");
    }

    private void Generate_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedWidget is null)
            return;

        try
        {
            Directory.CreateDirectory(DeployPath);

            string jsonData = _selectedWidget.Files.FirstOrDefault(file => file.WidgetFileType == WidgetFileType.DataJson)?.Content ?? string.Empty;

            foreach (WidgetFile file in _selectedWidget.Files)
            {
                string fileName = file.WidgetFileType switch
                {
                    WidgetFileType.Html
                        => "index.html",
                    WidgetFileType.Javascript
                        => "index.js",
                    WidgetFileType.Css
                        => "index.css",
                    WidgetFileType.DataJson
                        => "config.js",
                    _
                        => file.FileName
                };

                string destination = Path.Combine(DeployPath, fileName);
                string content = file.WidgetFileType == WidgetFileType.Html || file.WidgetFileType == WidgetFileType.Css
                    ? SearchAndReplaceDataVariables(file.Content, jsonData)
                    : file.Content;

                if (file.WidgetFileType == WidgetFileType.Html)
                {
                    content = string.Format(TemplateFiles.HtmlTemplate, FixProtocolRelativeUrls(file.Content));
                }
                else if (file.WidgetFileType == WidgetFileType.DataJson)
                {
                    content = string.Format(TemplateFiles.JavascriptDataFileTemplate, file.Content);
                }

                File.WriteAllText(destination, content);
            }

            File.WriteAllText(Path.Combine(DeployPath, "streamerBotEvents.js"), TemplateFiles.StreamerBotEventHandlers);

            SetStatus($"Generated to '{DeployPath}'", success: true);
        }
        catch (Exception exception)
        {
            SetStatus($"Error: {exception.Message}", error: true);
        }
    }

    #endregion Event Handlers

    #region Helpers

    public static string SearchAndReplaceDataVariables(string content, string jsonData)
    {
        using JsonDocument jsonDocument = JsonDocument.Parse(jsonData);

        foreach (JsonProperty field in jsonDocument.RootElement.EnumerateObject())
        {
            string value = field.Value.ValueKind switch
            {
                JsonValueKind.String
                    => field.Value.GetString() ?? string.Empty,
                JsonValueKind.Number
                    => field.Value.GetRawText(),
                JsonValueKind.True
                    => "true",
                JsonValueKind.False
                    => "false",
                JsonValueKind.Null
                    => string.Empty,
                _
                    => field.Value.GetRawText()
            };

            content = content.Replace("{{" + field.Name + "}}", value);
            content = content.Replace("{" + field.Name + "}", value);
        }

        return content;
    }

    public static string FixProtocolRelativeUrls(string htmlContent)
        => Regex.Replace
        (
            htmlContent,
            @"(src|href)=""//",
            "$1=\"https://",
            RegexOptions.IgnoreCase,
            TimeSpan.FromSeconds(1)
        );

    private void SelectWidget(Widget widget)
    {
        _selectedWidget      = widget;
        WidgetNameBox.Text   = widget.Name;
        DeployPathBox.Text   = widget.FolderLocation;
        FileList.ItemsSource = widget.Files;
    
        OnFilesChanged();
        SetStatus("Import your widget files.");
    }

    private void ClearDetail()
    {
        WidgetNameBox.Text    = "Select or create a widget";
        DeployPathBox.Text    = string.Empty;
        GenerateBtn.IsEnabled = false;
        HideWarning();
        SetStatus("Select or create a widget to begin.");
    }

    private void OnFilesChanged()
    {
        if (_selectedWidget is null)
            return;

        bool hasFiles = _selectedWidget.Files.Count > 0;

        FilesEmptyLabel.Visibility = hasFiles
            ? Visibility.Collapsed
            : Visibility.Visible;

        if (!hasFiles)
        {
            HideWarning();
            GenerateBtn.IsEnabled = false;
            SetStatus("Import your widget files to begin.");
            return;
        }

        if (!IsValidFileSet(out string warning))
        {
            ShowWarning(warning);
            GenerateBtn.IsEnabled = false;
        }
        else
        {
            HideWarning();
            GenerateBtn.IsEnabled = true;
            SetStatus("Ready to generate.");
        }
    }

    private bool IsValidFileSet(out string validationError)
    {
        var duplicates = new List<string>();

        foreach (string extension in _relevantFileExtensions)
        {
            int count = _selectedWidget!.Files.Count(file => Path.GetExtension(file.FileName).Equals(extension, StringComparison.OrdinalIgnoreCase));

            if (count > 1 && extension != ".json")
                duplicates.Add(extension);
        }

        if (duplicates.Count > 0)
        {
            validationError = $"Duplicate files detected: {string.Join(", ", duplicates)}. Remove the extra file(s).";
            return false;
        }

        if (!_selectedWidget!.Files.Any(file => Path.GetExtension(file.FileName).Equals(".html", StringComparison.OrdinalIgnoreCase)))
        {
            validationError = "A .html file is required. Please import your widget's HTML file.";
            return false;
        }

        validationError = string.Empty;
        return true;
    }

    private void ShowWarning(string message)
    {
        WarningText.Text         = message;
        WarningBanner.Visibility = Visibility.Visible;
    }

    private void HideWarning()
        => WarningBanner.Visibility = Visibility.Collapsed;

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