using StreamElementsToStreamerBotOverlayMigrator.Data;

namespace StreamElementsToStreamerBotOverlayMigrator.Common;

public class WidgetFileComparer: IComparer<WidgetFile>
{
    public static readonly WidgetFileComparer Instance = new();

    private WidgetFileComparer()
    {}

    public int Compare(WidgetFile? first, WidgetFile? second)
    {
        if (ReferenceEquals(first, second))
            return 0;
        if (first is null)
            return -1;
        if (second is null)
            return 1;

        int enumComparison = first.WidgetFileType.CompareTo(second.WidgetFileType);

        if (enumComparison != 0)
            return enumComparison;

        return string.Compare(first.FileName, second.FileName, StringComparison.OrdinalIgnoreCase);
    }
}