using System.IO;

namespace StreamElementsToStreamerBotOverlayMigrator.Common;

internal static class SupportedFileTypes
{
    public static readonly string[] DocumentExtensions = { ".html", ".js", ".css", ".json" };
    public static readonly string[] ImageExtensions    = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".bmp", ".ico" };
    public static readonly string[] AudioExtensions    = { ".mp3", ".wav", ".m4a", ".aac" };
    public static readonly string[] VideoExtensions    = { ".mp4", ".webm" };
    public static readonly string[] ArchiveExtensions  = { ".zip" };

    public static readonly string[] AllowedWidgetFileExtensions = DocumentExtensions
        .Concat(ImageExtensions)
        .Concat(AudioExtensions)
        .Concat(VideoExtensions)
        .ToArray();

    public static readonly string[] AllowedImportFileExtensions = AllowedWidgetFileExtensions
        .Concat(ArchiveExtensions)
        .ToArray();

    public static string BuildImportFileDialogFilter(string label = "Widget files")
    {
        string pattern = string.Join(";", AllowedImportFileExtensions.Select(extension => $"*{extension}"));
        return $"{label} ({pattern})|{pattern}|All files (*.*)|*.*";
    }

    public static bool IsTextBasedExtension(string fileName)
        => DocumentExtensions.Contains(Path.GetExtension(fileName), StringComparer.OrdinalIgnoreCase);
}