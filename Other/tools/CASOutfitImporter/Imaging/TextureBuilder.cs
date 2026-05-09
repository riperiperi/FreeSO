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

        // Build a thumbnail PNG by downscaling the source BMP. Used as the
        // ThumbnailID target for each Appearance, so the wedding-trunk grid
        // (and any future surface that calls AvatarThumbnails.Get) has
        // something to render. Scaled with box-area averaging in
        // premultiplied-alpha space — bilinear shows magenta/black halos
        // around keyed-out body silhouettes.
        public static byte[] BuildThumbnailFromBmp(string bmpPath, bool keyMagenta, int maxEdge)
        {
            var img = BmpReader.ReadFile(bmpPath);
            if (keyMagenta) ApplyMagentaKey(img);
            var thumb = ResizeBox(img, maxEdge);
            return PngWriter.Encode(thumb);
        }

        private static RgbaImage ResizeBox(RgbaImage src, int maxEdge)
        {
            if (src.Width <= maxEdge && src.Height <= maxEdge) return src;

            double scale = (double)maxEdge / Math.Max(src.Width, src.Height);
            int dstW = Math.Max(1, (int)Math.Round(src.Width * scale));
            int dstH = Math.Max(1, (int)Math.Round(src.Height * scale));
            var dst = new RgbaImage(dstW, dstH);

            for (int dy = 0; dy < dstH; dy++)
            {
                int sy0 = dy * src.Height / dstH;
                int sy1 = Math.Min(src.Height, (dy + 1) * src.Height / dstH);
                if (sy1 == sy0) sy1 = sy0 + 1;

                for (int dx = 0; dx < dstW; dx++)
                {
                    int sx0 = dx * src.Width / dstW;
                    int sx1 = Math.Min(src.Width, (dx + 1) * src.Width / dstW);
                    if (sx1 == sx0) sx1 = sx0 + 1;

                    ulong rA = 0, gA = 0, bA = 0, aSum = 0;
                    int n = 0;
                    for (int sy = sy0; sy < sy1; sy++)
                    for (int sx = sx0; sx < sx1; sx++)
                    {
                        int i = sy * src.Width + sx;
                        byte a = src.A(i);
                        rA += (ulong)src.R(i) * a;
                        gA += (ulong)src.G(i) * a;
                        bA += (ulong)src.B(i) * a;
                        aSum += a;
                        n++;
                    }

                    byte rOut, gOut, bOut, aOut;
                    if (aSum == 0)
                    {
                        rOut = 0; gOut = 0; bOut = 0; aOut = 0;
                    }
                    else
                    {
                        rOut = (byte)(rA / aSum);
                        gOut = (byte)(gA / aSum);
                        bOut = (byte)(bA / aSum);
                        aOut = (byte)(aSum / (ulong)n);
                    }
                    dst.Set(dy * dstW + dx, rOut, gOut, bOut, aOut);
                }
            }
            return dst;
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