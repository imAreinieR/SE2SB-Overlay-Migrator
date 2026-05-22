using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StreamElementsToStreamerBotOverlayMigrator.Common.ExtensionMethods;

public static partial class ExtensionMethods
{
    public static DependencyObject? FindAncestorWithChildren(this DependencyObject start)
    {
        DependencyObject current = VisualTreeHelper.GetParent(start);

        while (current != null)
        {
            if
            (
                FindVisualChild<StackPanel>(current, "NameDisplayPanel") != null
                    && FindVisualChild<TextBox>(current, "NameEditBox") != null
            )
                return current;

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    public static T? FindVisualChild<T>(this DependencyObject parent, string name)
        where T : FrameworkElement
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T fe && fe.Name == name)
                return fe;

            var result = FindVisualChild<T>(child, name);
            if (result != null)
                return result;
        }
        return null;
    }

    public static T? FindVisualSibling<T>(this FrameworkElement element, string name)
        where T : FrameworkElement
    {
        var parent = VisualTreeHelper.GetParent(element);
        if (parent == null)
            return null;

        return FindVisualChild<T>(parent, name);
    }
}