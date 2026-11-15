using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace PCWarmer.Screens;

/// <summary>What a frame's input does to a running screensaver.</summary>
public enum ScreensaverAction
{
    None,

    /// <summary>Bring the dashboard back (it fades out on its own), keeping the run going.</summary>
    ShowDashboard,

    /// <summary>Stop the run and leave the screensaver.</summary>
    Stop,
}

/// <summary>
/// Decides what input does on the screensaver: any key, gamepad button or click stops the run, and moving the mouse
/// brings the dashboard back. Has no drawing or devices, so it can be tested.
/// </summary>
public static class ScreensaverInput
{
    /// <summary>How far the mouse must move in one frame, in pixels, to count as moving it on purpose.</summary>
    public const float MoveThreshold = 4f;

    /// <param name="newKeys">Keys that went down this frame.</param>
    /// <param name="newButtons">Gamepad buttons that went down this frame.</param>
    /// <param name="newClick">Whether a mouse button went down this frame.</param>
    /// <param name="mouseDelta">How far the mouse moved this frame, in window pixels.</param>
    public static ScreensaverAction Decide(
        IReadOnlyCollection<Keys> newKeys,
        IReadOnlyCollection<Buttons> newButtons,
        bool newClick,
        Vector2 mouseDelta)
    {
        if (newKeys.Count > 0 || newButtons.Count > 0 || newClick)
            return ScreensaverAction.Stop;

        return mouseDelta.Length() > MoveThreshold ? ScreensaverAction.ShowDashboard : ScreensaverAction.None;
    }
}
