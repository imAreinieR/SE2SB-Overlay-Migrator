namespace StreamElementsToStreamerBotOverlayMigrator.Services;

internal static class MediaServerHost
{
    private static readonly Lazy<InMemoryMediaServer> _instance = new(() => new InMemoryMediaServer());

    public static InMemoryMediaServer Instance
        => _instance.Value;
}