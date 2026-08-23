using StreamElementsToStreamerBotOverlayMigrator.Data.Interfaces;
using System.IO;
using System.IO.Compression;

namespace StreamElementsToStreamerBotOverlayMigrator.Data;

public class ZipWidgetFileSource: IWidgetFileSource
{
    private readonly ZipArchive _zip;

    public ZipWidgetFileSource(ZipArchive zip)
        => _zip = zip;

    public IEnumerable<string> FileNames
        => _zip.Entries.Select(entry => entry.Name);

    public bool TryReadFile(string fileName, out byte[] fileBytes)
    {
        ZipArchiveEntry? entry = _zip.Entries.FirstOrDefault
        (
            entry => entry.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase)
        );

        if (entry is null)
        {
            fileBytes = Array.Empty<byte>();
            return false;
        }

        fileBytes = ReadZipEntryAsBytes(entry);
        return true;
    }

    private static byte[] ReadZipEntryAsBytes(ZipArchiveEntry entry)
    {
        using Stream stream = entry.Open();
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
}