using System;
using PCWarmer.Core.Display;
using PCWarmer.Core.Stress;

namespace PCWarmer.Core.Settings;

/// <summary>
/// The values each warming option steps through on the settings screen, and how saved values become run
/// parameters. Saved values can be hand-edited, so anything unexpected falls back to a safe choice.
/// </summary>
public static class SettingChoices
{
    private static readonly HardwareLoad[] HardwareLoadSteps =
        { HardwareLoad.Custom, HardwareLoad.None, HardwareLoad.Low, HardwareLoad.Medium, HardwareLoad.High };

    private static readonly StressLevel[] CpuLoadSteps = { StressLevel.Low, StressLevel.Medium, StressLevel.High };
    private static readonly FpsCap[] FpsCapSteps = { FpsCap.Fps30, FpsCap.Fps60, FpsCap.Fps144, FpsCap.Uncapped };

    /// <summary>0 means unlimited.</summary>
    private static readonly int[] DurationSteps = { 0, 15, 30, 60, 120 };

    /// <summary>The next hardware load in menu order (Custom, None, Low, Medium, High); an unknown value restarts at Custom.</summary>
    public static HardwareLoad NextHardwareLoad(HardwareLoad load) => Next(HardwareLoadSteps, load);

    /// <summary>The next CPU load level (Low, Medium, High, then Low again); an unknown value restarts at Low.</summary>
    public static StressLevel NextCpuLoad(StressLevel level) => Next(CpuLoadSteps, level);

    /// <summary>The CPU load level a run uses: the saved level, or Medium if the saved value is unknown.</summary>
    public static StressLevel CpuLoadOrDefault(StressLevel level)
        => Array.IndexOf(CpuLoadSteps, level) >= 0 ? level : StressLevel.Medium;

    /// <summary>The next frame-rate cap; an undefined value restarts at the first step.</summary>
    public static FpsCap NextFpsCap(FpsCap cap) => Next(FpsCapSteps, cap);

    /// <summary>The next run length in minutes, where 0 is unlimited; an unknown value restarts at unlimited.</summary>
    public static int NextDuration(int minutes) => Next(DurationSteps, minutes);

    /// <summary>
    /// The next thread count: 0 (all cores), then 1 up to <paramref name="cores"/>, then back to 0.
    /// Out-of-range values restart at 0.
    /// </summary>
    public static int NextCpuThreads(int threads, int cores)
        => threads < 0 || threads >= cores ? 0 : threads + 1;

    /// <summary>The run length for saved minutes; 0 or less means unlimited (null).</summary>
    public static TimeSpan? RunDuration(int minutes)
        => minutes > 0 ? TimeSpan.FromMinutes(minutes) : null;

    private static T Next<T>(T[] steps, T current)
    {
        int index = Array.IndexOf(steps, current);
        return index < 0 ? steps[0] : steps[(index + 1) % steps.Length];
    }
}
