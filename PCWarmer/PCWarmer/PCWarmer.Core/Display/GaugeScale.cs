using System;

namespace PCWarmer.Core.Display
{
    /// <summary>How full the dashboard's gauges are.</summary>
    public static class GaugeScale
    {
        /// <summary>How far round a full gauge goes, from bottom left over the top to bottom right.</summary>
        public const float SweepDegrees = 270f;

        /// <summary><paramref name="value"/> as a fraction of <paramref name="full"/>, clamped to 0..1; 0 when full isn't positive.</summary>
        public static float Fraction(double value, double full)
            => full <= 0 ? 0f : (float)Math.Clamp(value / full, 0, 1);

        /// <summary>The frame rate that fills the FPS gauge: the cap, or 240 for Uncapped. Undefined caps count as 60.</summary>
        public static double FpsFull(FpsCap cap) => SettingsFallback(cap) switch
        {
            FpsCap.Fps120 => 120,
            FpsCap.Uncapped => 240,
            _ => 60,
        };

        private static FpsCap SettingsFallback(FpsCap cap) => Settings.SettingChoices.FpsCapOrDefault(cap);
    }
}
