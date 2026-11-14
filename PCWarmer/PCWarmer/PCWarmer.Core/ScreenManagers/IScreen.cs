using Microsoft.Xna.Framework;

namespace PCWarmer.ScreenManagers;

/// <summary>
/// Represents a screen in PCWarmer: a single layer with its own update and draw logic.
/// </summary>
public interface IScreen
{
    /// <summary>
    /// Runs the screen's per-frame logic.
    /// </summary>
    /// <param name="gameTime">Timing values for the current frame.</param>
    /// <param name="otherScreenHasFocus">True if another screen currently has input focus.</param>
    /// <param name="coveredByOtherScreen">True if a non-popup screen is drawn over this one.</param>
    void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen);

    /// <summary>
    /// Draws the screen.
    /// </summary>
    /// <param name="gameTime">Timing values for the current frame.</param>
    void Draw(GameTime gameTime);
}
