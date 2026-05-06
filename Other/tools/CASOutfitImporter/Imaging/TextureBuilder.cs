using System;

namespace CASOutfitImporter.Imaging
{
    // Loads TS1 BMP textures, optionally applies a magenta color-key, and emits
    // PNG bytes the FSO texture pipeline can decode at runtime.
    internal static class TextureBuilder
    {
        public static byte[] BmpToPng(string bmpPath, bool keyMagenta)
        {
            var img = BmpReader.ReadFile(bmpPath);
            if (keyMagenta) ApplyMagentaKey(img);
            return PngWriter.Encode(img);
        }

        // Synthesize medium/dark skin variants from a single light texture by
        // multiplying RGB by `factor` (<1 darkens). Stop-gap for inputs that
        // ship only `lgt`.
        public static byte[] SynthesizeTone(string lightBmpPath, float factor, bool keyMagenta)
        {
            var img = BmpReader.ReadFile(lightBmpPath);
            if (keyMagenta) ApplyMagentaKey(img);
            DarkenInPlace(img, factor);
            return PngWriter.Encode(img);
        }

        private static void ApplyMagentaKey(RgbaImage img)
        {
            for (int i = 0; i < img.Pixels.Length; i++)
            {
                byte r = img.R(i), g = img.G(i), b = img.B(i);
                if (r == 255 && g == 0 && b == 255)
                    img.Set(i, r, g, b, 0);
            }
        }

        private static void DarkenInPlace(RgbaImage img, float factor)
        {
            for (int i = 0; i < img.Pixels.Length; i++)
            {
                if (img.A(i) == 0) continue; // skip keyed-out pixels
                byte r = (byte)Math.Min(255, (int)(img.R(i) * factor));
                byte g = (byte)Math.Min(255, (int)(img.G(i) * factor));
                byte b = (byte)Math.Min(255, (int)(img.B(i) * factor));
                img.Set(i, r, g, b, img.A(i));
            }
        }
    }
}