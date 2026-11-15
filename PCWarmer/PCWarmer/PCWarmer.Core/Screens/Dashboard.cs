using System;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PCWarmer.Core.Display;
using PCWarmer.Core.Ui;

namespace PCWarmer.Screens;

/// <summary>
/// The warming screen's dashboard: a rounded panel in the bottom-left corner with a CPU gauge, an FPS gauge and a
/// short readout. Sizes are for an 800×480 window and multiplied by the menu scale.
/// </summary>
sealed class Dashboard
{
    private const int ArcSegments = 54;

    private readonly UiFonts fonts;
    private readonly Texture2D pixel;
    private readonly Texture2D rounded;

    public Dashboard(UiFonts fonts, GraphicsDevice device)
    {
        this.fonts = fonts ?? throw new ArgumentNullException(nameof(fonts));
        pixel = new Texture2D(device, 1, 1);
        pixel.SetData(new[] { Color.White });
        rounded = UiTextures.RoundedSquare(device, UiTheme.CornerRadius * 2);
    }

    /// <param name="opacity">0 to 1; the dashboard fades out on its own and comes back when the mouse moves.</param>
    /// <param name="scale">The menu scale for the window (1 at 800×480).</param>
    public void Draw(SpriteBatch batch, Viewport viewport, float opacity, float scale, double cpuDutyCycle, double fps,
        FpsCap cap, TimeSpan? remaining, int threads, int particles)
    {
        int Px(float n) => Math.Max(1, (int)MathF.Round(n * scale));

        SpriteFontBase label = fonts.Regular(Px(11));
        SpriteFontBase strong = fonts.SemiBold(Px(12));

        string[] lines =
        {
            remaining is { } left ? $"{left:hh\\:mm\\:ss} left" : "Unlimited",
            cpuDutyCycle > 0 ? $"{threads} threads" : "No CPU load",
            $"{particles:N0} particles",
            "Press any key to stop",
        };

        int gauge = Px(62);
        int gap = Px(14);
        int padding = Px(12);
        float lineHeight = label.LineHeight + Px(2);
        float readoutWidth = 0;
        foreach (string line in lines)
            readoutWidth = Math.Max(readoutWidth, label.MeasureString(line).X);

        int labelHeight = (int)label.LineHeight + Px(4);
        int width = padding * 2 + gauge * 2 + gap * 2 + (int)readoutWidth;
        int height = padding * 2 + Math.Max(gauge + labelHeight, (int)(lineHeight * lines.Length));
        var panel = new Rectangle(Px(14), viewport.Height - Px(14) - height, width, height);

        batch.Begin();
        DrawNinePatch(batch, panel, Px(UiTheme.CornerRadius), UiTheme.Surface * (0.8f * opacity));

        var cpuCentre = new Vector2(panel.X + padding + gauge / 2f, panel.Y + padding + gauge / 2f);
        var fpsCentre = cpuCentre + new Vector2(gauge + gap, 0);
        DrawGauge(batch, cpuCentre, gauge / 2f, Px(5), GaugeScale.Fraction(cpuDutyCycle, 1), opacity);
        DrawGauge(batch, fpsCentre, gauge / 2f, Px(5), GaugeScale.Fraction(fps, GaugeScale.FpsFull(cap)), opacity);

        DrawCentred(batch, label, $"CPU {cpuDutyCycle * 100:0}%", cpuCentre + new Vector2(0, gauge / 2f + Px(4)), opacity);
        DrawCentred(batch, label, $"{fps:0} FPS", fpsCentre + new Vector2(0, gauge / 2f + Px(4)), opacity);

        var text = new Vector2(fpsCentre.X + gauge / 2f + gap, panel.Y + padding);
        for (int i = 0; i < lines.Length; i++)
        {
            SpriteFontBase font = i == 0 ? strong : label;
            Color color = i == lines.Length - 1 ? UiTheme.TextMuted : i == 0 ? UiTheme.TextStrong : UiTheme.Text;
            font.DrawText(batch, lines[i], text + new Vector2(0, lineHeight * i), color * opacity);
        }

        batch.End();
    }

    /// <summary>A 270° track with a red arc over the first <paramref name="fraction"/> of it, from bottom left clockwise.</summary>
    private void DrawGauge(SpriteBatch batch, Vector2 centre, float radius, int thickness, float fraction, float opacity)
    {
        // In screen space (y down) bottom left is 135°, and angles grow clockwise, over the top (270°) to bottom right.
        const float start = 135f;
        float r = radius - thickness / 2f;
        float step = GaugeScale.SweepDegrees / ArcSegments;
        float length = MathHelper.ToRadians(step) * r + 1;

        for (int i = 0; i < ArcSegments; i++)
        {
            float angle = MathHelper.ToRadians(start + step * (i + 0.5f));
            Vector2 at = centre + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * r;
            bool lit = (i + 0.5f) / ArcSegments <= fraction;
            Color color = (lit ? UiTheme.Accent : UiTheme.Border) * opacity;

            batch.Draw(pixel, at, null, color, angle + MathHelper.PiOver2, new Vector2(0.5f, 0.5f),
                new Vector2(length, thickness), SpriteEffects.None, 0);
        }
    }

    private static void DrawCentred(SpriteBatch batch, SpriteFontBase font, string text, Vector2 topCentre, float opacity)
    {
        Vector2 size = font.MeasureString(text);
        font.DrawText(batch, text, new Vector2(topCentre.X - size.X / 2, topCentre.Y), UiTheme.TextMuted * opacity);
    }

    /// <summary>Draws the rounded texture stretched to <paramref name="bounds"/> while keeping its corners round.</summary>
    private void DrawNinePatch(SpriteBatch batch, Rectangle bounds, int corner, Color color)
    {
        int src = rounded.Width / 4;   // the texture's corner radius
        corner = Math.Min(corner, Math.Min(bounds.Width, bounds.Height) / 2);
        int[] sx = { 0, src, rounded.Width - src, rounded.Width };
        int[] dx = { bounds.Left, bounds.Left + corner, bounds.Right - corner, bounds.Right };
        int[] dy = { bounds.Top, bounds.Top + corner, bounds.Bottom - corner, bounds.Bottom };

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                var source = new Rectangle(sx[col], sx[row], sx[col + 1] - sx[col], sx[row + 1] - sx[row]);
                var dest = new Rectangle(dx[col], dy[row], dx[col + 1] - dx[col], dy[row + 1] - dy[row]);
                if (dest.Width > 0 && dest.Height > 0)
                    batch.Draw(rounded, dest, source, color);
            }
        }
    }
}
