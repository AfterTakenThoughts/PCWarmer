using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace PCWarmer.Core.Screensavers
{
    /// <summary>One glowing spark.</summary>
    public struct Ember
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Age;
        public float Lifetime;

        /// <summary>1 when new, falling to 0 as it burns out.</summary>
        public readonly float Heat => MathHelper.Clamp(1f - Age / Lifetime, 0f, 1f);
    }

    /// <summary>
    /// The motion behind the Embers screensaver: sparks emitted along the bottom edge that drift upward
    /// and are removed once they burn out or leave the top. Has no graphics, so it can be tested.
    /// </summary>
    public sealed class EmberField
    {
        private const float MinRiseSpeed = 60f;
        private const float MaxRiseSpeed = 160f;
        private const float MaxDrift = 20f;
        private const float MinLifetime = 2f;
        private const float MaxLifetime = 4f;

        private readonly Vector2 size;
        private readonly Random random;
        private readonly EmissionBudget budget;
        private readonly List<Ember> embers = new();

        /// <param name="size">Screen size in pixels; embers start on its bottom edge.</param>
        public EmberField(Vector2 size, int particlesPerSecond, int maxParticles, Random random)
        {
            this.size = size;
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            budget = new EmissionBudget(particlesPerSecond, maxParticles);
        }

        public IReadOnlyList<Ember> Embers => embers;

        public int Count => embers.Count;

        public void Update(TimeSpan delta)
        {
            float seconds = (float)delta.TotalSeconds;

            for (int i = embers.Count - 1; i >= 0; i--)
            {
                Ember ember = embers[i];
                ember.Position += ember.Velocity * seconds;
                ember.Age += seconds;

                if (ember.Age >= ember.Lifetime || ember.Position.Y < 0)
                {
                    // Swap-remove: O(1), and order doesn't matter when drawing additive sparks.
                    embers[i] = embers[^1];
                    embers.RemoveAt(embers.Count - 1);
                }
                else
                {
                    embers[i] = ember;
                }
            }

            int toEmit = budget.Next(delta, embers.Count);
            for (int i = 0; i < toEmit; i++)
                embers.Add(Spawn());
        }

        private Ember Spawn() => new()
        {
            Position = new Vector2(NextFloat(0, size.X), size.Y),
            Velocity = new Vector2(NextFloat(-MaxDrift, MaxDrift), -NextFloat(MinRiseSpeed, MaxRiseSpeed)),
            Lifetime = NextFloat(MinLifetime, MaxLifetime),
        };

        private float NextFloat(float min, float max) => min + (float)random.NextDouble() * (max - min);
    }
}
