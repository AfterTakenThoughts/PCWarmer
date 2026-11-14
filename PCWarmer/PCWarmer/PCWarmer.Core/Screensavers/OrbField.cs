using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace PCWarmer.Core.Screensavers
{
    /// <summary>One glowing ball.</summary>
    public struct Orb
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Radius;
        public Color Color;
    }

    /// <summary>
    /// The motion behind the Bouncing Orbs screensaver: orbs travel in straight lines and bounce off the screen
    /// edges, so they never leave it. Has no graphics, so it can be tested.
    /// </summary>
    public sealed class OrbField
    {
        private readonly Vector2 size;
        private readonly Orb[] orbs;

        /// <param name="size">Screen size in pixels.</param>
        /// <param name="orbs">The orbs to move.</param>
        public OrbField(Vector2 size, IEnumerable<Orb> orbs)
        {
            this.size = size;
            this.orbs = (orbs ?? throw new ArgumentNullException(nameof(orbs))).ToArray();
        }

        /// <summary>A field of <paramref name="count"/> orbs with random sizes, colors, positions and directions.</summary>
        public static OrbField CreateRandom(Vector2 size, int count, Random random)
        {
            ArgumentNullException.ThrowIfNull(random);
            float Next(float min, float max) => min + (float)random.NextDouble() * (max - min);

            var orbs = new Orb[Math.Max(0, count)];
            for (int i = 0; i < orbs.Length; i++)
            {
                float radius = Next(4, 12);
                float angle = Next(0, MathHelper.TwoPi);
                float speed = Next(50, 250);
                orbs[i] = new Orb
                {
                    Radius = radius,
                    Position = new Vector2(Next(radius, size.X - radius), Next(radius, size.Y - radius)),
                    Velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed,
                    Color = new Color(Next(0.3f, 1f), Next(0.3f, 1f), Next(0.3f, 1f)),
                };
            }

            return new OrbField(size, orbs);
        }

        public IReadOnlyList<Orb> Orbs => orbs;

        public int Count => orbs.Length;

        public void Update(TimeSpan delta)
        {
            float seconds = (float)delta.TotalSeconds;

            for (int i = 0; i < orbs.Length; i++)
            {
                ref Orb orb = ref orbs[i];
                orb.Position += orb.Velocity * seconds;

                Bounce(ref orb.Position.X, ref orb.Velocity.X, orb.Radius, size.X - orb.Radius);
                Bounce(ref orb.Position.Y, ref orb.Velocity.Y, orb.Radius, size.Y - orb.Radius);
            }
        }

        /// <summary>Reflects a coordinate that went past <paramref name="min"/> or <paramref name="max"/> back inside, reversing its velocity.</summary>
        private static void Bounce(ref float position, ref float velocity, float min, float max)
        {
            if (position < min)
            {
                position = 2 * min - position;
                velocity = MathF.Abs(velocity);
            }
            else if (position > max)
            {
                position = 2 * max - position;
                velocity = -MathF.Abs(velocity);
            }

            // A very long frame can overshoot by more than the whole width; keep it inside regardless.
            position = MathHelper.Clamp(position, min, max);
        }
    }
}
