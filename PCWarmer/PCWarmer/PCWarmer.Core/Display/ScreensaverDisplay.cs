using Microsoft.Xna.Framework;

namespace PCWarmer.Core.Display
{
    /// <summary>The <c>GraphicsDeviceManager</c> values for showing the screensaver.</summary>
    /// <param name="IsFullScreen">Value for <c>IsFullScreen</c>.</param>
    /// <param name="HardwareModeSwitch">Value for <c>HardwareModeSwitch</c>; always false.</param>
    /// <param name="BackBufferSize">Values for <c>PreferredBackBufferWidth</c> and <c>PreferredBackBufferHeight</c>.</param>
    public readonly record struct DisplaySettings(bool IsFullScreen, bool HardwareModeSwitch, Point BackBufferSize);

    /// <summary>How the screensaver fills the screen. Only the screensaver goes full screen; the menus stay windowed.</summary>
    public static class ScreensaverDisplay
    {
        /// <summary>
        /// Full screen is borderless at the desktop's current resolution: a window covering the screen, so the monitor's
        /// resolution never changes and other open windows aren't resized. Windowed keeps the current window size.
        /// </summary>
        public static DisplaySettings For(bool fullScreen, Point desktopSize, Point windowSize)
            => fullScreen
                ? new DisplaySettings(true, false, desktopSize)
                : new DisplaySettings(false, false, windowSize);
    }
}
