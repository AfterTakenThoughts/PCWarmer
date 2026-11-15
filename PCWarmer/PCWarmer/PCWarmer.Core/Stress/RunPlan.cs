using System;
using PCWarmer.Core.Display;
using PCWarmer.Core.Settings;

namespace PCWarmer.Core.Stress
{
    /// <summary>Everything a warming run needs, worked out from the picked intensity and the saved settings.</summary>
    /// <param name="CpuDutyCycle">Fraction of each time window the CPU workers spend busy; 0 means no CPU load at all.</param>
    /// <param name="CpuThreads">CPU worker threads; 0 means one per core.</param>
    /// <param name="FpsCap">The screensaver's frame-rate cap.</param>
    /// <param name="ScreensaverLevel">Sets how many particles, stars or orbs the screensaver draws.</param>
    /// <param name="Duration">How long the run lasts; null means unlimited.</param>
    /// <param name="FullScreen">Whether the screensaver covers the screen.</param>
    public sealed record RunPlan(
        double CpuDutyCycle,
        int CpuThreads,
        FpsCap FpsCap,
        StressLevel ScreensaverLevel,
        TimeSpan? Duration,
        bool FullScreen)
    {
        /// <summary>
        /// None, Low, Medium and High are presets that set the CPU load, threads, frame rate and screensaver detail.
        /// Custom uses the saved Custom options. Every intensity takes its run length and display mode from the saved
        /// settings. An unknown intensity runs like Medium.
        /// </summary>
        /// <param name="cores">The machine's core count (<see cref="Environment.ProcessorCount"/> in production).</param>
        public static RunPlan For(HardwareLoad load, PCWarmerSettings settings, int cores)
        {
            ArgumentNullException.ThrowIfNull(settings);

            TimeSpan? duration = SettingChoices.RunDuration(settings.DurationMinutes);
            bool fullScreen = settings.FullScreen;
            cores = Math.Max(1, cores);

            return SettingChoices.HardwareLoadOrDefault(load) switch
            {
                HardwareLoad.Custom => new RunPlan(
                    StressProfile.For(SettingChoices.LevelOrDefault(settings.CpuLoad)).CpuDutyCycle,
                    settings.CpuThreads,
                    SettingChoices.FpsCapOrDefault(settings.FpsCap),
                    SettingChoices.LevelOrDefault(settings.ScreensaverLevel),
                    duration,
                    fullScreen),
                HardwareLoad.None => new RunPlan(0, 0, FpsCap.Fps60, StressLevel.Low, duration, fullScreen),
                HardwareLoad.Low => Preset(StressLevel.Low, Math.Max(1, cores / 2), FpsCap.Fps60, duration, fullScreen),
                HardwareLoad.High => Preset(StressLevel.High, cores, FpsCap.Uncapped, duration, fullScreen),
                // Three-quarters of the cores, rounded down, leaves at least one free for other apps on 2+ cores.
                _ => Preset(StressLevel.Medium, Math.Max(1, cores * 3 / 4), FpsCap.Fps120, duration, fullScreen),
            };
        }

        /// <summary>The CPU load for this run: nothing at all when the duty cycle is 0, otherwise worker threads.</summary>
        public ICpuLoad CreateCpuLoad()
            => CpuDutyCycle > 0 ? new CpuStressService(CpuThreads) : new NoCpuLoad();

        private static RunPlan Preset(StressLevel level, int threads, FpsCap fpsCap, TimeSpan? duration, bool fullScreen)
            => new(StressProfile.For(level).CpuDutyCycle, threads, fpsCap, level, duration, fullScreen);
    }
}
