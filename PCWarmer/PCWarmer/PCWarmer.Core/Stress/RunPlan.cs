using System;
using PCWarmer.Core.Display;
using PCWarmer.Core.Settings;

namespace PCWarmer.Core.Stress
{
    /// <summary>Everything a warming run needs, worked out from the picked hardware load and the saved settings.</summary>
    /// <param name="CpuDutyCycle">Fraction of each time window the CPU workers spend busy; 0 means no CPU load at all.</param>
    /// <param name="CpuThreads">CPU worker threads; 0 means one per core.</param>
    /// <param name="FpsCap">The screensaver's frame-rate cap.</param>
    /// <param name="ScreensaverLevel">Sets how many particles the screensaver draws.</param>
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
        /// None, Low, Medium and High are presets that ignore the settings screen (duration and display use the defaults).
        /// Custom uses the settings screen's options. An unknown load runs like Medium.
        /// </summary>
        public static RunPlan For(HardwareLoad load, PCWarmerSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            return load switch
            {
                HardwareLoad.Custom => new RunPlan(
                    StressProfile.For(SettingChoices.CpuLoadOrDefault(settings.CpuLoad)).CpuDutyCycle,
                    settings.CpuThreads,
                    settings.FpsCap,
                    StressLevel.Medium,
                    SettingChoices.RunDuration(settings.DurationMinutes),
                    settings.FullScreen),
                HardwareLoad.None => Preset(cpuDutyCycle: 0, FpsCap.Fps30, StressLevel.Low),
                HardwareLoad.Low => Preset(StressLevel.Low, FpsCap.Fps30),
                HardwareLoad.High => Preset(StressLevel.High, FpsCap.Uncapped),
                _ => Preset(StressLevel.Medium, FpsCap.Fps60),
            };
        }

        /// <summary>The CPU load for this run: nothing at all when the duty cycle is 0, otherwise worker threads.</summary>
        public ICpuLoad CreateCpuLoad()
            => CpuDutyCycle > 0 ? new CpuStressService(CpuThreads) : new NoCpuLoad();

        private static RunPlan Preset(StressLevel level, FpsCap fpsCap)
            => Preset(StressProfile.For(level).CpuDutyCycle, fpsCap, level);

        private static RunPlan Preset(double cpuDutyCycle, FpsCap fpsCap, StressLevel screensaverLevel)
        {
            var defaults = new PCWarmerSettings();
            return new RunPlan(
                cpuDutyCycle,
                CpuThreads: 0,
                fpsCap,
                screensaverLevel,
                SettingChoices.RunDuration(defaults.DurationMinutes),
                defaults.FullScreen);
        }
    }
}
