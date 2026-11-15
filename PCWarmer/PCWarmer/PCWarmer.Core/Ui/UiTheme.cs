using Microsoft.Xna.Framework;

namespace PCWarmer.Core.Ui
{
    /// <summary>The menus' colours and shape sizes. Red (<see cref="Accent"/>) is only for Start, "Yes, start" and the gauge arcs.</summary>
    public static class UiTheme
    {
        public static readonly Color Background = new(0x14, 0x14, 0x16);
        public static readonly Color Surface = new(0x1D, 0x1E, 0x22);
        public static readonly Color SurfaceSelected = new(0x2A, 0x2B, 0x30);
        public static readonly Color Border = new(0x2E, 0x2F, 0x35);
        public static readonly Color BorderFocused = new(0x8A, 0x8A, 0x90);

        /// <summary>The thin light border of secondary buttons (Exit, Cancel, Reset settings), which are never red.</summary>
        public static readonly Color SecondaryBorder = new(0x4A, 0x4B, 0x52);
        public static readonly Color Text = new(0xCF, 0xCF, 0xD4);
        public static readonly Color TextStrong = new(0xF2, 0xF2, 0xF2);
        public static readonly Color TextMuted = new(0x7C, 0x7C, 0x84);
        public static readonly Color Accent = new(0xC8, 0x10, 0x2E);
        public static readonly Color AccentLight = new(0xFF, 0x5A, 0x4E);

        /// <summary>Corner radius of panels, rows, tiles and buttons, at the 800×480 reference size.</summary>
        public const int CornerRadius = 10;
    }
}
