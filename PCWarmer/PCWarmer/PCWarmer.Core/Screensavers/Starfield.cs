using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace PCWarmer.Core.Screensavers
{
    /// <summary>One star streaming away from the center.</summary>
    public struct Star
    {
        public Vector2 Position;

        /// <summary>Where it was last frame; the screensaver draws a streak between the two.</summary>
        public Vector2 PreviousPosition;

        /// <summary>Unit vector pointing away from the center.</summary>
        public Vector2 Direction;
    }

    /// <summary>
    /// The motion behind the Starfield screensaver: stars fly outward from the center, speeding up as they go,
    /// and any that leave the screen come back near the center. Has no graphics, so it can be tested.
    /// </summary>
    public sealed class Starfield
    {
        private const float BaseSpeed = 20f;

        /// <summary>Extra speed per pixel of distance from the center, which gives the warp-speed acceleration.</summary>
        private const float Acceleration = 1.5f;

        private const float RespawnRadius = 20f;

        private readonly Vector2 size;
        private readonly Vector2 center;
        private readonly Random random;
        private readonly Star[] stars;

        /// <param name="size">Screen size in pixels.</param>
        /// <param name="count">How many stars; constant for the field's life.</param>
        public Starfield(Vector2 size, int count, Random random)
        {
            this.size = size;
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            center = size / 2f;
            stars = new Star[Math.Max(0, count)];

            // Start with stars spread over the whole screen, so it isn't empty for the first seconds.
            for (int i = 0; i < stars.Length; i++)
            {
                var position = new Vector2(NextFloat(0, size.X), NextFloat(0, size.Y));
                stars[i] = new Star { Position = position, PreviousPosition = position, Direction = DirectionFrom(position) };
            }
        }

        public IReadOnlyList<Star> Stars => stars;

        public int Count => stars.Length;

        public void Update(TimeSpan delta)
        {
            float seconds = (float)delta.TotalSeconds;

            for (int i = 0; i < stars.Length; i++)
            {
                ref Star star = ref stars[i];
                float distance = Vector2.Distance(star.Position, center);

                star.PreviousPosition = star.Position;
                star.Position += star.Direction * (BaseSpeed + distance * Acceleration) * seconds;

                if (star.Position.X < 0 || star.Position.X > size.X || star.Position.Y < 0 || star.Position.Y > size.Y)
                    Respawn(ref star);
            }
        }

        private void Respawn(ref Star star)
        {
            float angle = NextFloat(0, MathHelper.TwoPi);
            star.Direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            star.Position = center + star.Direction * NextFloat(0, RespawnRadius);
            star.PreviousPosition = star.Position;
        }

        private Vector2 DirectionFrom(Vector2 position)
        {
            Vector2 offset = position - center;
            if (offset == Vector2.Zero)
            {
                float angle = NextFloat(0, MathHelper.TwoPi);
                return new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            }

            offset.Normalize();
            return offset;
        }

        private float NextFloat(float min, float max) => min + (float)random.NextDouble() * (max - min);
    }
}
