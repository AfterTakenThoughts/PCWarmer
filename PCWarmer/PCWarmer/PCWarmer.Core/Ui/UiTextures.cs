using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;

namespace PCWarmer.Core.Ui
{
    /// <summary>White textures with <see cref="ShapeMasks"/> alpha, made once per graphics device and tinted when drawn.</summary>
    public static class UiTextures
    {
        private static readonly Dictionary<(GraphicsDevice, string, int), Texture2D> Cache = new();

        /// <summary>A rounded square, 4 × <paramref name="radius"/> across, for stretching as a nine-patch.</summary>
        public static Texture2D RoundedSquare(GraphicsDevice device, int radius)
            => Get(device, "rounded", radius, () => ShapeMasks.RoundedRect(radius * 4, radius * 4, radius), radius * 4);

        /// <summary>A one-pixel rounded outline, 4 × <paramref name="radius"/> across, for stretching as a nine-patch.</summary>
        public static Texture2D RoundedOutlineSquare(GraphicsDevice device, int radius)
            => Get(device, "outline", radius, () => ShapeMasks.RoundedOutline(radius * 4, radius * 4, radius, 1), radius * 4);

        public static Texture2D Disc(GraphicsDevice device, int size) => Get(device, "disc", size, () => ShapeMasks.Disc(size), size);

        public static Texture2D Glow(GraphicsDevice device, int size) => Get(device, "glow", size, () => ShapeMasks.Glow(size), size);

        public static Texture2D Halo(GraphicsDevice device, int size) => Get(device, "halo", size, () => ShapeMasks.Halo(size), size);

        /// <summary>A rounded panel background for Myra that stretches to any size while keeping its corners.</summary>
        public static NinePatchRegion RoundedPanel(GraphicsDevice device, int radius)
        {
            Texture2D texture = RoundedSquare(device, radius);
            return new NinePatchRegion(texture, new Rectangle(0, 0, texture.Width, texture.Height), new Thickness(radius));
        }

        /// <summary>A rounded one-pixel outline for Myra that stretches to any size; its middle is see-through.</summary>
        public static NinePatchRegion RoundedOutline(GraphicsDevice device, int radius)
        {
            Texture2D texture = RoundedOutlineSquare(device, radius);
            return new NinePatchRegion(texture, new Rectangle(0, 0, texture.Width, texture.Height), new Thickness(radius));
        }

        private static Texture2D Get(GraphicsDevice device, string kind, int size, Func<float[]> mask, int side)
        {
            if (Cache.TryGetValue((device, kind, size), out Texture2D cached) && !cached.IsDisposed)
                return cached;

            float[] alpha = mask();
            var pixels = new Color[alpha.Length];
            for (int i = 0; i < alpha.Length; i++)
                pixels[i] = Color.White * alpha[i];   // premultiplied, as SpriteBatch expects

            var texture = new Texture2D(device, side, side);
            texture.SetData(pixels);
            Cache[(device, kind, size)] = texture;
            return texture;
        }
    }
}
