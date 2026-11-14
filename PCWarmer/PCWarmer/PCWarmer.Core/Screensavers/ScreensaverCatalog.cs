using System;
using System.Collections.Generic;
using System.Linq;
using PCWarmer.Core.Stress;

namespace PCWarmer.Core.Screensavers
{
    /// <summary>A screensaver the user can pick.</summary>
    /// <param name="Id">Stable id, used to create it.</param>
    /// <param name="Name">What the picker shows.</param>
    public sealed record ScreensaverEntry(string Id, string Name);

    /// <summary>Every screensaver PCWarmer offers. Creating one never touches the GPU; that happens in its LoadContent.</summary>
    public static class ScreensaverCatalog
    {
        public const string DefaultId = "particle-storm";

        private static readonly (ScreensaverEntry Entry, Func<StressLevel, IScreensaver> Create)[] All =
        {
            (new ScreensaverEntry(DefaultId, "Particle Storm"), level => new ParticleStormScreensaver(level)),
            (new ScreensaverEntry("embers", "Embers"), level => new EmbersScreensaver(level)),
            (new ScreensaverEntry("starfield", "Starfield"), level => new StarfieldScreensaver(level)),
            (new ScreensaverEntry("bouncing-orbs", "Bouncing Orbs"), level => new BouncingOrbsScreensaver(level)),
        };

        /// <summary>The screensavers in the order the picker lists them.</summary>
        public static IReadOnlyList<ScreensaverEntry> Entries { get; } = All.Select(s => s.Entry).ToArray();

        /// <summary>A new screensaver for <paramref name="id"/>; unknown or null ids get the default.</summary>
        /// <param name="level">Sets how many particles it draws.</param>
        public static IScreensaver Create(string id, StressLevel level = StressLevel.Medium)
        {
            foreach (var (entry, create) in All)
            {
                if (entry.Id == id)
                    return create(level);
            }

            return new ParticleStormScreensaver(level);
        }
    }
}
