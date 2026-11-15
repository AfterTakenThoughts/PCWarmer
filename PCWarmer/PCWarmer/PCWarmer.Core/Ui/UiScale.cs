using System;
using Microsoft.Xna.Framework;

namespace PCWarmer.Core.Ui
{
    /// <summary>How much to scale the menus, whose sizes are given for an 800×480 window.</summary>
    public static class UiScale
    {
        public const float ReferenceWidth = 800f;
        public const float ReferenceHeight = 480f;

        /// <summary>The largest scale at which an 800×480 layout fits the window, never below 0.5.</summary>
        public static float For(Point windowSize)
            => Math.Max(0.5f, Math.Min(windowSize.X / ReferenceWidth, windowSize.Y / ReferenceHeight));
    }
}
