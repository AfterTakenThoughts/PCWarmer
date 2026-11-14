using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PCWarmer.Core.Screensavers
{
    /// <summary>Makes a soft round white dot at runtime, so round screensavers need no content asset.</summary>
    internal static class GlowTexture
    {
        public const int Size = 32;

        /// <summary>A white dot that fades from opaque at the center to clear at the edge (premultiplied alpha).</summary>
        public static Texture2D Create(GraphicsDevice device)
        {
            var pixels = new Color[Size * Size];
            float radius = Size / 2f;

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius));
                    float falloff = MathF.Max(0f, 1f - distance / radius);
                    float alpha = falloff * falloff;
                    pixels[y * Size + x] = new Color(alpha, alpha, alpha, alpha);
                }
            }

            var texture = new Texture2D(device, Size, Size);
            texture.SetData(pixels);
            return texture;
        }
    }
}
