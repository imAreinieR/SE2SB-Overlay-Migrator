using StreamElementsToStreamerBotOverlayMigrator.Common;
using StreamElementsToStreamerBotOverlayMigrator.Common.ExtensionMethods;
using StreamElementsToStreamerBotOverlayMigrator.Data;
using StreamElementsToStreamerBotOverlayMigrator.Data.Interfaces;
using StreamElementsToStreamerBotOverlayMigrator.Templates;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace StreamElementsToStreamerBotOverlayMigrator.Services;

public static class WidgetFileImportAndExportService
{
    private readonly static TimeSpan                   _defaultRegexTimeout = TimeSpan.FromSeconds(1);
    private readonly static Dictionary<string, string> WidgetIniSectionToCanonicalFileName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["HTML"]   = "widget.html",
        ["CSS"]    = "widget.css",
        ["JS"]     = "widget.js",
        ["FIELDS"] = "fields.json",
        ["DATA"]   = "data.json"
    };

    public static void AddWidgetFilesToFromPaths(Widget widget, IEnumerable<string> paths)
    {
        foreach (WidgetFile widgetFile in FetchWidgetFiles(paths))
        {
            if (widget.Files.Any(file => file.FileName == widgetFile.FileName))
                continue;

            widget.AddWidgetFile(widgetFile);
        }
    }

    public static IEnumerable<WidgetFile> FetchWidgetFiles(IEnumerable<string> filePaths)
    {
        List<string> filePathList      = filePaths.ToList();
        List<string> nonDirectoryPaths = filePathList.Where(filePath => !File.GetAttributes(filePath).HasFlag(FileAttributes.Directory)).ToList();

        string? widgetIniPath = nonDirectoryPaths.FirstOrDefault
        (
            filePath => Path.GetFileName(filePath).Equals("widget.ini", StringComparison.OrdinalIgnoreCase)
        );

        if (widgetIniPath is not null)
            return ExtractWidgetIoFiles(new LooseWidgetFileSource(nonDirectoryPaths), File.ReadAllText(widgetIniPath));

        return filePathList
            .Where(filePath => SupportedFileTypes.AllowedImportFileExtensions.Contains(Path.GetExtension(filePath)) || File.GetAttributes(filePath).HasFlag(FileAttributes.Directory))
            .SelectMany
            (
                filePath =>
                {
                    bool isDirectory = File.GetAttributes(filePath).HasFlag(FileAttributes.Directory);

                    return isDirectory
                        ? FetchWidgetFiles(Directory.EnumerateFiles(filePath, "*.*", SearchOption.AllDirectories))
                        : Path.GetExtension(filePath).Equals(".zip", StringComparison.OrdinalIgnoreCase)
                            ? FetchWidgetFilesFromZip(filePath)
                            : new[] { new WidgetFile(Path.GetFileName(filePath), EncodeFileContentForImport(File.ReadAllBytes(filePath), filePath)) };
                }
            );
    }

    private static List<WidgetFile> FetchWidgetFilesFromZip(string filePath)
    {
        using var file = File.OpenRead(filePath);
        using var zip  = new ZipArchive(file, ZipArchiveMode.Read);

        var source = new ZipWidgetFileSource(zip);

        ZipArchiveEntry? widgetIniEntry = zip.Entries.FirstOrDefault
        (
            entry => entry.Name.Equals("widget.ini", StringComparison.OrdinalIgnoreCase)
        );

        return widgetIniEntry is not null
            ? ExtractWidgetIoFiles(source, ReadZipEntryAsText(widgetIniEntry))
            : ExtractFilesByExtension(source, SupportedFileTypes.AllowedWidgetFileExtensions);
    }

    private static List<WidgetFile> ExtractWidgetIoFiles(IWidgetFileSource source, string iniContent)
    {
        Dictionary<string, string> sectionPaths = ParseWidgetIniPaths(iniContent);

        var widgetFiles = new List<WidgetFile>();

        foreach ((string section, string canonicalFileName) in WidgetIniSectionToCanonicalFileName)
        {
            if (!sectionPaths.TryGetValue(section, out string? referencedFileName))
                continue;

            string lookupName = Path.GetFileName(referencedFileName);

            if (!source.TryReadFile(lookupName, out byte[] fileBytes))
                continue;

            widgetFiles.Add(new WidgetFile(canonicalFileName, EncodeFileContentForImport(fileBytes, canonicalFileName)));
        }

        return widgetFiles;
    }

    private static List<WidgetFile> ExtractFilesByExtension(IWidgetFileSource source, IEnumerable<string> allowedExtensions)
        => source
            .FileNames
            .Where(fileName => allowedExtensions.Contains(Path.GetExtension(fileName)))
            .Select
            (
                fileName =>
                {
                    source.TryReadFile(fileName, out byte[] fileBytes);
                    return new WidgetFile(fileName, EncodeFileContentForImport(fileBytes, fileName));
                }
            )
            .ToList();

    private static Dictionary<string, string> ParseWidgetIniPaths(string iniContent)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? currentSection = null;

        foreach (string rawLine in iniContent.Split('\n'))
        {
            string line = rawLine.Trim().TrimEnd('\r');

            if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("#"))
                continue;

            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                currentSection = line[1..^1].Trim();
                continue;
            }

            if (currentSection is null)
                continue;

            Match match = Regex.Match(line, @"^path\s*=\s*""(?<path>.*)""\s*$", RegexOptions.IgnoreCase, _defaultRegexTimeout);

            if (match.Success)
                result[currentSection] = match.Groups["path"].Value.Trim();
        }

        return result;
    }

    private static byte[] ReadZipEntryAsBytes(ZipArchiveEntry entry)
    {
        using Stream stream = entry.Open();
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    private static string ReadZipEntryAsText(ZipArchiveEntry entry)
        => System.Text.Encoding.UTF8.GetString(ReadZipEntryAsBytes(entry));

    private static string EncodeFileContentForImport(byte[] fileBytes, string fileName)
        => SupportedFileTypes.IsTextBasedExtension(fileName)
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

    public static bool ExportRawFilesAsZip(this Widget widget, string destinationZipPath, out string errorMessage)
    {
        try
        {
            if (File.Exists(destinationZipPath))
                File.Delete(destinationZipPath);

            using (FileStream zipStream = new FileStream(destinationZipPath, FileMode.Create))
            using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
            {
                foreach (WidgetFile widgetFile in widget.Files)
                {
                    ZipArchiveEntry entry = archive.CreateEntry(widgetFile.FileName, CompressionLevel.Optimal);

                    using Stream entryStream = entry.Open();
                    byte[]       fileBytes   = DecodeFileContentForExport(widgetFile);

                    entryStream.Write(fileBytes, 0, fileBytes.Length);
                }
            }

            errorMessage = $"Exported raw files to '{destinationZipPath}'";
            return true;
        }
        catch (Exception exception)
        {
            errorMessage = $"Error: {exception.Message}";
            Debug.WriteLine(exception);
            return false;
        }
    }

    private static byte[] DecodeFileContentForExport(WidgetFile widgetFile)
        => widgetFile.WidgetFileType.IsTextBasedFile()
            ? System.Text.Encoding.UTF8.GetBytes(widgetFile.Content)
            : Convert.FromBase64String(widgetFile.Content);

    public static bool GenerateExportFilesForWidget(this Widget widget, out string errorMessage)
    {
        try
        {
            TryClearDirectory(widget.FolderLocation);
            Directory.CreateDirectory(widget.FolderLocation);

            string jsonData = widget.Files.FirstOrDefault(file => file.WidgetFileType == WidgetFileType.DataJson)?.Content ?? string.Empty;

            foreach (WidgetFile widgetFile in widget.Files)
            {
                string fileName          = widgetFile.GetFileNameForWidgetFileType();
                string subFolderName     = widgetFile.WidgetFileType.GetSubFolderForWidgetFileType();
                string destinationFolder = string.IsNullOrEmpty(subFolderName)
                    ? widget.FolderLocation
                    : Path.Combine(widget.FolderLocation, subFolderName);

                if (!Directory.Exists(destinationFolder))
                    Directory.CreateDirectory(destinationFolder);

                string destination = Path.Combine(destinationFolder, fileName);

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

            File.WriteAllText(Path.Combine(widget.FolderLocation, "sessionData.js"),                  TemplateFiles.SessionDataFile);
            File.WriteAllText(Path.Combine(widget.FolderLocation, "streamerBotApiAndEventBridge.js"), TemplateFiles.GetApiAndEventBridgeFile(SettingsService.Current));

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
            content = string.Format(TemplateFiles.HtmlFile, FixProtocolRelativeUrls(content));
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