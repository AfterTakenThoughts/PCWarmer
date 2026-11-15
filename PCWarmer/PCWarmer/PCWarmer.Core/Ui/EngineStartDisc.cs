using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;

namespace PCWarmer.Core.Ui
{
    /// <summary>The round, glowing red disc behind the start screen's Start label. Its size never changes; the glow pulses slowly.</summary>
    public sealed class EngineStartDisc : Widget
    {
        private static readonly Stopwatch Clock = Stopwatch.StartNew();
        private static readonly Color Ring = new(0x2A, 0x2A, 0x2E);

        /// <summary>Diameter of the red disc, in pixels.</summary>
        public int Diameter { get; set; } = 120;

        public bool Focused { get; set; }

        public EngineStartDisc()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
        }

        public override void InternalRender(RenderContext context)
        {
            GraphicsDevice device = MyraEnvironment.GraphicsDevice;
            var disc = new TextureRegion(UiTextures.Disc(device, 256));
            var glow = new TextureRegion(UiTextures.Glow(device, 128));
            var halo = new TextureRegion(UiTextures.Halo(device, 128));

            Rectangle bounds = ActualBounds;
            Point centre = bounds.Center;

            // A slow 2.4-second pulse between 0.8 and 1.
            float pulse = 0.9f + 0.1f * MathF.Sin((float)(Clock.Elapsed.TotalSeconds * Math.PI * 2 / 2.4));

            // A wide red halo filling the whole widget, plus a hotter one hugging the disc, so the glow shows
            // clearly around the button.
            halo.Draw(context, bounds, UiTheme.Accent * (0.7f * pulse));
            halo.Draw(context, Square(centre, Diameter * 3 / 2), UiTheme.AccentLight * (0.3f * pulse));

            int ring = Math.Max(2, Diameter / 20);
            if (Focused)
                disc.Draw(context, Square(centre, Diameter + ring * 2 + ring), UiTheme.BorderFocused);
            disc.Draw(context, Square(centre, Diameter + ring * 2), Ring);
            disc.Draw(context, Square(centre, Diameter), UiTheme.Accent);

            // A lighter patch towards the top left reads as a radial gradient.
            var highlight = Square(new Point(centre.X - Diameter / 10, centre.Y - Diameter / 8), Diameter * 3 / 4);
            glow.Draw(context, highlight, UiTheme.AccentLight * 0.7f);
        }

        private static Rectangle Square(Point centre, int size) => new(centre.X - size / 2, centre.Y - size / 2, size, size);
    }
}
