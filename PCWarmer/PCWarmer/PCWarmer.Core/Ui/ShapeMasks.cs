using System;

namespace PCWarmer.Core.Ui
{
    /// <summary>
    /// Alpha masks (row-major, 0 to 1) for the menus' shapes, worked out on the CPU so they can be tested without a GPU.
    /// <see cref="UiTextures"/> turns them into textures.
    /// </summary>
    public static class ShapeMasks
    {
        /// <summary>A filled rounded rectangle, anti-aliased over about one pixel at its edge.</summary>
        public static float[] RoundedRect(int width, int height, int radius)
        {
            var mask = new float[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                    mask[y * width + x] = RoundedCoverage(x, y, 0, 0, width, height, radius);
            }

            return mask;
        }

        /// <summary>Only the edge of a rounded rectangle, <paramref name="thickness"/> pixels wide; the middle is clear.</summary>
        public static float[] RoundedOutline(int width, int height, int radius, int thickness)
        {
            var mask = new float[width * height];
            int innerRadius = Math.Max(0, radius - thickness);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float outer = RoundedCoverage(x, y, 0, 0, width, height, radius);
                    float inner = RoundedCoverage(x, y, thickness, thickness, width - thickness * 2, height - thickness * 2, innerRadius);
                    mask[y * width + x] = Math.Clamp(outer - inner, 0, 1);
                }
            }

            return mask;
        }

        /// <summary>How much of pixel (x, y) is inside the rounded rectangle at (left, top) of the given size.</summary>
        private static float RoundedCoverage(int x, int y, float left, float top, float width, float height, float radius)
        {
            if (width <= 0 || height <= 0)
                return 0;

            float r = Math.Min(radius, Math.Min(width, height) / 2f);

            // Distance from the pixel centre to the rectangle shrunk by the radius, minus the radius.
            float px = x + 0.5f - left, py = y + 0.5f - top;
            float dx = Math.Max(Math.Max(r - px, px - (width - r)), 0);
            float dy = Math.Max(Math.Max(r - py, py - (height - r)), 0);
            return Coverage(MathF.Sqrt(dx * dx + dy * dy) - r);
        }

        /// <summary>A filled circle filling a <paramref name="size"/>-pixel square.</summary>
        public static float[] Disc(int size)
        {
            float r = size / 2f;
            var mask = new float[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                    mask[y * size + x] = Coverage(DistanceFromCentre(x, y, r) - r);
            }

            return mask;
        }

        /// <summary>A soft radial glow: 1 at the centre, falling to 0 at the edge of the square.</summary>
        public static float[] Glow(int size)
        {
            float r = size / 2f;
            var mask = new float[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float t = Math.Clamp(1 - DistanceFromCentre(x, y, r) / r, 0, 1);
                    mask[y * size + x] = t * t;
                }
            }

            return mask;
        }

        /// <summary>
        /// A brighter, wider glow than <see cref="Glow"/>: it fades evenly from 1 at the centre to 0 at the edge, so it
        /// is still strong well away from the centre. Used for the Start button's red halo.
        /// </summary>
        public static float[] Halo(int size)
        {
            float r = size / 2f;
            var mask = new float[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                    mask[y * size + x] = Math.Clamp(1 - DistanceFromCentre(x, y, r) / r, 0, 1);
            }

            return mask;
        }

        private static float DistanceFromCentre(int x, int y, float r)
        {
            float dx = x + 0.5f - r, dy = y + 0.5f - r;
            return MathF.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>How much of a pixel is inside a shape, given the signed distance of its centre from the shape's edge.</summary>
        private static float Coverage(float distance) => Math.Clamp(0.5f - distance, 0, 1);
    }
}
