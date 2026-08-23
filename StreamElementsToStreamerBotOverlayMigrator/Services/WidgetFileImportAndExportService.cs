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
    private readonly static TimeSpan _defaultRegexTimeout = TimeSpan.FromSeconds(1);

    public static IEnumerable<WidgetFile> FetchWidgetFiles(IEnumerable<string> filePaths)
        => filePaths
        .Where(filePath => SupportedFileTypes.AllowedImportFileExtensions.Contains(Path.GetExtension(filePath)) || File.GetAttributes(filePath).HasFlag(FileAttributes.Directory))
        .SelectMany
        (
            filePath =>
            {
                bool isDirectory = File.GetAttributes(filePath).HasFlag(FileAttributes.Directory);

                return isDirectory
                    ? FetchWidgetFiles(Directory.EnumerateFiles(filePath, "*.*", SearchOption.AllDirectories))
                    : Path.GetExtension(filePath).Equals(".zip", StringComparison.OrdinalIgnoreCase)
                        ? UnZipAndExtractWidgetFiles(filePath)
                        : new[] { new WidgetFile(Path.GetFileName(filePath), EncodeFileContentForImport(File.ReadAllBytes(filePath), filePath)) };
            }
        );

    private static IEnumerable<WidgetFile> UnZipAndExtractWidgetFiles(string filePath)
    {
        using var file = File.OpenRead(filePath);
        using var zip = new ZipArchive(file, ZipArchiveMode.Read);

        return zip
            .Entries
            .Where(entry => SupportedFileTypes.AllowedWidgetFileExtensions.Contains(Path.GetExtension(entry.Name)))
            .Select
            (
                entry =>
                {
                    using Stream stream = entry.Open();
                    using var memoryStream = new MemoryStream();
                    stream.CopyTo(memoryStream);

                    return new WidgetFile(entry.Name, EncodeFileContentForImport(memoryStream.ToArray(), entry.Name));
                }
            )
            .ToList();
    }

    private static bool IsTextBasedExtension(string fileName)
        => SupportedFileTypes
            .DocumentExtensions
            .Contains(Path.GetExtension(fileName), StringComparer.OrdinalIgnoreCase);

    private static string EncodeFileContentForImport(byte[] fileBytes, string fileName)
        => IsTextBasedExtension(fileName)
            ? System.Text.Encoding.UTF8.GetString(fileBytes)
            : Convert.ToBase64String(fileBytes);

    public static bool CheckIsValidFileSet(this IEnumerable<WidgetFile> widgetFiles, out string validationError)
    {
        var duplicates = new List<string>();

        foreach (string extension in SupportedFileTypes.AllowedWidgetFileExtensions)
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
            TryClearDirectory(widget.FolderLocation);
            Directory.CreateDirectory(widget.FolderLocation);

            string jsonData = widget.Files.FirstOrDefault(file => file.WidgetFileType == WidgetFileType.DataJson)?.Content ?? string.Empty;

            foreach (WidgetFile widgetFile in widget.Files)
            {
                string fileName    = widgetFile.GetFileNameForWidgetFileType();
                string destination = Path.Combine(widget.FolderLocation, fileName);

                if (widgetFile.WidgetFileType.IsTextBasedFile())
                {
                    File.WriteAllText(destination, GenerateFile(widgetFile, jsonData));
                }
                else
                {
                    byte[] fileBytes = Convert.FromBase64String(widgetFile.Content);
                    File.WriteAllBytes(destination, fileBytes);
                }
            }

            File.WriteAllText(Path.Combine(widget.FolderLocation, "streamerBotApiAndEventBridge.js"), TemplateFiles.ApiAndEventBridgeFile);

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

    public static string GenerateFile(WidgetFile widgetFile, string jsonData)
    {
        string content = widgetFile.WidgetFileType == WidgetFileType.Html || widgetFile.WidgetFileType == WidgetFileType.Css || widgetFile.WidgetFileType == WidgetFileType.Javascript
            ? SearchAndReplaceDataVariables(widgetFile.Content, jsonData)
            : widgetFile.Content;

        if (widgetFile.WidgetFileType == WidgetFileType.Html)
        {
            content = string.Format(TemplateFiles.HtmlFile, FixProtocolRelativeUrls(widgetFile.Content));
        }
        else if (widgetFile.WidgetFileType == WidgetFileType.DataJson)
        {
            content = string.Format(TemplateFiles.JavascriptDataFile, widgetFile.Content);
        }

        return content;
    }

    private static bool TryClearDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            return false;

        try
        {
            var directoryInfo = new DirectoryInfo(directoryPath);
            foreach (FileInfo file in directoryInfo.GetFiles())
                file.Delete();

            foreach (DirectoryInfo subDirectory in directoryInfo.GetDirectories())
                subDirectory.Delete(true);

            return true;
        }
        catch (Exception exception)
        {
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

    private static bool RequiresVariableReplacement(string fileContent)
        => Regex.IsMatch
        (
            fileContent,
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