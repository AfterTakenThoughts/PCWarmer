using System;
using System.Collections.Generic;
using System.Linq;

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

        private static readonly (ScreensaverEntry Entry, Func<IScreensaver> Create)[] All =
        {
            (new ScreensaverEntry(DefaultId, "Particle Storm"), () => new ParticleStormScreensaver()),
            (new ScreensaverEntry("embers", "Embers"), () => new EmbersScreensaver()),
            (new ScreensaverEntry("starfield", "Starfield"), () => new StarfieldScreensaver()),
            (new ScreensaverEntry("bouncing-orbs", "Bouncing Orbs"), () => new BouncingOrbsScreensaver()),
        };

        /// <summary>The screensavers in the order the picker lists them.</summary>
        public static IReadOnlyList<ScreensaverEntry> Entries { get; } = All.Select(s => s.Entry).ToArray();

        /// <summary>A new screensaver for <paramref name="id"/>; unknown or null ids get the default.</summary>
        public static IScreensaver Create(string id)
        {
            foreach (var (entry, create) in All)
            {
                if (entry.Id == id)
                    return create();
            }

            return new ParticleStormScreensaver();
        }
    }
}
