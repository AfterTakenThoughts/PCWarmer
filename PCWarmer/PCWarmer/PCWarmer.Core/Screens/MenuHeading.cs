using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PCWarmer.Screens;

/// <summary>
/// A section heading inside a menu. It can't be selected (keyboard navigation and clicks skip it)
/// and is drawn like a menu title rather than a grayed-out option.
/// </summary>
class MenuHeading : MenuEntry
{
    private static readonly Color HeadingColor = Color.Black;

    public MenuHeading(string text)
        : base(text, enabled: false)
    {
    }

    public override void Draw(MenuScreen screen, bool isSelected, GameTime gameTime)
    {
        SpriteFont font = screen.ScreenManager.Font;
        var origin = new Vector2(0, font.LineSpacing / 2);

        screen.ScreenManager.SpriteBatch.DrawString(font, Text, Position, HeadingColor * screen.TransitionAlpha, 0,
                                                    origin, 1f, SpriteEffects.None, 0);
    }
}
