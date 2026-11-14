using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using PCWarmer.Core.Stress;

namespace PCWarmer.Core.Screensavers
{
    /// <summary>Stars streaking outward from the center, getting brighter and longer as they speed up.</summary>
    public sealed class StarfieldScreensaver : IScreensaver
    {
        private const float StreakThickness = 0.12f;

        private readonly int starCount;
        private Texture2D glow;
        private Starfield field;
        private Vector2 center;
        private float maxDistance;

        public StarfieldScreensaver(StressLevel level = StressLevel.Medium)
        {
            // Every star is drawn as a streak every frame, so half the level's particle cap is plenty.
            starCount = StressProfile.For(level).MaxParticles / 2;
        }

        public string Id => "starfield";

        public int LiveParticles => field?.Count ?? 0;

        public void LoadContent(ContentManager content, GraphicsDevice device)
        {
            glow = GlowTexture.Create(device);
            Viewport viewport = device.Viewport;
            var size = new Vector2(viewport.Width, viewport.Height);
            center = size / 2f;
            maxDistance = center.Length();
            field = new Starfield(size, starCount, new Random());
        }

        public void Update(GameTime gameTime) => field?.Update(gameTime.ElapsedGameTime);

        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (field == null)
                return;

            var origin = new Vector2(GlowTexture.Size / 2f);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);
            foreach (Star star in field.Stars)
            {
                Vector2 streak = star.Position - star.PreviousPosition;
                float length = MathF.Max(streak.Length(), 2f);
                float brightness = MathHelper.Clamp(Vector2.Distance(star.Position, center) / maxDistance, 0.15f, 1f);
                float rotation = MathF.Atan2(star.Direction.Y, star.Direction.X);
                Vector2 midpoint = star.PreviousPosition + streak / 2f;
                var scale = new Vector2(length / GlowTexture.Size * 1.5f, StreakThickness + 0.2f * brightness);

                spriteBatch.Draw(glow, midpoint, null, Color.White * brightness, rotation, origin, scale, SpriteEffects.None, 0f);
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
