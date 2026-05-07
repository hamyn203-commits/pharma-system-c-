using System.Windows.Controls;

namespace AlNeda.Admin.ViewModels;

public class TextBlockPlaceholder : ContentControl
{
    public TextBlockPlaceholder(string title)
    {
        Content = new TextBlock
        {
            Text = $"{title}\n(قريباً)",
            FontSize = 24,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x8D, 0x97, 0xA6)),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = System.Windows.VerticalAlignment.Center,
            TextAlignment = System.Windows.TextAlignment.Center
        };
    }
}
