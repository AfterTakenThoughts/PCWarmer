using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using PCWarmer.Core.Stress;

namespace PCWarmer.Core.Screensavers
{
    /// <summary>Colorful glowing balls bouncing around the screen; overlapping glows add up into bright spots.</summary>
    public sealed class BouncingOrbsScreensaver : IScreensaver
    {
        /// <summary>The glow halo is drawn larger than the orb itself.</summary>
        private const float GlowScale = 2.5f;

        private readonly int orbCount;
        private Texture2D glow;
        private OrbField field;

        public BouncingOrbsScreensaver(StressLevel level = StressLevel.Medium)
        {
            // Orbs are big and overlap a lot (heavy on pixel fill), so a tenth of the level's particle cap.
            orbCount = StressProfile.For(level).MaxParticles / 10;
        }

        public string Id => "bouncing-orbs";

        public int LiveParticles => field?.Count ?? 0;

        public void LoadContent(ContentManager content, GraphicsDevice device)
        {
            glow = GlowTexture.Create(device);
            Viewport viewport = device.Viewport;
            field = OrbField.CreateRandom(new Vector2(viewport.Width, viewport.Height), orbCount, new Random());
        }

        public void Update(GameTime gameTime) => field?.Update(gameTime.ElapsedGameTime);

        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (field == null)
                return;

            var origin = new Vector2(GlowTexture.Size / 2f);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);
            foreach (Orb orb in field.Orbs)
            {
                float scale = orb.Radius * 2f * GlowScale / GlowTexture.Size;
                spriteBatch.Draw(glow, orb.Position, null, orb.Color * 0.6f, 0f, origin, scale, SpriteEffects.None, 0f);
            }
            spriteBatch.End();
        }

        public void UnloadContent()
        {
            field = null;
            glow?.Dispose();
            glow = null;
        }
    }
}
