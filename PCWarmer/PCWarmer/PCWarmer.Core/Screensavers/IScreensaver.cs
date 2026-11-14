using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace PCWarmer.Core.Screensavers
{
    /// <summary>A graphics load that <c>ScreensaverScreen</c> hosts while the room warms.</summary>
    public interface IScreensaver
    {
        /// <summary>The id the catalog and settings use, for example <c>"particle-storm"</c>.</summary>
        string Id { get; }

        /// <summary>Things currently on screen (particles, stars, orbs), for the HUD.</summary>
        int LiveParticles { get; }

        void LoadContent(ContentManager content, GraphicsDevice device);

        void Update(GameTime gameTime);

        /// <summary>Draws the screensaver. Opens and closes its own sprite batch.</summary>
        void Draw(SpriteBatch spriteBatch, GameTime gameTime);

        void UnloadContent();
    }
}
