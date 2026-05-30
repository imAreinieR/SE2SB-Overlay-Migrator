using StreamElementsToStreamerBotOverlayMigrator.Common;
using StreamElementsToStreamerBotOverlayMigrator.Common.ExtensionMethods;
using StreamElementsToStreamerBotOverlayMigrator.Data;
using StreamElementsToStreamerBotOverlayMigrator.Templates;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace StreamElementsToStreamerBotOverlayMigrator.Services;

public static class WidgetFileImportAndExportService
{
    private readonly static List<string> _allowedImportFileExtensions = new() { ".html", ".js", ".css", ".json", ".zip" };
    private readonly static List<string> _allowedWidgetFileExtensions = new() { ".html", ".js", ".css", ".json" };
    private readonly static TimeSpan     _defaultRegexTimeout = TimeSpan.FromSeconds(1);

    public static IEnumerable<WidgetFile> FetchWidgetFiles(IEnumerable<string> filePaths)
        => filePaths
        .Where(filePath => _allowedImportFileExtensions.Contains(Path.GetExtension(filePath)) || File.GetAttributes(filePath).HasFlag(FileAttributes.Directory))
        .SelectMany
        (
            filePath =>
            {
                bool isDirectory = File.GetAttributes(filePath).HasFlag(FileAttributes.Directory);

                return isDirectory
                    ? FetchWidgetFiles(Directory.EnumerateFiles(filePath, "*.*", SearchOption.AllDirectories))
                    : Path.GetExtension(filePath).Equals(".zip", StringComparison.OrdinalIgnoreCase)
                        ? UnZipAndExtractWidgetFiles(filePath)
                        : new[] { new WidgetFile(Path.GetFileName(filePath), File.ReadAllText(filePath)) };
            }
        );

    private static IEnumerable<WidgetFile> UnZipAndExtractWidgetFiles(string filePath)
    {
        using var file = File.OpenRead(filePath);
        using var zip = new ZipArchive(file, ZipArchiveMode.Read);

        return zip
            .Entries
            .Where(entry => _allowedWidgetFileExtensions.Contains(Path.GetExtension(entry.Name)))
            .Select(entry =>
            {
                using var stream = entry.Open();
                return new WidgetFile(entry.Name, new StreamReader(stream).ReadToEnd());
            })
            .ToList();
    }

    public static bool CheckIsValidFileSet(this IEnumerable<WidgetFile> widgetFiles, out string validationError)
    {
        var duplicates = new List<string>();

        foreach (string extension in _allowedWidgetFileExtensions)
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
            Directory.CreateDirectory(widget.FolderLocation);

            string jsonData = widget.Files.FirstOrDefault(file => file.WidgetFileType == WidgetFileType.DataJson)?.Content ?? string.Empty;

            foreach (WidgetFile file in widget.Files)
            {
                string fileName    = file.GetFileNameForWidgetFileType();
                string destination = Path.Combine(widget.FolderLocation, fileName);
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

            File.WriteAllText(Path.Combine(widget.FolderLocation, "streamerBotEvents.js"), TemplateFiles.StreamerBotEventHandlersFile);

            errorMessage = $"Generated to '{widget.FolderLocation}'";
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
        if (!RequiresVariableReplacement(content))
        {
            return content;
        }
        else if (string.IsNullOrEmpty(jsonData))
        {
            throw new ArgumentException("Missing fields.json file.");
        }

        using JsonDocument jsonDocument = JsonDocument.Parse
        (
            jsonData,
            new JsonDocumentOptions
            {
                AllowTrailingCommas = true
            }
        );

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

    private static bool RequiresVariableReplacement(string content)
        => Regex.IsMatch(
            content,
            @"(?<!\$)\{\{[\w]+\}\}|(?<!\$)(?<!\{)\{[\w]+\}(?!\})",
            RegexOptions.None,
            _defaultRegexTimeout
        );

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