using StreamElementsToStreamerBotOverlayMigrator.Common;
using StreamElementsToStreamerBotOverlayMigrator.Common.ExtensionMethods;
using StreamElementsToStreamerBotOverlayMigrator.Data;
using StreamElementsToStreamerBotOverlayMigrator.Templates;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace StreamElementsToStreamerBotOverlayMigrator.Services;

public static class WidgetFileImportAndExportService
{
    private readonly static List<string> _relevantFileExtensions = new() { ".html", ".js", ".css", ".json" };
    private readonly static TimeSpan     _defaultRegexTimeout = TimeSpan.FromSeconds(1);

    public static IEnumerable<WidgetFile> FetchWidgetFiles(string[] filePaths)
    {
        foreach (string filePath in filePaths)
        {
            string? fileName = Path.GetFileName(filePath);

            yield return new WidgetFile(fileName, File.ReadAllText(filePath));
        }
    }

    public static bool CheckIsValidFileSet(this IEnumerable<WidgetFile> widgetFiles, out string validationError)
    {
        var duplicates = new List<string>();

        foreach (string extension in _relevantFileExtensions)
        {
            int count = widgetFiles.Count(file => Path.GetExtension(file.FileName).Equals(extension, StringComparison.OrdinalIgnoreCase));

            if (count > 1 && extension != ".json")
                duplicates.Add(extension);
        }

        if (duplicates.Count > 0)
        {
            validationError = $"Duplicate files detected: {string.Join(", ", duplicates)}. Remove the extra file(s).";
            return false;
        }

        if (!widgetFiles.Any(file => Path.GetExtension(file.FileName).Equals(".html", StringComparison.OrdinalIgnoreCase)))
        {
            validationError = "A .html file is required. Please import your widget's HTML file.";
            return false;
        }

        validationError = string.Empty;
        return true;
    }

    public static bool GenerateExportFilesForWidget(this Widget widget, out string errorMessage)
    {
        try
        {
            Directory.CreateDirectory(widget.DeployedLocation);

            string jsonData = widget.Files.FirstOrDefault(file => file.WidgetFileType == WidgetFileType.DataJson)?.Content ?? string.Empty;

            foreach (WidgetFile file in widget.Files)
            {
                string fileName    = file.GetFileNameForWidgetFileType();
                string destination = Path.Combine(widget.DeployedLocation, fileName);
                string content     = file.WidgetFileType == WidgetFileType.Html || file.WidgetFileType == WidgetFileType.Css || file.WidgetFileType == WidgetFileType.Javascript
                    ? SearchAndReplaceDataVariables(file.Content, jsonData)
                    : file.Content;

                if (file.WidgetFileType == WidgetFileType.Html)
                {
                    content = string.Format(TemplateFiles.HtmlFile, FixProtocolRelativeUrls(file.Content));
                }
                else if (file.WidgetFileType == WidgetFileType.DataJson)
                {
                    content = string.Format(TemplateFiles.JavascriptDataFile, file.Content);
                }

                File.WriteAllText(destination, content);
            }

            File.WriteAllText(Path.Combine(widget.DeployedLocation, "streamerBotEvents.js"), TemplateFiles.StreamerBotEventHandlersFile);

            errorMessage = $"Generated to '{widget.DeployedLocation}'";
            return true;
        }
        catch (Exception exception)
        {
            errorMessage = $"Error: {exception.Message}";
            Debug.WriteLine(exception);
            return false;
        }
    }

    private static string SearchAndReplaceDataVariables(string content, string jsonData)
    {
        using JsonDocument jsonDocument = JsonDocument.Parse(jsonData);

        foreach (JsonProperty jsonProperty in jsonDocument.RootElement.EnumerateObject())
        {
            string value = jsonProperty.Value.ValueKind switch
            {
                JsonValueKind.String
                    => jsonProperty.Value.GetString() ?? string.Empty,
                JsonValueKind.Number
                    => jsonProperty.Value.GetRawText(),
                JsonValueKind.True
                    => "true",
                JsonValueKind.False
                    => "false",
                JsonValueKind.Null
                    => string.Empty,
                _
                    => jsonProperty.Value.GetRawText()
            };

            string escapedName = Regex.Escape(jsonProperty.Name);

            try
            {
                content = Regex.Replace
                (
                    content,
                    @"(?<!\$)\{\{" + escapedName + @"\}\}",
                    value,
                    RegexOptions.None,
                    _defaultRegexTimeout
                );

                content = Regex.Replace
                (
                    content,
                    @"(?<!\$)(?<!\{)\{" + escapedName + @"\}(?!\})",
                    value,
                    RegexOptions.None,
                    _defaultRegexTimeout
                );
            }
            catch (RegexMatchTimeoutException exception)
            {
                throw new InvalidOperationException($"Regex timed out while replacing variable '{jsonProperty.Name}'. Pattern may be too complex for the given input.", exception);
            }
        }
        return content;
    }

    private static string FixProtocolRelativeUrls(string htmlContent)
        => Regex.Replace
        (
            htmlContent,
            @"(src|href)=""//",
            "$1=\"https://",
            RegexOptions.IgnoreCase,
            _defaultRegexTimeout
        );
}