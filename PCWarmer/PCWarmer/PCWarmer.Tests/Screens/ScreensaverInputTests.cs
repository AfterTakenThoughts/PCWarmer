using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using NUnit.Framework;
using PCWarmer.Screens;

namespace PCWarmer.Tests.Screens;

[TestFixture]
public class ScreensaverInputTests
{
    private static readonly Rectangle HideButton = new(16, 200, 120, 30);
    private static readonly Keys[] NoKeys = Array.Empty<Keys>();
    private static readonly Buttons[] NoButtons = Array.Empty<Buttons>();

    private static ScreensaverAction Decide(
        Keys[]? keys = null, Buttons[]? buttons = null, Vector2? click = null, bool hudVisible = true)
        => ScreensaverInput.Decide(keys ?? NoKeys, buttons ?? NoButtons, click, hudVisible, HideButton);

    [Test]
    public void NothingPressed_DoesNothing()
        => Assert.That(Decide(), Is.EqualTo(ScreensaverAction.None));

    [TestCase(true)]
    [TestCase(false)]
    public void HKey_TogglesTheHudInsteadOfStopping(bool hudVisible)
        => Assert.That(Decide(keys: new[] { Keys.H }, hudVisible: hudVisible), Is.EqualTo(ScreensaverAction.ToggleHud));

    [Test]
    public void GamepadY_TogglesTheHud()
        => Assert.That(Decide(buttons: new[] { Buttons.Y }), Is.EqualTo(ScreensaverAction.ToggleHud));

    [TestCase(Keys.Space)]
    [TestCase(Keys.Escape)]
    [TestCase(Keys.A)]
    public void AnyOtherKey_Stops(Keys key)
        => Assert.That(Decide(keys: new[] { key }), Is.EqualTo(ScreensaverAction.Stop));

    [Test]
    public void AnotherKeyWithH_Stops()
        => Assert.That(Decide(keys: new[] { Keys.H, Keys.Space }), Is.EqualTo(ScreensaverAction.Stop));

    [Test]
    public void AnyOtherGamepadButton_Stops()
        => Assert.That(Decide(buttons: new[] { Buttons.A }), Is.EqualTo(ScreensaverAction.Stop));

    [Test]
    public void ClickOnTheHideButton_TogglesTheHud()
        => Assert.That(Decide(click: new Vector2(50, 210)), Is.EqualTo(ScreensaverAction.ToggleHud));

    [Test]
    public void ClickElsewhere_Stops()
        => Assert.That(Decide(click: new Vector2(400, 300)), Is.EqualTo(ScreensaverAction.Stop));

    // While the HUD is hidden, the hint says "Press any key to show UI": nothing stops the run until it's back.
    [TestCase(Keys.Space)]
    [TestCase(Keys.Escape)]
    [TestCase(Keys.H)]
    public void WhileHidden_AnyKey_ShowsTheHud(Keys key)
        => Assert.That(Decide(keys: new[] { key }, hudVisible: false), Is.EqualTo(ScreensaverAction.ToggleHud));

    [Test]
    public void WhileHidden_AnyGamepadButton_ShowsTheHud()
        => Assert.That(Decide(buttons: new[] { Buttons.A }, hudVisible: false), Is.EqualTo(ScreensaverAction.ToggleHud));

    [TestCase(50f, 210f, TestName = "Click where the button was")]
    [TestCase(400f, 300f, TestName = "Click anywhere else")]
    public void WhileHidden_AnyClick_ShowsTheHud(float x, float y)
        => Assert.That(Decide(click: new Vector2(x, y), hudVisible: false), Is.EqualTo(ScreensaverAction.ToggleHud));

    [Test]
    public void WhileHidden_NothingPressed_DoesNothing()
        => Assert.That(Decide(hudVisible: false), Is.EqualTo(ScreensaverAction.None));
}
