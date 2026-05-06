using System;
using System.IO;
using System.Text;

namespace CASOutfitImporter.Formats
{
    // Big-endian binary writer matching FreeSO's IoWriter when ByteOrder=BIG_ENDIAN.
    // FSO stores avatar metadata (.bnd, .apr, .oft, .po, .col, .mesh) in big-endian.
    internal sealed class BeWriter : IDisposable
    {
        private readonly BinaryWriter _w;
        public Stream BaseStream { get; }

        public BeWriter(Stream s)
        {
            BaseStream = s;
            _w = new BinaryWriter(s);
        }

        public void U8(byte v) => _w.Write(v);
        public void U16(ushort v) => _w.Write(SwapU16(v));
        public void I16(short v) => _w.Write((short)SwapU16((ushort)v));
        public void U32(uint v) => _w.Write(SwapU32(v));
        public void I32(int v) => _w.Write((int)SwapU32((uint)v));
        public void U64(ulong v) => _w.Write(SwapU64(v));
        public void F32(float v)
        {
            // Mirrors IoWriter behavior: floats are written native-endian; the avatar
            // formats consume them as native little-endian on disk despite "BE" wrappers
            // because FloatSwap defaults to false.
            _w.Write(v);
        }
        public void Bytes(byte[] b) => _w.Write(b);

        // Pascal string: 1-byte length prefix then ASCII bytes.
        public void PascalString(string s)
        {
            if (s == null) s = string.Empty;
            if (s.Length > 255) throw new ArgumentException($"Pascal string too long: '{s}'");
            _w.Write((byte)s.Length);
            _w.Write(Encoding.ASCII.GetBytes(s));
        }

        public static ushort SwapU16(ushort v) => (ushort)(((v & 0xFF) << 8) | ((v >> 8) & 0xFF));
        public static uint SwapU32(uint v)
            => ((v & 0x000000FFu) << 24)
             | ((v & 0x0000FF00u) << 8)
             | ((v & 0x00FF0000u) >> 8)
             | ((v & 0xFF000000u) >> 24);
        public static ulong SwapU64(ulong v)
            => ((ulong)SwapU32((uint)(v & 0xFFFFFFFF)) << 32)
             | (SwapU32((uint)(v >> 32)));

        public void Dispose() => _w.Dispose();
    }

    // Encodes the (typeId, fileId) pair into the loose-file naming convention used
    // by TSOAvatarContentProvider: "<basename>.<HEXID16>.<ext>" where HEXID16 is
    // (fileId << 32 | typeId) printed as 16 hex chars (lowercase) using x16 format.
    internal static class IdEncoder
    {
        public static string EmbedId(string baseName, string ext, uint typeId, uint fileId)
        {
            ulong id = ((ulong)fileId << 32) | typeId;
            return $"{baseName}.{id:x16}{ext}";
        }
    }
}