using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Bogopali.Core;

namespace Bogopali.App;

/// <summary>Draws the Status Indicator icons: <c>ā</c> for Pāli Mode and <c>a</c> for Bypass Mode.</summary>
internal static class StatusIcons
{
    private const int Size = 64;

    public static WindowIcon For(InputMode mode) => mode == InputMode.Pali
        ? Render("ā", Color.FromRgb(0xB4, 0x53, 0x09))
        : Render("a", Color.FromRgb(0x52, 0x52, 0x5B));

    private static WindowIcon Render(string letter, Color background)
    {
        RenderTargetBitmap bitmap = new(new PixelSize(Size, Size), new Vector(96, 96));
        using (DrawingContext context = bitmap.CreateDrawingContext())
        {
            context.DrawRectangle(new SolidColorBrush(background), null, new RoundedRect(new Rect(0, 0, Size, Size), 12));
            FormattedText text = new(
                letter,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI", FontStyle.Normal, FontWeight.Bold),
                52,
                Brushes.White);
            context.DrawText(text, new Point((Size - text.Width) / 2, (Size - text.Height) / 2));
        }

        return new WindowIcon(bitmap);
    }
}
