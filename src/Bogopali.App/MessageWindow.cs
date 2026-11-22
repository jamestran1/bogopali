using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Bogopali.App;

/// <summary>A small, non-blocking notification window; a tray app has no main window to show messages in.</summary>
internal sealed class MessageWindow : Window
{
    public MessageWindow(string message, string? detail, string okText)
    {
        Title = "bogopali";
        SizeToContent = SizeToContent.WidthAndHeight;
        CanResize = false;
        Topmost = true;
        ShowInTaskbar = true;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        Button ok = new()
        {
            Content = okText,
            HorizontalAlignment = HorizontalAlignment.Right,
            MinWidth = 80,
            IsDefault = true,
        };
        ok.Click += (_, _) => Close();

        StackPanel panel = new() { Margin = new Thickness(20), Spacing = 12, MaxWidth = 440 };
        panel.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap });
        if (!string.IsNullOrWhiteSpace(detail))
        {
            panel.Children.Add(new TextBlock { Text = detail, TextWrapping = TextWrapping.Wrap, Opacity = 0.7, FontSize = 12 });
        }

        panel.Children.Add(ok);
        Content = panel;
    }

    public static void ShowMessage(string message, string? detail, string okText)
    {
        MessageWindow window = new(message, detail, okText);
        window.Show();
        window.Activate();
    }
}
