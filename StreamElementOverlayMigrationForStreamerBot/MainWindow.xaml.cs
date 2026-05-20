using Microsoft.Win32;
using StreamElementsToStreamerBotMigrationTool.Common;
using StreamElementsToStreamerBotMigrationTool.Data;
using StreamElementsToStreamerBotMigrationTool.Templates;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;

namespace StreamElementsToStreamerBotMigrationTool;

public partial class MainWindow: Window
{
    private readonly List<string>                       _relevantFileExtensions = new () { ".html", ".js", ".css", ".json" };
    private readonly ObservableCollection<ImportedFile> _importedFiles          = new ();

    private static readonly string DeployPath = Path.Combine
    (
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "imA-SB-Widgets",
        "widget1"
    );

    public MainWindow()
    {
        InitializeComponent();

        FileList.ItemsSource = _importedFiles;
        _importedFiles.CollectionChanged += (_, _) => OnFilesChanged();

        DeployPathBox.Text = DeployPath;
    }

    #region Event Handlers

    private void Import_Click(object sender, RoutedEventArgs e)
    {
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

            if (_importedFiles.Any(file => file.FileName == name))
                continue;

            _importedFiles.Add(new ImportedFile(name, File.ReadAllText(path)));
        }
    }

    private void RemoveFile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.Tag is ImportedFile file)
            _importedFiles.Remove(file);
    }

    private void CopyPath_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(DeployPath);
        SetStatus("Path copied to clipboard.");
    }

    private void Generate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(DeployPath);

            string jsonData = _importedFiles.FirstOrDefault(file => file.WidgetFileType == WidgetFileType.DataJson)?.Content ?? string.Empty;

            foreach (ImportedFile file in _importedFiles)
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
                    ? ApplySEFieldData(file.Content, jsonData)
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

            SetStatus($"Generated to {DeployPath}", success: true);
        }
        catch (Exception exception)
        {
            SetStatus($"Error: {exception.Message}", error: true);
        }
    }

    #endregion Event Handlers

    #region Helpers

    public static string ApplySEFieldData(string content, string jsonData)
    {
        using JsonDocument doc = JsonDocument.Parse(jsonData);

        foreach (JsonProperty field in doc.RootElement.EnumerateObject())
        {
            string value = field.Value.ValueKind switch
            {
                JsonValueKind.String => field.Value.GetString() ?? "",
                JsonValueKind.Number => field.Value.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => "",
                _ => field.Value.GetRawText()
            };

            // Replace double-brace first to avoid partial matches
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

    private void OnFilesChanged()
    {
        bool hasFiles = _importedFiles.Any();

        EmptyLabel.Visibility = hasFiles
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
            int count = _importedFiles.Count(file => Path.GetExtension(file.FileName).Equals(extension, StringComparison.OrdinalIgnoreCase));

            if (count > 1 && extension != ".json")
                duplicates.Add(extension);
        }

        if (duplicates.Count > 0)
        {
            validationError = $"Duplicate files detected: {string.Join(", ", duplicates)}. Remove the extra file(s).";
            return false;
        }

        if (!_importedFiles.Any(file => Path.GetExtension(file.FileName).Equals(".html", StringComparison.OrdinalIgnoreCase)))
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
