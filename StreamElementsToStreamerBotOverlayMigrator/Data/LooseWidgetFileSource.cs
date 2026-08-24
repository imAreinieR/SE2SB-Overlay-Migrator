using StreamElementsToStreamerBotOverlayMigrator.Data.Interfaces;
using System.IO;

namespace StreamElementsToStreamerBotOverlayMigrator.Data;

public class LooseWidgetFileSource: IWidgetFileSource
{
    private readonly Dictionary<string, string> _pathsByFileName;

    public LooseWidgetFileSource(IEnumerable<string> filePaths)
        => _pathsByFileName = filePaths.ToDictionary
        (
            filePath => Path.GetFileName(filePath)!,
            filePath => filePath, StringComparer.OrdinalIgnoreCase
        );

    public IEnumerable<string> FileNames
        => _pathsByFileName.Keys;

    public bool TryReadFile(string fileName, out byte[] fileBytes)
    {
        if (_pathsByFileName.TryGetValue(fileName, out string? filePath))
        {
            fileBytes = File.ReadAllBytes(filePath);
            return true;
        }

        fileBytes = Array.Empty<byte>();
        return false;
    }
}