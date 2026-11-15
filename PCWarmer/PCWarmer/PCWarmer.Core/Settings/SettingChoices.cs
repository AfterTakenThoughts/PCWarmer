using System;
using System.Collections.Generic;
using System.Linq;
using PCWarmer.Core.Display;
using PCWarmer.Core.Screensavers;
using PCWarmer.Core.Stress;

namespace PCWarmer.Core.Settings;

/// <summary>
/// The values each warming option steps through on the start screen, and how saved values become run
/// parameters. Saved values can be hand-edited, so anything unexpected falls back to a safe choice.
/// </summary>
public static class SettingChoices
{
    /// <summary>The intensity choices in the order the start screen shows them.</summary>
    public static IReadOnlyList<HardwareLoad> HardwareLoads { get; } =
        new[] { HardwareLoad.None, HardwareLoad.Low, HardwareLoad.Medium, HardwareLoad.High, HardwareLoad.Custom };

    /// <summary>Low, Medium and High: the choices for CPU load and screensaver detail.</summary>
    public static IReadOnlyList<StressLevel> Levels { get; } = new[] { StressLevel.Low, StressLevel.Medium, StressLevel.High };

    public static IReadOnlyList<FpsCap> FpsCaps { get; } = new[] { FpsCap.Fps60, FpsCap.Fps120, FpsCap.Uncapped };

    /// <summary>Run lengths in minutes; 0 means unlimited.</summary>
    public static IReadOnlyList<int> Durations { get; } = new[] { 0, 15, 30, 60, 120 };

    /// <summary>
    /// The choice after (<paramref name="direction"/> +1) or before (-1) <paramref name="current"/>, wrapping at both ends.
    /// An unknown <paramref name="current"/> gives the first choice.
    /// </summary>
    public static T Step<T>(IReadOnlyList<T> steps, T current, int direction)
    {
        int index = IndexOf(steps, current);
        if (index < 0)
            return steps[0];

        int count = steps.Count;
        return steps[((index + Math.Sign(direction)) % count + count) % count];
    }

    /// <summary>
    /// The thread count after or before <paramref name="threads"/>: 0 (all cores), then 1 up to <paramref name="cores"/>,
    /// wrapping at both ends. Out-of-range values give 0.
    /// </summary>
    public static int StepCpuThreads(int threads, int cores, int direction)
    {
        if (threads < 0 || threads > cores)
            return 0;

        int count = cores + 1;
        return ((threads + Math.Sign(direction)) % count + count) % count;
    }

    /// <summary>The next intensity in start-screen order; an unknown value restarts at None.</summary>
    public static HardwareLoad NextHardwareLoad(HardwareLoad load) => Step(HardwareLoads, load, 1);

    /// <summary>The next level (Low, Medium, High, then Low again); an unknown value restarts at Low.</summary>
    public static StressLevel NextCpuLoad(StressLevel level) => Step(Levels, level, 1);

    /// <summary>The next frame-rate cap; an undefined value restarts at 60.</summary>
    public static FpsCap NextFpsCap(FpsCap cap) => Step(FpsCaps, cap, 1);

    /// <summary>The next run length in minutes, where 0 is unlimited; an unknown value restarts at unlimited.</summary>
    public static int NextDuration(int minutes) => Step(Durations, minutes, 1);

    /// <summary>
    /// The next thread count: 0 (all cores), then 1 up to <paramref name="cores"/>, then back to 0.
    /// Out-of-range values restart at 0.
    /// </summary>
    public static int NextCpuThreads(int threads, int cores)
        => threads < 0 || threads >= cores ? 0 : threads + 1;

    /// <summary>The intensity a run uses: the saved one, or Medium if the saved value is unknown.</summary>
    public static HardwareLoad HardwareLoadOrDefault(HardwareLoad load) => IndexOf(HardwareLoads, load) >= 0 ? load : HardwareLoad.Medium;

    /// <summary>A CPU load or screensaver detail level a run uses: the saved level, or Medium if the saved value is unknown.</summary>
    public static StressLevel LevelOrDefault(StressLevel level) => IndexOf(Levels, level) >= 0 ? level : StressLevel.Medium;

    /// <summary>The frame-rate cap a run uses: the saved cap, or 60 if the saved value is unknown (such as the old 30 fps).</summary>
    public static FpsCap FpsCapOrDefault(FpsCap cap) => IndexOf(FpsCaps, cap) >= 0 ? cap : FpsCap.Fps60;

    /// <summary>The screensaver a run uses: the saved id if the catalog has it, otherwise the default (Particle Storm).</summary>
    public static string ScreensaverIdOrDefault(string id)
        => ScreensaverCatalog.Entries.Any(entry => entry.Id == id) ? id : ScreensaverCatalog.DefaultId;

    /// <summary>The run length for saved minutes; 0 or less means unlimited (null).</summary>
    public static TimeSpan? RunDuration(int minutes)
        => minutes > 0 ? TimeSpan.FromMinutes(minutes) : null;

    private static int IndexOf<T>(IReadOnlyList<T> steps, T value)
    {
        for (int i = 0; i < steps.Count; i++)
        {
            if (EqualityComparer<T>.Default.Equals(steps[i], value))
                return i;
        }

        return -1;
    }
}
