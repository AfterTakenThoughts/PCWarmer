using NUnit.Framework;
using PCWarmer.Core.Display;
using PCWarmer.Core.Settings;
using PCWarmer.Core.Stress;

namespace PCWarmer.Tests.Stress;

[TestFixture]
public class RunPlanTests
{
    // Custom settings that differ from every preset, to show presets ignore them.
    private static PCWarmerSettings CustomSettings() => new()
    {
        CpuLoad = StressLevel.High,
        CpuThreads = 1,
        FpsCap = FpsCap.Fps144,
        DurationMinutes = 120,
        FullScreen = false,
    };

    [TestCase(HardwareLoad.None, 0.0, FpsCap.Fps30, StressLevel.Low)]
    [TestCase(HardwareLoad.Low, 0.25, FpsCap.Fps30, StressLevel.Low)]
    [TestCase(HardwareLoad.Medium, 0.60, FpsCap.Fps60, StressLevel.Medium)]
    [TestCase(HardwareLoad.High, 1.00, FpsCap.Uncapped, StressLevel.High)]
    public void Preset_SetsTheLoadAndIgnoresCustomSettings(HardwareLoad load, double duty, FpsCap fps, StressLevel particles)
    {
        RunPlan plan = RunPlan.For(load, CustomSettings());

        Assert.That(plan.CpuDutyCycle, Is.EqualTo(duty));
        Assert.That(plan.CpuThreads, Is.EqualTo(0), "all cores");
        Assert.That(plan.FpsCap, Is.EqualTo(fps));
        Assert.That(plan.ScreensaverLevel, Is.EqualTo(particles));
    }

    [TestCase(HardwareLoad.None)]
    [TestCase(HardwareLoad.High)]
    public void Preset_UsesTheDefaultDurationAndDisplay(HardwareLoad load)
    {
        var defaults = new PCWarmerSettings();

        RunPlan plan = RunPlan.For(load, CustomSettings());

        Assert.That(plan.Duration, Is.EqualTo(TimeSpan.FromMinutes(defaults.DurationMinutes)));
        Assert.That(plan.FullScreen, Is.EqualTo(defaults.FullScreen));
    }

    [Test]
    public void Custom_UsesTheSettingsScreenOptions()
    {
        RunPlan plan = RunPlan.For(HardwareLoad.Custom, CustomSettings());

        Assert.That(plan, Is.EqualTo(new RunPlan(
            CpuDutyCycle: 1.0,
            CpuThreads: 1,
            FpsCap: FpsCap.Fps144,
            ScreensaverLevel: StressLevel.Medium,
            Duration: TimeSpan.FromMinutes(120),
            FullScreen: false)));
    }

    [Test]
    public void Custom_BadSavedValuesFallBack()
    {
        var settings = new PCWarmerSettings { CpuLoad = (StressLevel)99, DurationMinutes = 0 };

        RunPlan plan = RunPlan.For(HardwareLoad.Custom, settings);

        Assert.That(plan.CpuDutyCycle, Is.EqualTo(0.60), "unknown CPU load runs at Medium");
        Assert.That(plan.Duration, Is.Null, "0 minutes is unlimited");
    }

    [Test]
    public void UnknownLoad_RunsLikeMedium()
        => Assert.That(RunPlan.For((HardwareLoad)42, CustomSettings()),
            Is.EqualTo(RunPlan.For(HardwareLoad.Medium, CustomSettings())));

    [Test]
    public void CreateCpuLoad_None_StartsNoThreads()
    {
        ICpuLoad cpu = RunPlan.For(HardwareLoad.None, CustomSettings()).CreateCpuLoad();

        Assert.That(cpu, Is.Not.InstanceOf<CpuStressService>());
        Assert.That(cpu.ThreadCount, Is.EqualTo(0));
    }

    [Test]
    public void CreateCpuLoad_Preset_UsesEveryCore()
    {
        using var cpu = (CpuStressService)RunPlan.For(HardwareLoad.Medium, CustomSettings()).CreateCpuLoad();

        Assert.That(cpu.ThreadCount, Is.EqualTo(Environment.ProcessorCount));
    }

    [Test]
    public void CreateCpuLoad_Custom_UsesTheSavedThreadCount()
    {
        using var cpu = (CpuStressService)RunPlan.For(HardwareLoad.Custom, CustomSettings()).CreateCpuLoad();

        Assert.That(cpu.ThreadCount, Is.EqualTo(1));
    }
}
