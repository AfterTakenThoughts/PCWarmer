using System.Linq;
using NUnit.Framework;
using PCWarmer.Core.Settings;
using PCWarmer.Core.Stress;
using PCWarmer.Core.Ui;

namespace PCWarmer.Tests.Ui;

[TestFixture]
public class StartMenuModelTests
{
    private static StartMenuModel Model(HardwareLoad load = HardwareLoad.Medium)
        => new(new PCWarmerSettings { HardwareLoad = load }, cores: 8);

    [Test]
    public void Opens_FocusedOnStart() => Assert.That(Model().Focused, Is.EqualTo(StartMenuRow.Start));

    [Test]
    public void Preset_HidesCustomRows()
        => Assert.That(Model().VisibleRows, Is.EqualTo(new[]
        {
            StartMenuRow.Intensity, StartMenuRow.Screensaver, StartMenuRow.Start, StartMenuRow.RunLength,
            StartMenuRow.Display, StartMenuRow.ResetSettings, StartMenuRow.Exit,
        }));

    [Test]
    public void Custom_ShowsCustomRowsAfterIntensity()
        => Assert.That(Model(HardwareLoad.Custom).VisibleRows.Take(5), Is.EqualTo(new[]
        {
            StartMenuRow.Intensity, StartMenuRow.CpuLoad, StartMenuRow.ScreensaverDetail, StartMenuRow.CpuThreads,
            StartMenuRow.FrameRate,
        }));

    [Test]
    public void LeavingCustom_MovesFocusOffAHiddenRow()
    {
        var model = Model(HardwareLoad.Custom);
        model.FocusRow(StartMenuRow.CpuThreads);

        model.SetIntensity(HardwareLoad.High);

        Assert.That(model.Focused, Is.EqualTo(StartMenuRow.Intensity));
    }

    [Test]
    public void FocusRow_OnAHiddenRow_IsIgnored()
    {
        var model = Model();

        model.FocusRow(StartMenuRow.CpuLoad);

        Assert.That(model.Focused, Is.EqualTo(StartMenuRow.Start));
    }

    [Test]
    public void MoveFocus_Wraps()
    {
        var model = Model();
        model.FocusRow(StartMenuRow.Exit);

        model.MoveFocus(1);

        Assert.That(model.Focused, Is.EqualTo(StartMenuRow.Intensity));
    }

    [Test]
    public void ChangeValue_OnIntensity_StepsAndReportsChange()
    {
        var settings = new PCWarmerSettings { HardwareLoad = HardwareLoad.High };
        var model = new StartMenuModel(settings, 8);
        model.FocusRow(StartMenuRow.Intensity);

        Assert.That(model.ChangeValue(1), Is.True);
        Assert.That(settings.HardwareLoad, Is.EqualTo(HardwareLoad.Custom));
    }

    [Test]
    public void ChangeValue_StepsEachSetting()
    {
        var settings = new PCWarmerSettings { HardwareLoad = HardwareLoad.Custom };
        var model = new StartMenuModel(settings, 8);

        model.FocusRow(StartMenuRow.CpuLoad); model.ChangeValue(1);
        model.FocusRow(StartMenuRow.ScreensaverDetail); model.ChangeValue(-1);
        model.FocusRow(StartMenuRow.CpuThreads); model.ChangeValue(-1);
        model.FocusRow(StartMenuRow.FrameRate); model.ChangeValue(1);
        model.FocusRow(StartMenuRow.Screensaver); model.ChangeValue(1);
        model.FocusRow(StartMenuRow.RunLength); model.ChangeValue(1);
        model.FocusRow(StartMenuRow.Display); model.ChangeValue(1);

        Assert.That(settings.CpuLoad, Is.EqualTo(StressLevel.High));
        Assert.That(settings.ScreensaverLevel, Is.EqualTo(StressLevel.Low));
        Assert.That(settings.CpuThreads, Is.EqualTo(8));
        Assert.That(settings.FpsCap, Is.EqualTo(Core.Display.FpsCap.Fps120));
        Assert.That(settings.ScreensaverId, Is.EqualTo("embers"));
        Assert.That(settings.DurationMinutes, Is.EqualTo(60));
        Assert.That(settings.FullScreen, Is.False);
    }

    [Test]
    public void ChangeValue_OnStart_DoesNothing() => Assert.That(Model().ChangeValue(1), Is.False);

    [Test]
    public void Activate_MapsCommands()
    {
        var model = Model();

        Assert.That(model.Activate(), Is.EqualTo(StartMenuCommand.StartRun));
        model.FocusRow(StartMenuRow.Exit);
        Assert.That(model.Activate(), Is.EqualTo(StartMenuCommand.Exit));
        model.FocusRow(StartMenuRow.ResetSettings);
        Assert.That(model.Activate(), Is.EqualTo(StartMenuCommand.ResetSettings));
    }

    [Test]
    public void Activate_OnAValueRow_StepsIt()
    {
        var settings = new PCWarmerSettings();
        var model = new StartMenuModel(settings, 8);
        model.FocusRow(StartMenuRow.RunLength);

        Assert.That(model.Activate(), Is.EqualTo(StartMenuCommand.None));
        Assert.That(settings.DurationMinutes, Is.EqualTo(60));
    }

    [Test]
    public void SetScreensaver_PicksAndFocusesIt()
    {
        var settings = new PCWarmerSettings();
        var model = new StartMenuModel(settings, 8);

        model.SetScreensaver("starfield");

        Assert.That(settings.ScreensaverId, Is.EqualTo("starfield"));
        Assert.That(model.Focused, Is.EqualTo(StartMenuRow.Screensaver));
    }

    [Test]
    public void Locked_IgnoresEverything()
    {
        var settings = new PCWarmerSettings();
        var model = new StartMenuModel(settings, 8) { Locked = true };

        model.MoveFocus(1);
        model.SetIntensity(HardwareLoad.High);
        model.SetScreensaver("embers");

        Assert.That(model.Focused, Is.EqualTo(StartMenuRow.Start));
        Assert.That(settings.HardwareLoad, Is.EqualTo(HardwareLoad.Medium));
        Assert.That(settings.ScreensaverId, Is.EqualTo("particle-storm"));
        Assert.That(model.ChangeValue(1), Is.False);
        Assert.That(model.Activate(), Is.EqualTo(StartMenuCommand.None));
    }

    [TestCase(StartMenuRow.RunLength, ExpectedResult = "30 min")]
    [TestCase(StartMenuRow.Display, ExpectedResult = "Full screen")]
    [TestCase(StartMenuRow.CpuThreads, ExpectedResult = "All cores (8)")]
    [TestCase(StartMenuRow.FrameRate, ExpectedResult = "60")]
    [TestCase(StartMenuRow.Screensaver, ExpectedResult = "Particle Storm")]
    [TestCase(StartMenuRow.Intensity, ExpectedResult = "Medium")]
    [TestCase(StartMenuRow.CpuLoad, ExpectedResult = "Medium")]
    public string ValueText_ForDefaults(StartMenuRow row) => Model().ValueText(row);

    [Test]
    public void ValueText_ShowsFallbacksAndOtherValues()
    {
        var model = new StartMenuModel(new PCWarmerSettings
        {
            DurationMinutes = 0, FullScreen = false, CpuThreads = 3, FpsCap = 0, ScreensaverId = "gone",
        }, cores: 8);

        Assert.That(model.ValueText(StartMenuRow.RunLength), Is.EqualTo("Unlimited"));
        Assert.That(model.ValueText(StartMenuRow.Display), Is.EqualTo("Window"));
        Assert.That(model.ValueText(StartMenuRow.CpuThreads), Is.EqualTo("3"));
        Assert.That(model.ValueText(StartMenuRow.FrameRate), Is.EqualTo("60"));
        Assert.That(model.ValueText(StartMenuRow.Screensaver), Is.EqualTo("Particle Storm"));
    }
}
