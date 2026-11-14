using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using PCWarmer.Core.Effects;
using PCWarmer.Core.Stress;

namespace PCWarmer.Core.Screensavers
{
    /// <summary>
    /// Bursts of glowing particles at random spots on the screen. The level's particle rate and cap
    /// set the GPU load, through an <see cref="EmissionBudget"/>.
    /// </summary>
    public sealed class ParticleStormScreensaver : IScreensaver
    {
        // Fireworks are left out: each dying firework particle spawns more particles, which would break the cap.
        private static readonly ParticleEffectType[] Effects =
        {
            ParticleEffectType.Confetti,
            ParticleEffectType.Explosions,
            ParticleEffectType.Sparkles,
        };

        private const int BurstSize = 50;

        private readonly Random random = new();
        private readonly EmissionBudget budget;
        private ParticleManager particles;
        private GraphicsDevice device;
        private int effectIndex;

        /// <param name="level">Sets the particle rate and cap, from <see cref="StressProfile.For"/>.</param>
        public ParticleStormScreensaver(StressLevel level = StressLevel.Medium)
        {
            StressProfile profile = StressProfile.For(level);
            budget = new EmissionBudget(profile.ParticlesPerSecond, profile.MaxParticles);
        }

        public string Id => "particle-storm";

        public int LiveParticles => particles?.ParticleCount ?? 0;

        public void LoadContent(ContentManager content, GraphicsDevice device)
        {
            this.device = device;
            particles = new ParticleManager(content.Load<Texture2D>("Sprites/blank"), Vector2.Zero);
        }

        public void Update(GameTime gameTime)
        {
            // Not loaded yet, or already unloaded by the host.
            if (particles == null)
                return;

            particles.Update(gameTime);

            int toEmit = budget.Next(gameTime.ElapsedGameTime, LiveParticles);
            Viewport viewport = device.Viewport;

            while (toEmit > 0)
            {
                int burst = Math.Min(BurstSize, toEmit);
                particles.Position = new Vector2(random.Next(viewport.Width), random.Next(viewport.Height));
                particles.Emit(burst, Effects[effectIndex]);

                effectIndex = (effectIndex + 1) % Effects.Length;
                toEmit -= burst;
            }
        }

        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (particles == null)
                return;

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);
            particles.Draw(spriteBatch);
            spriteBatch.End();
        }

        public void UnloadContent()
        {
            // The texture belongs to the ContentManager, which the host unloads.
            particles = null;
            device = null;
        }
    }
}
