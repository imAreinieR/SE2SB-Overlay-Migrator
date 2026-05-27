using System.Collections.ObjectModel;

namespace StreamElementsToStreamerBotOverlayMigrator.Common.ExtensionMethods;

public static partial class ExtensionMethods
{
    public static void AddSorted<T>(this ObservableCollection<T> collection, T item, IComparer<T>? comparer = null)
    {
        comparer ??= Comparer<T>.Default;

        int i = 0;
        while (i < collection.Count && comparer.Compare(collection[i], item) < 0)
            i++;

        collection.Insert(i, item);
    }

    public static void AddRangeSorted<T>(this ObservableCollection<T> collection, IEnumerable<T> items, IComparer<T>? comparer = null)
    {
        foreach (T item in items)
            collection.AddSorted(item, comparer);
    }
}