using System;

namespace PCWarmer.Core.Display
{
    /// <summary>The MonoGame switches that produce a frame-rate cap.</summary>
    /// <param name="IsFixedTimeStep">Value for <c>Game.IsFixedTimeStep</c>.</param>
    /// <param name="VSync">Value for <c>GraphicsDeviceManager.SynchronizeWithVerticalRetrace</c>.</param>
    /// <param name="TargetElapsedTime">Value for <c>Game.TargetElapsedTime</c>.</param>
    public readonly record struct FrameRateSettings(bool IsFixedTimeStep, bool VSync, TimeSpan TargetElapsedTime);

    public static class FrameRatePolicy
    {
        /// <summary>
        /// The switches for <paramref name="cap"/>. VSync is always off so a cap above the monitor's
        /// refresh rate (or Uncapped) really renders more frames. Undefined values get 60 fps.
        /// </summary>
        public static FrameRateSettings For(FpsCap cap) => cap switch
        {
            FpsCap.Fps30 => Capped(30),
            FpsCap.Fps144 => Capped(144),
            FpsCap.Uncapped => new FrameRateSettings(false, false, FrameTime(60)),
            _ => Capped(60),
        };

        private static FrameRateSettings Capped(int fps) => new(true, false, FrameTime(fps));

        private static TimeSpan FrameTime(int fps) => TimeSpan.FromTicks(TimeSpan.TicksPerSecond / fps);
    }
}
