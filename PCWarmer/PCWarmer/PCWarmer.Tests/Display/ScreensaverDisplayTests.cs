using Microsoft.Xna.Framework;
using NUnit.Framework;
using PCWarmer.Core.Display;

namespace PCWarmer.Tests.Display;

[TestFixture]
public class ScreensaverDisplayTests
{
    private static readonly Point Desktop = new(2560, 1440);
    private static readonly Point Window = new(800, 480);

    [Test]
    public void FullScreen_IsBorderlessAtTheDesktopResolution()
        => Assert.That(ScreensaverDisplay.For(fullScreen: true, Desktop, Window),
            Is.EqualTo(new DisplaySettings(IsFullScreen: true, HardwareModeSwitch: false, BackBufferSize: Desktop)));

    [Test]
    public void Windowed_KeepsTheCurrentWindowSize()
        => Assert.That(ScreensaverDisplay.For(fullScreen: false, Desktop, Window),
            Is.EqualTo(new DisplaySettings(IsFullScreen: false, HardwareModeSwitch: false, BackBufferSize: Window)));

    // A hardware mode switch changes the monitor's resolution, which makes Windows resize every other open window.
    [TestCase(true)]
    [TestCase(false)]
    public void NeverSwitchesTheMonitorsMode(bool fullScreen)
        => Assert.That(ScreensaverDisplay.For(fullScreen, Desktop, Window).HardwareModeSwitch, Is.False);
}
