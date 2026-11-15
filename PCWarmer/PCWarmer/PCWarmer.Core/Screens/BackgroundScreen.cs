using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using PCWarmer.Core.Ui;

namespace PCWarmer.Screens;

/// <summary>
/// The BackgroundScreen renders a static background image behind all other menu screens.
/// It remains fixed and unaffected by transitions on top of it.
/// </summary>
class BackgroundScreen : GameScreen
{

    /// <summary>
    /// Initializes a new instance of the BackgroundScreen class.
    /// Sets the transition times for screen appearance and disappearance.
    /// </summary>
    public BackgroundScreen()
    {
        TransitionOnTime = TimeSpan.FromSeconds(0.5);
        TransitionOffTime = TimeSpan.FromSeconds(0.5);
    }

    /// <summary>
    /// Loads the background texture using a local ContentManager.
    /// This allows for independent unloading of the background texture.
    /// </summary>
    public override void LoadContent()
    {
    }

    /// <summary>
    /// Unloads the background texture by unloading the local ContentManager.
    /// </summary>
    public override void UnloadContent()
    {
    }

    /// <summary>
    /// Updates the background screen.
    /// Forces the screen to remain active even when covered by other screens.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    /// <param name="otherScreenHasFocus">Indicates whether another screen has focus.</param>
    /// <param name="coveredByOtherScreen">Indicates whether the screen is covered by another screen.</param>
    public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
    {
        // Force coveredByOtherScreen to false to prevent the screen from transitioning off.
        base.Update(gameTime, otherScreenHasFocus, false);
    }

    /// <summary>
    /// Draws the background screen.
    /// Clears the screen and renders the background texture with transition alpha.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    public override void Draw(GameTime gameTime)
    {
        // A plain dark background behind every menu, fading with the screen transition.
        ScreenManager.GraphicsDevice.Clear(Color.Lerp(Color.Black, UiTheme.Background, TransitionAlpha));
    }
}
