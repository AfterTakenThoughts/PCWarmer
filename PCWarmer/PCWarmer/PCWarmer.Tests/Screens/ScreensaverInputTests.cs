using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using NUnit.Framework;
using PCWarmer.Screens;

namespace PCWarmer.Tests.Screens;

[TestFixture]
public class ScreensaverInputTests
{
    private static readonly Keys[] NoKeys = Array.Empty<Keys>();
    private static readonly Buttons[] NoButtons = Array.Empty<Buttons>();

    [Test]
    public void Nothing_DoesNothing()
        => Assert.That(ScreensaverInput.Decide(NoKeys, NoButtons, false, Vector2.Zero), Is.EqualTo(ScreensaverAction.None));

    [TestCase(Keys.H)]
    [TestCase(Keys.Space)]
    [TestCase(Keys.Escape)]
    public void AnyKey_Stops(Keys key)
        => Assert.That(ScreensaverInput.Decide(new[] { key }, NoButtons, false, Vector2.Zero), Is.EqualTo(ScreensaverAction.Stop));

    [Test]
    public void AnyGamepadButton_Stops()
        => Assert.That(ScreensaverInput.Decide(NoKeys, new[] { Buttons.Y }, false, Vector2.Zero), Is.EqualTo(ScreensaverAction.Stop));

    [Test]
    public void Click_Stops()
        => Assert.That(ScreensaverInput.Decide(NoKeys, NoButtons, true, Vector2.Zero), Is.EqualTo(ScreensaverAction.Stop));

    [Test]
    public void ClickWhileMoving_StillStops()
        => Assert.That(ScreensaverInput.Decide(NoKeys, NoButtons, true, new Vector2(50, 0)), Is.EqualTo(ScreensaverAction.Stop));

    [Test]
    public void MouseMove_ShowsTheDashboard()
        => Assert.That(ScreensaverInput.Decide(NoKeys, NoButtons, false, new Vector2(3, 4.1f)), Is.EqualTo(ScreensaverAction.ShowDashboard));

    [Test]
    public void TinyJitter_IsIgnored()
        => Assert.That(ScreensaverInput.Decide(NoKeys, NoButtons, false, new Vector2(2, 2)), Is.EqualTo(ScreensaverAction.None));
}
