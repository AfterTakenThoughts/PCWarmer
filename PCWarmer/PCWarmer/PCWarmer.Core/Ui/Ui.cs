using FontStashSharp;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace PCWarmer.Core.Ui
{
    /// <summary>Small helpers for building the menus' widgets.</summary>
    public static class Ui
    {
        public static Label Text(string text, SpriteFontBase font, Color color) => new()
        {
            Text = text,
            Font = font,
            TextColor = color,
            VerticalAlignment = VerticalAlignment.Center,
        };

        /// <summary>A panel with a rounded background behind <paramref name="content"/>, which gets <paramref name="padding"/>.</summary>
        public static Panel Box(Widget content, Color fill, Color border, Thickness padding, int radius, Color? hoverBorder = null)
        {
            var panel = new Panel();
            panel.Widgets.Add(new BoxShape { Fill = fill, BorderColor = border, HoverBorderColor = hoverBorder, Radius = radius });
            content.Margin = padding;
            panel.Widgets.Add(content);
            return panel;
        }
    }
}
