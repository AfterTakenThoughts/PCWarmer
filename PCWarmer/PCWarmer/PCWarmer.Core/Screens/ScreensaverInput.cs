using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace PCWarmer.Screens;

/// <summary>What a frame's input does to a running screensaver.</summary>
public enum ScreensaverAction
{
    None,

    /// <summary>Show or hide the HUD, keeping the run going.</summary>
    ToggleHud,

    /// <summary>Stop the run and leave the screensaver.</summary>
    Stop,
}

/// <summary>
/// Decides what input does on the screensaver. With the HUD showing, H, gamepad Y or the on-screen Hide UI button
/// hide it, and any other key, button or click stops the run. With the HUD hidden, any key, button or click shows it
/// again, so nothing stops the run by accident. Has no drawing or devices, so it can be tested.
/// </summary>
public static class ScreensaverInput
{
    public const Keys ToggleHudKey = Keys.H;
    public const Buttons ToggleHudButton = Buttons.Y;

    /// <param name="newKeys">Keys that went down this frame.</param>
    /// <param name="newButtons">Gamepad buttons that went down this frame.</param>
    /// <param name="click">Where the mouse was clicked this frame, in window pixels; null for no click.</param>
    /// <param name="hudVisible">Whether the HUD (and its Hide UI button) is showing.</param>
    /// <param name="hideButton">The Hide UI button's bounds, in window pixels.</param>
    public static ScreensaverAction Decide(
        IReadOnlyCollection<Keys> newKeys,
        IReadOnlyCollection<Buttons> newButtons,
        Vector2? click,
        bool hudVisible,
        Rectangle hideButton)
    {
        if (!hudVisible)
        {
            bool anyPress = newKeys.Count > 0 || newButtons.Count > 0 || click.HasValue;
            return anyPress ? ScreensaverAction.ToggleHud : ScreensaverAction.None;
        }

        bool otherKey = newKeys.Any(key => key != ToggleHudKey);
        bool otherButton = newButtons.Any(button => button != ToggleHudButton);
        bool clickOnHideButton = click is { } point && hideButton.Contains(point);
        bool otherClick = click.HasValue && !clickOnHideButton;

        if (otherKey || otherButton || otherClick)
            return ScreensaverAction.Stop;

        if (newKeys.Contains(ToggleHudKey) || newButtons.Contains(ToggleHudButton) || clickOnHideButton)
            return ScreensaverAction.ToggleHud;

        return ScreensaverAction.None;
    }
}
