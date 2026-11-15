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

    [Test]
    public void ResetToDefaults_RestoresEveryOptionOnTheSettingsScreen()
    {
        var settings = new PCWarmerSettings
        {
            CpuLoad = StressLevel.High,
            FpsCap = FpsCap.Uncapped,
            DurationMinutes = 120,
            CpuThreads = 3,
            FullScreen = false,
        };

        settings.ResetToDefaults();

        Assert.That(settings.CpuLoad, Is.EqualTo(StressLevel.Medium));
        Assert.That(settings.FpsCap, Is.EqualTo(FpsCap.Fps60));
        Assert.That(settings.DurationMinutes, Is.EqualTo(30));
        Assert.That(settings.CpuThreads, Is.EqualTo(0));
        Assert.That(settings.FullScreen, Is.True);
    }

    [Test]
    public void ResetToDefaults_LeavesTheLanguageAlone()
    {
        // Language isn't on the settings screen; resetting it would switch the UI culture behind the user's back.
        var settings = new PCWarmerSettings { Language = 0 };

        settings.ResetToDefaults();

        Assert.That(settings.Language, Is.EqualTo(0));
    }

    [Test]
    public void ResetToDefaults_ReportsEachChangedSetting()
    {
        var settings = new PCWarmerSettings { CpuLoad = StressLevel.High, DurationMinutes = 60 };
        var changed = new List<string?>();
        settings.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        settings.ResetToDefaults();

        Assert.That(changed, Is.EquivalentTo(new[] { "CpuLoad", "DurationMinutes" }));
    }

    [Test]
    public void HardwareLoad_DefaultsToMediumAndSurvivesSaveAndLoad()
    {
        Assert.That(new PCWarmerSettings().HardwareLoad, Is.EqualTo(HardwareLoad.Medium));

        var loaded = JsonSerializer.Deserialize<PCWarmerSettings>(
            JsonSerializer.Serialize(new PCWarmerSettings { HardwareLoad = HardwareLoad.Custom }))!;

        Assert.That(loaded.HardwareLoad, Is.EqualTo(HardwareLoad.Custom));
    }

    [Test]
    public void ResetToDefaults_LeavesTheHardwareLoadAlone()
    {
        // The hardware load is picked on the screensaver screen, not the settings screen.
        var settings = new PCWarmerSettings { HardwareLoad = HardwareLoad.Custom };

        settings.ResetToDefaults();

        Assert.That(settings.HardwareLoad, Is.EqualTo(HardwareLoad.Custom));
    }

    [Test]
    public void NewSettings_PickParticleStormAtMediumDetail()
    {
        var settings = new PCWarmerSettings();

        Assert.That(settings.ScreensaverId, Is.EqualTo("particle-storm"));
        Assert.That(settings.ScreensaverLevel, Is.EqualTo(StressLevel.Medium));
        Assert.That(settings.FpsCap, Is.EqualTo(FpsCap.Fps60));
    }

    [Test]
    public void FileWithoutNewFields_LoadsWithDefaults()
    {
        var settings = JsonSerializer.Deserialize<PCWarmerSettings>("""{"FpsCap":0,"HardwareLoad":2}""")!;

        Assert.That(settings.ScreensaverId, Is.EqualTo("particle-storm"));
        Assert.That(SettingChoices.FpsCapOrDefault(settings.FpsCap), Is.EqualTo(FpsCap.Fps60));
    }

    [Test]
    public void NewFields_SurviveSaveAndLoad()
    {
        var saved = new PCWarmerSettings { ScreensaverId = "embers", ScreensaverLevel = StressLevel.High };

        var loaded = JsonSerializer.Deserialize<PCWarmerSettings>(JsonSerializer.Serialize(saved))!;

        Assert.That(loaded.ScreensaverId, Is.EqualTo("embers"));
        Assert.That(loaded.ScreensaverLevel, Is.EqualTo(StressLevel.High));
    }

    [Test]
    public void ResetToDefaults_ResetsDetailButKeepsScreensaverAndIntensity()
    {
        var settings = new PCWarmerSettings
        {
            ScreensaverLevel = StressLevel.High,
            ScreensaverId = "embers",
            HardwareLoad = HardwareLoad.High,
        };

        settings.ResetToDefaults();

        Assert.That(settings.ScreensaverLevel, Is.EqualTo(StressLevel.Medium));
        Assert.That(settings.ScreensaverId, Is.EqualTo("embers"));
        Assert.That(settings.HardwareLoad, Is.EqualTo(HardwareLoad.High));
    }
}
