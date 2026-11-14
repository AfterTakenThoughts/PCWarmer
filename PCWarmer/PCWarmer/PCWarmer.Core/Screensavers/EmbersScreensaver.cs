using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using PCWarmer.Core.Stress;

namespace PCWarmer.Core.Screensavers
{
    /// <summary>Glowing sparks rising from the bottom of the screen, cooling from yellow-white to deep red as they fade.</summary>
    public sealed class EmbersScreensaver : IScreensaver
    {
        private static readonly Color Hot = new(255, 240, 180);
        private static readonly Color Cool = new(180, 20, 0);

        private readonly StressProfile profile;
        private Texture2D glow;
        private EmberField field;

        public EmbersScreensaver(StressLevel level = StressLevel.Medium)
        {
            profile = StressProfile.For(level);
        }

        public string Id => "embers";

        public int LiveParticles => field?.Count ?? 0;

        public void LoadContent(ContentManager content, GraphicsDevice device)
        {
            glow = GlowTexture.Create(device);
            Viewport viewport = device.Viewport;
            field = new EmberField(new Vector2(viewport.Width, viewport.Height), profile.ParticlesPerSecond, profile.MaxParticles, new Random());
        }

        public void Update(GameTime gameTime) => field?.Update(gameTime.ElapsedGameTime);

        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (field == null)
                return;

            var origin = new Vector2(GlowTexture.Size / 2f);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);
            foreach (Ember ember in field.Embers)
            {
                float heat = ember.Heat;
                Color color = Color.Lerp(Cool, Hot, heat) * heat;
                spriteBatch.Draw(glow, ember.Position, null, color, 0f, origin, 0.25f + 0.35f * heat, SpriteEffects.None, 0f);
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
