using System.Windows;
using System.Windows.Controls;

namespace StreamElementsToStreamerBotOverlayMigrator.Controls;

/// <summary>
/// A ComboBox subclass that automatically applies the dark themed template
/// defined in Themes/Generic.xaml — no style key needed at the call site.
/// </summary>
public class StyledDropdown: ComboBox
{
    static StyledDropdown()
    {
        DefaultStyleKeyProperty.OverrideMetadata
        (
            typeof(StyledDropdown),
            new FrameworkPropertyMetadata(typeof(StyledDropdown))
        );
    }
}