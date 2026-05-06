using System;
using System.IO;

namespace CASOutfitImporter.Imaging
{
    // Minimal reader for 8-bit indexed Windows BMP-3 files (BITMAPINFOHEADER, no
    // compression). All TS1 skin BMPs are this flavor. Returns a top-down RGBA32
    // pixel buffer.
    internal static class BmpReader
    {
        public static RgbaImage ReadFile(string path)
        {
            byte[] data = File.ReadAllBytes(path);
            return Read(data);
        }

        public static RgbaImage Read(byte[] data)
        {
            // BMP file header (14 bytes): "BM", file size, reserved, dataOffset.
            if (data.Length < 54 || data[0] != 'B' || data[1] != 'M')
                throw new InvalidDataException("not a BMP file");
            uint dataOffset = BitConverter.ToUInt32(data, 10);

            // BITMAPINFOHEADER (40 bytes) immediately after.
            uint headerSize = BitConverter.ToUInt32(data, 14);
            int width  = BitConverter.ToInt32(data, 18);
            int heightRaw = BitConverter.ToInt32(data, 22);
            ushort planes = BitConverter.ToUInt16(data, 26);
            ushort bpp = BitConverter.ToUInt16(data, 28);
            uint compression = BitConverter.ToUInt32(data, 30);

            if (planes != 1) throw new InvalidDataException($"unexpected BMP planes={planes}");
            if (compression != 0) throw new InvalidDataException($"compressed BMPs not supported (compression={compression})");

            bool topDown = heightRaw < 0;
            int height = Math.Abs(heightRaw);

            int bytesPerRow = ((width * bpp + 31) / 32) * 4; // padded to 4 bytes

            switch (bpp)
            {
                case 8: return Read8Bit(data, dataOffset, width, height, bytesPerRow, topDown);
                case 24: return Read24Bit(data, dataOffset, width, height, bytesPerRow, topDown);
                case 32: return Read32Bit(data, dataOffset, width, height, bytesPerRow, topDown);
                default: throw new InvalidDataException($"unsupported BMP bpp={bpp}");
            }
        }

        private static RgbaImage Read8Bit(byte[] data, uint dataOffset, int w, int h, int rowBytes, bool topDown)
        {
            // Palette starts right after the 14-byte file header + 40-byte info header.
            // Each entry is 4 bytes: B, G, R, reserved.
            int paletteStart = 14 + 40;
            int paletteEntries = (int)((dataOffset - paletteStart) / 4);
            if (paletteEntries < 1) paletteEntries = 256;
            var palette = new uint[paletteEntries];
            for (int i = 0; i < paletteEntries; i++)
            {
                byte b = data[paletteStart + i * 4 + 0];
                byte g = data[paletteStart + i * 4 + 1];
                byte r = data[paletteStart + i * 4 + 2];
                palette[i] = ((uint)0xFF << 24) | ((uint)b << 16) | ((uint)g << 8) | r;
            }

            var img = new RgbaImage(w, h);
            for (int y = 0; y < h; y++)
            {
                int srcY = topDown ? y : (h - 1 - y);
                int rowOff = (int)dataOffset + srcY * rowBytes;
                for (int x = 0; x < w; x++)
                {
                    byte idx = data[rowOff + x];
                    if (idx >= palette.Length) idx = 0;
                    img.Pixels[y * w + x] = palette[idx];
                }
            }
            return img;
        }

        private static RgbaImage Read24Bit(byte[] data, uint dataOffset, int w, int h, int rowBytes, bool topDown)
        {
            var img = new RgbaImage(w, h);
            for (int y = 0; y < h; y++)
            {
                int srcY = topDown ? y : (h - 1 - y);
                int rowOff = (int)dataOffset + srcY * rowBytes;
                for (int x = 0; x < w; x++)
                {
                    byte b = data[rowOff + x * 3 + 0];
                    byte g = data[rowOff + x * 3 + 1];
                    byte r = data[rowOff + x * 3 + 2];
                    img.Pixels[y * w + x] = ((uint)0xFF << 24) | ((uint)b << 16) | ((uint)g << 8) | r;
                }
            }
            return img;
        }

        private static RgbaImage Read32Bit(byte[] data, uint dataOffset, int w, int h, int rowBytes, bool topDown)
        {
            var img = new RgbaImage(w, h);
            for (int y = 0; y < h; y++)
            {
                int srcY = topDown ? y : (h - 1 - y);
                int rowOff = (int)dataOffset + srcY * rowBytes;
                for (int x = 0; x < w; x++)
                {
                    byte b = data[rowOff + x * 4 + 0];
                    byte g = data[rowOff + x * 4 + 1];
                    byte r = data[rowOff + x * 4 + 2];
                    byte a = data[rowOff + x * 4 + 3];
                    img.Pixels[y * w + x] = ((uint)a << 24) | ((uint)b << 16) | ((uint)g << 8) | r;
                }
            }
            return img;
        }
    }

    // Top-down RGBA8888 pixel buffer. Pixel layout in `uint`:
    //   bits 0-7   : R
    //   bits 8-15  : G
    //   bits 16-23 : B
    //   bits 24-31 : A
    internal sealed class RgbaImage
    {
        public readonly int Width;
        public readonly int Height;
        public readonly uint[] Pixels;
        public RgbaImage(int w, int h)
        {
            Width = w;
            Height = h;
            Pixels = new uint[w * h];
        }

        public byte R(int i) => (byte)(Pixels[i] & 0xFF);
        public byte G(int i) => (byte)((Pixels[i] >> 8) & 0xFF);
        public byte B(int i) => (byte)((Pixels[i] >> 16) & 0xFF);
        public byte A(int i) => (byte)((Pixels[i] >> 24) & 0xFF);

        public void Set(int i, byte r, byte g, byte b, byte a)
        {
            Pixels[i] = ((uint)a << 24) | ((uint)b << 16) | ((uint)g << 8) | r;
        }
    }
}