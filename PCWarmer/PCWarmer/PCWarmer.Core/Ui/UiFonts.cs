using System.IO;
using FontStashSharp;

namespace PCWarmer.Core.Ui
{
    /// <summary>The menus' font (Inter, SIL OFL 1.1), sharp at any pixel size.</summary>
    public sealed class UiFonts
    {
        private readonly FontSystem regular = new();
        private readonly FontSystem semiBold = new();

        public UiFonts(Stream regularFont, Stream semiBoldFont)
        {
            regular.AddFont(ReadAll(regularFont));
            semiBold.AddFont(ReadAll(semiBoldFont));
        }

        public SpriteFontBase Regular(int pixels) => regular.GetFont(pixels);

        public SpriteFontBase SemiBold(int pixels) => semiBold.GetFont(pixels);

        private static byte[] ReadAll(Stream stream)
        {
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            return memory.ToArray();
        }
    }
}
