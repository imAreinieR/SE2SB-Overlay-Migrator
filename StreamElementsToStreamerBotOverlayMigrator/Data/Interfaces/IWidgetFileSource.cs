namespace StreamElementsToStreamerBotOverlayMigrator.Data.Interfaces;

public interface IWidgetFileSource
{
    IEnumerable<string> FileNames { get; }
    bool TryReadFile(string fileName, out byte[] fileBytes);
}