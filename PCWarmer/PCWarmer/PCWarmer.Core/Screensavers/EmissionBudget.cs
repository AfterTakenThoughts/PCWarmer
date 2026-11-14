using System;

namespace PCWarmer.Core.Screensavers
{
    /// <summary>
    /// Decides how many particles a screensaver emits each frame: a steady rate per second,
    /// never more than the cap on live particles. Keeps memory bounded however high the frame rate goes.
    /// </summary>
    public sealed class EmissionBudget
    {
        private readonly int particlesPerSecond;
        private readonly int maxParticles;
        private double carry;

        public EmissionBudget(int particlesPerSecond, int maxParticles)
        {
            this.particlesPerSecond = Math.Max(0, particlesPerSecond);
            this.maxParticles = Math.Max(0, maxParticles);
        }

        /// <summary>Particles to emit for a frame that took <paramref name="delta"/>, with <paramref name="liveParticles"/> already alive.</summary>
        public int Next(TimeSpan delta, int liveParticles)
        {
            int room = Math.Max(0, maxParticles - liveParticles);
            double due = carry + particlesPerSecond * Math.Max(0, delta.TotalSeconds);
            int whole = (int)Math.Min(due, int.MaxValue);

            if (whole >= room)
            {
                // Capped: drop the overflow so it isn't emitted in one burst when room opens up.
                carry = 0;
                return room;
            }

            carry = due - whole;
            return whole;
        }
    }
}
