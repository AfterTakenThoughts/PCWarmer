using System.Text.Json;
using NUnit.Framework;
using PCWarmer.Core.Display;
using PCWarmer.Core.Effects;
using PCWarmer.Core.Settings;
using PCWarmer.Core.Stress;

namespace PCWarmer.Tests.Settings;

[TestFixture]
public class PCWarmerSettingsTests
{
    [Test]
    public void NewSettings_UseTheSuggestedDefaults()
    {
        var settings = new PCWarmerSettings();

        Assert.That(settings.FullScreen, Is.True);
        Assert.That(settings.CpuLoad, Is.EqualTo(StressLevel.Medium));
        Assert.That(settings.FpsCap, Is.EqualTo(FpsCap.Fps60));
        Assert.That(settings.DurationMinutes, Is.EqualTo(30), "a forgotten run should stop on its own");
        Assert.That(settings.CpuThreads, Is.EqualTo(0), "0 means all cores");
        Assert.That(settings.ParticleEffect, Is.EqualTo(ParticleEffectType.Fireworks));
    }

    [Test]
    public void SavedUnlimitedDuration_IsKept()
    {
        var settings = JsonSerializer.Deserialize<PCWarmerSettings>("""{"DurationMinutes":0}""")!;

        Assert.That(settings.DurationMinutes, Is.EqualTo(0));
    }

    [Test]
    public void OldFileWithoutStressFields_LoadsWithDefaults()
    {
        var settings = JsonSerializer.Deserialize<PCWarmerSettings>("""{"FullScreen":true}""")!;

        Assert.That(settings.FullScreen, Is.True);
        Assert.That(settings.CpuLoad, Is.EqualTo(StressLevel.Medium));
        Assert.That(settings.FpsCap, Is.EqualTo(FpsCap.Fps60));
        Assert.That(settings.DurationMinutes, Is.EqualTo(30));
        Assert.That(settings.CpuThreads, Is.EqualTo(0), "0 means all cores");
    }

    [Test]
    public void StressFields_SurviveSaveAndLoad()
    {
        var saved = new PCWarmerSettings
        {
            CpuLoad = StressLevel.High,
            FpsCap = FpsCap.Uncapped,
            DurationMinutes = 30,
            CpuThreads = 2,
        };

        var loaded = JsonSerializer.Deserialize<PCWarmerSettings>(JsonSerializer.Serialize(saved))!;

        Assert.That(loaded.CpuLoad, Is.EqualTo(StressLevel.High));
        Assert.That(loaded.FpsCap, Is.EqualTo(FpsCap.Uncapped));
        Assert.That(loaded.DurationMinutes, Is.EqualTo(30));
        Assert.That(loaded.CpuThreads, Is.EqualTo(2));
    }

    [Test]
    public void FileFromThePercentVersion_LoadsWithTheDefaultLevel()
    {
        var settings = JsonSerializer.Deserialize<PCWarmerSettings>("""{"CpuLoadPercent":75,"DurationMinutes":30}""")!;

        Assert.That(settings.CpuLoad, Is.EqualTo(StressLevel.Medium));
        Assert.That(settings.DurationMinutes, Is.EqualTo(30));
    }
}
