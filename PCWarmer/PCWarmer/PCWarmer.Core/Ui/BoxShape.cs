using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace PCWarmer.Core.Ui
{
    /// <summary>A rounded rectangle with an optional one-pixel border, drawn behind a panel's content.</summary>
    public sealed class BoxShape : Widget
    {
        public Color Fill { get; set; }

        public Color BorderColor { get; set; }

        /// <summary>Used for the border while the mouse is over the parent panel, if set.</summary>
        public Color? HoverBorderColor { get; set; }

        public int Radius { get; set; } = UiTheme.CornerRadius;

        public BoxShape()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
        }

        public override void InternalRender(RenderContext context)
        {
            GraphicsDevice device = MyraEnvironment.GraphicsDevice;
            int radius = Math.Max(1, Radius);
            Rectangle bounds = ActualBounds;
            Color border = HoverBorderColor is { } hover && Parent?.IsMouseInside == true ? hover : BorderColor;

            // Fill first, inside the border, then the border as an outline so an unfilled box stays see-through.
            if (Fill.A > 0)
            {
                Rectangle inner = bounds;
                if (border.A > 0)
                    inner.Inflate(-1, -1);
                UiTextures.RoundedPanel(device, radius).Draw(context, inner, Fill);
            }

            if (border.A > 0)
                UiTextures.RoundedOutline(device, radius).Draw(context, bounds, border);
        }
    }
}
