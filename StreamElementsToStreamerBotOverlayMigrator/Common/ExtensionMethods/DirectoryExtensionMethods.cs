using System.IO;

namespace StreamElementsToStreamerBotOverlayMigrator.Common.ExtensionMethods;

public static partial class ExtensionMethods
{
    public static DateTime? GetOldestFileTimestamp(this string directoryPath, bool recursive = false)
    {
        SearchOption searchOption = recursive
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;

        DateTime? oldestTime = Directory
            .EnumerateFiles(directoryPath, "*", searchOption)
            .Select(File.GetLastWriteTime)
            .Cast<DateTime?>()
            .Min();

        return oldestTime;
    }

    public static DateTime? GetLatestFileTimestamp(this string directoryPath, bool recursive = false)
    {
        SearchOption searchOption = recursive
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;

        DateTime? oldestTime = Directory
            .EnumerateFiles(directoryPath, "*", searchOption)
            .Select(File.GetLastWriteTime)
            .Cast<DateTime?>()
            .Max();

        return oldestTime;
    }
}