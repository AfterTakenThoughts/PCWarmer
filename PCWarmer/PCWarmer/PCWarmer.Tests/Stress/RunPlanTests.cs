using System;
using NUnit.Framework;
using PCWarmer.Core.Display;
using PCWarmer.Core.Settings;
using PCWarmer.Core.Stress;

namespace PCWarmer.Tests.Stress;

[TestFixture]
public class RunPlanTests
{
    // Saved settings that differ from every preset, to show what presets ignore and what they keep.
    private static PCWarmerSettings Saved() => new()
    {
        CpuLoad = StressLevel.High,
        CpuThreads = 1,
        FpsCap = FpsCap.Uncapped,
        ScreensaverLevel = StressLevel.Low,
        DurationMinutes = 120,
        FullScreen = false,
    };

    [TestCase(HardwareLoad.None, 0.0, 0, FpsCap.Fps60, StressLevel.Low)]
    [TestCase(HardwareLoad.Low, 0.25, 4, FpsCap.Fps60, StressLevel.Low)]
    [TestCase(HardwareLoad.Medium, 0.60, 6, FpsCap.Fps120, StressLevel.Medium)]
    [TestCase(HardwareLoad.High, 1.00, 8, FpsCap.Uncapped, StressLevel.High)]
    public void Preset_OnEightCores(HardwareLoad load, double duty, int threads, FpsCap fps, StressLevel level)
    {
        RunPlan plan = RunPlan.For(load, Saved(), cores: 8);

        Assert.That((plan.CpuDutyCycle, plan.CpuThreads, plan.FpsCap, plan.ScreensaverLevel),
            Is.EqualTo((duty, threads, fps, level)));
    }

    [TestCase(1, 1, 1, 1)]
    [TestCase(2, 1, 1, 2)]
    [TestCase(3, 1, 2, 3)]
    [TestCase(4, 2, 3, 4)]
    public void PresetThreads_NeverZeroAndMediumLeavesACoreFree(int cores, int low, int medium, int high)
    {
        Assert.That(RunPlan.For(HardwareLoad.Low, Saved(), cores).CpuThreads, Is.EqualTo(low));
        Assert.That(RunPlan.For(HardwareLoad.Medium, Saved(), cores).CpuThreads, Is.EqualTo(medium));
        Assert.That(RunPlan.For(HardwareLoad.High, Saved(), cores).CpuThreads, Is.EqualTo(high));
    }

    [TestCase(HardwareLoad.None)]
    [TestCase(HardwareLoad.High)]
    [TestCase(HardwareLoad.Custom)]
    public void EveryLoad_UsesSavedRunLengthAndDisplay(HardwareLoad load)
    {
        RunPlan plan = RunPlan.For(load, Saved(), cores: 8);

        Assert.That(plan.Duration, Is.EqualTo(TimeSpan.FromMinutes(120)));
        Assert.That(plan.FullScreen, Is.False);
    }

    [Test]
    public void Custom_UsesItsOwnScreensaverLevel()
        => Assert.That(RunPlan.For(HardwareLoad.Custom, Saved(), cores: 8),
            Is.EqualTo(new RunPlan(1.0, 1, FpsCap.Uncapped, StressLevel.Low, TimeSpan.FromMinutes(120), false)));

    [Test]
    public void Custom_BadSavedValuesFallBack()
    {
        var settings = new PCWarmerSettings
        {
            CpuLoad = (StressLevel)99,
            ScreensaverLevel = (StressLevel)99,
            FpsCap = 0,
            DurationMinutes = 0,
        };

        RunPlan plan = RunPlan.For(HardwareLoad.Custom, settings, cores: 8);

        Assert.That(plan.CpuDutyCycle, Is.EqualTo(0.60), "unknown CPU load runs at Medium");
        Assert.That(plan.ScreensaverLevel, Is.EqualTo(StressLevel.Medium), "unknown detail runs at Medium");
        Assert.That(plan.FpsCap, Is.EqualTo(FpsCap.Fps60), "the old 30 fps runs at 60");
        Assert.That(plan.Duration, Is.Null, "0 minutes is unlimited");
    }

    [Test]
    public void UnknownLoad_RunsAsMedium()
        => Assert.That(RunPlan.For((HardwareLoad)99, Saved(), cores: 8),
            Is.EqualTo(RunPlan.For(HardwareLoad.Medium, Saved(), cores: 8)));

    [Test]
    public void CreateCpuLoad_None_StartsNoThreads()
    {
        ICpuLoad cpu = RunPlan.For(HardwareLoad.None, Saved(), cores: 8).CreateCpuLoad();

        Assert.That(cpu, Is.Not.InstanceOf<CpuStressService>());
        Assert.That(cpu.ThreadCount, Is.EqualTo(0));
    }

    [Test]
    public void CreateCpuLoad_High_UsesEveryCore()
    {
        using var cpu = (CpuStressService)RunPlan.For(HardwareLoad.High, Saved(), Environment.ProcessorCount).CreateCpuLoad();

        Assert.That(cpu.ThreadCount, Is.EqualTo(Environment.ProcessorCount));
    }

    [Test]
    public void CreateCpuLoad_Custom_UsesTheSavedThreadCount()
    {
        using var cpu = (CpuStressService)RunPlan.For(HardwareLoad.Custom, Saved(), cores: 8).CreateCpuLoad();

        Assert.That(cpu.ThreadCount, Is.EqualTo(1));
    }
}
