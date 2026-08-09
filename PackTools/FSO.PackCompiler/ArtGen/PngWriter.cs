using System;
using System.IO;
using System.IO.Compression;
using Microsoft.Xna.Framework;

namespace FSO.PackCompiler.ArtGen
{
    /// <summary>
    /// Minimal RGBA8 PNG encoder using only BCL (System.IO.Compression.ZLibStream for the
    /// required zlib-wrapped DEFLATE stream) — no System.Drawing/libgdiplus dependency,
    /// so it works in this macOS dev environment without extra native libs. Exists purely so
    /// generated frames can be eyeballed without launching the game client.
    /// </summary>
    public static class PngWriter
    {
        public static void Write(string path, Color[] pixels, int width, int height)
        {
            using var fs = new FileStream(path, FileMode.Create);
            fs.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }); // PNG signature

            WriteChunk(fs, "IHDR", IHDR(width, height));

            using (var raw = new MemoryStream())
            {
                for (int y = 0; y < height; y++)
                {
                    raw.WriteByte(0); // filter type 0 (none) per scanline
                    for (int x = 0; x < width; x++)
                    {
                        var c = pixels[y * width + x];
                        raw.WriteByte(c.R); raw.WriteByte(c.G); raw.WriteByte(c.B); raw.WriteByte(c.A);
                    }
                }
                var compressed = ZlibCompress(raw.ToArray());
                WriteChunk(fs, "IDAT", compressed);
            }

            WriteChunk(fs, "IEND", Array.Empty<byte>());
        }

        static byte[] IHDR(int width, int height)
        {
            var b = new byte[13];
            WriteUInt32BE(b, 0, (uint)width);
            WriteUInt32BE(b, 4, (uint)height);
            b[8] = 8;  // bit depth
            b[9] = 6;  // color type 6 = RGBA
            b[10] = 0; // compression
            b[11] = 0; // filter
            b[12] = 0; // interlace
            return b;
        }

        static byte[] ZlibCompress(byte[] data)
        {
            using var ms = new MemoryStream();
            // zlib header (CMF/FLG for a standard deflate window, no dictionary)
            ms.WriteByte(0x78); ms.WriteByte(0x9C);
            using (var deflate = new DeflateStream(ms, CompressionLevel.Optimal, leaveOpen: true))
                deflate.Write(data, 0, data.Length);
            var adler = Adler32(data);
            var tail = new byte[4];
            WriteUInt32BE(tail, 0, adler);
            ms.Write(tail);
            return ms.ToArray();
        }

        static uint Adler32(byte[] data)
        {
            const uint MOD = 65521;
            uint a = 1, b = 0;
            foreach (var by in data)
            {
                a = (a + by) % MOD;
                b = (b + a) % MOD;
            }
            return (b << 16) | a;
        }

        static void WriteChunk(Stream s, string type, byte[] data)
        {
            var len = new byte[4];
            WriteUInt32BE(len, 0, (uint)data.Length);
            s.Write(len);
            var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
            s.Write(typeBytes);
            s.Write(data);

            using var crcInput = new MemoryStream();
            crcInput.Write(typeBytes);
            crcInput.Write(data);
            var crc = Crc32(crcInput.ToArray());
            var crcBytes = new byte[4];
            WriteUInt32BE(crcBytes, 0, crc);
            s.Write(crcBytes);
        }

        static void WriteUInt32BE(byte[] buf, int offset, uint value)
        {
            buf[offset] = (byte)(value >> 24);
            buf[offset + 1] = (byte)(value >> 16);
            buf[offset + 2] = (byte)(value >> 8);
            buf[offset + 3] = (byte)value;
        }

        static readonly uint[] CrcTable = BuildCrcTable();

        static uint[] BuildCrcTable()
        {
            var table = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                uint c = n;
                for (int k = 0; k < 8; k++)
                    c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
                table[n] = c;
            }
            return table;
        }

        static uint Crc32(byte[] data)
        {
            uint crc = 0xFFFFFFFF;
            foreach (var b in data)
                crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
            return crc ^ 0xFFFFFFFF;
        }
    }
}
