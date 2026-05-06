using System;
using System.IO;
using System.IO.Compression;

namespace CASOutfitImporter.Imaging
{
    // Minimal PNG encoder for 8-bit RGBA images.
    //   signature (8) | IHDR | IDAT | IEND, each chunk: u32 length BE, 4-byte type,
    //   payload, u32 CRC32 BE over (type+payload).
    // IDAT carries zlib-wrapped deflate of filtered scanlines.
    internal static class PngWriter
    {
        private static readonly byte[] Signature = { 137, 80, 78, 71, 13, 10, 26, 10 };

        public static byte[] Encode(RgbaImage img)
        {
            using var ms = new MemoryStream();
            ms.Write(Signature, 0, Signature.Length);

            // IHDR
            using (var ih = new MemoryStream())
            {
                WriteU32Be(ih, (uint)img.Width);
                WriteU32Be(ih, (uint)img.Height);
                ih.WriteByte(8);   // bit depth
                ih.WriteByte(6);   // color type: RGBA
                ih.WriteByte(0);   // compression: deflate
                ih.WriteByte(0);   // filter: 0
                ih.WriteByte(0);   // interlace: none
                WriteChunk(ms, "IHDR", ih.ToArray());
            }

            // Build raw filtered scanlines: each row prefixed with filter byte 0.
            int stride = img.Width * 4;
            byte[] raw = new byte[(stride + 1) * img.Height];
            int p = 0;
            for (int y = 0; y < img.Height; y++)
            {
                raw[p++] = 0; // filter type "None"
                int rowStart = y * img.Width;
                for (int x = 0; x < img.Width; x++)
                {
                    uint px = img.Pixels[rowStart + x];
                    raw[p++] = (byte)(px & 0xFF);          // R
                    raw[p++] = (byte)((px >> 8) & 0xFF);   // G
                    raw[p++] = (byte)((px >> 16) & 0xFF);  // B
                    raw[p++] = (byte)((px >> 24) & 0xFF);  // A
                }
            }

            byte[] zlib = ZlibCompress(raw);
            WriteChunk(ms, "IDAT", zlib);
            WriteChunk(ms, "IEND", Array.Empty<byte>());
            return ms.ToArray();
        }

        private static void WriteChunk(Stream s, string type, byte[] data)
        {
            WriteU32Be(s, (uint)data.Length);
            byte[] tBytes = { (byte)type[0], (byte)type[1], (byte)type[2], (byte)type[3] };
            s.Write(tBytes, 0, 4);
            s.Write(data, 0, data.Length);
            // CRC over type + data.
            uint crc = Crc32.Compute(tBytes, 0, 4);
            crc = Crc32.Compute(data, 0, data.Length, crc);
            WriteU32Be(s, crc);
        }

        private static void WriteU32Be(Stream s, uint v)
        {
            s.WriteByte((byte)(v >> 24));
            s.WriteByte((byte)(v >> 16));
            s.WriteByte((byte)(v >> 8));
            s.WriteByte((byte)v);
        }

        // zlib stream = 2-byte header + deflate payload + 4-byte adler32 (BE).
        private static byte[] ZlibCompress(byte[] data)
        {
            using var ms = new MemoryStream();
            ms.WriteByte(0x78); // CMF: deflate, 32K window
            ms.WriteByte(0x01); // FLG: fastest, no preset dict, FCHECK valid
            using (var def = new DeflateStream(ms, CompressionLevel.Fastest, leaveOpen: true))
            {
                def.Write(data, 0, data.Length);
            }
            uint adler = Adler32.Compute(data);
            ms.WriteByte((byte)(adler >> 24));
            ms.WriteByte((byte)(adler >> 16));
            ms.WriteByte((byte)(adler >> 8));
            ms.WriteByte((byte)adler);
            return ms.ToArray();
        }
    }

    internal static class Crc32
    {
        private static readonly uint[] Table = BuildTable();

        private static uint[] BuildTable()
        {
            var t = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                uint c = n;
                for (int k = 0; k < 8; k++)
                    c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : (c >> 1);
                t[n] = c;
            }
            return t;
        }

        public static uint Compute(byte[] data, int off, int len, uint seed = 0)
        {
            uint c = seed ^ 0xFFFFFFFFu;
            for (int i = 0; i < len; i++)
                c = Table[(c ^ data[off + i]) & 0xFF] ^ (c >> 8);
            return c ^ 0xFFFFFFFFu;
        }
    }

    internal static class Adler32
    {
        public static uint Compute(byte[] data)
        {
            const uint MOD = 65521;
            uint a = 1, b = 0;
            foreach (byte v in data)
            {
                a = (a + v) % MOD;
                b = (b + a) % MOD;
            }
            return (b << 16) | a;
        }
    }
}