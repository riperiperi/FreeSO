using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CASOutfitImporter.Formats
{
    // Read-only port of FSO.Files.FAR3.FAR3Archive — just what the importer needs to
    // pull an existing collection out of an installed game's archive.
    //
    // Format (little-endian throughout):
    //   8B "FAR!byAZ"
    //   u32 version=3
    //   u32 manifestOffset
    //   ...
    //   at manifestOffset:
    //     u32 numFiles
    //     numFiles × Far3Entry { decompSize:u32, compSize:u24, dataType:u8,
    //                            dataOffset:u32, isCompressed:u8, accessNumber:u8,
    //                            filenameLength:u16, typeId:u32, fileId:u32,
    //                            filename:ASCII[filenameLength] }
    internal sealed class Far3Entry
    {
        public uint DecompressedSize;
        public uint CompressedSize;
        public byte DataType;
        public uint DataOffset;
        public byte IsCompressed;
        public byte AccessNumber;
        public ushort FilenameLength;
        public uint TypeId;
        public uint FileId;
        public string Filename;
    }

    internal sealed class Far3Reader : IDisposable
    {
        private readonly BinaryReader _r;
        private readonly Stream _s;
        public List<Far3Entry> Entries { get; } = new List<Far3Entry>();

        public Far3Reader(Stream stream)
        {
            _s = stream;
            _r = new BinaryReader(stream);

            string header = Encoding.ASCII.GetString(_r.ReadBytes(8));
            if (header != "FAR!byAZ")
                throw new InvalidDataException($"not a FAR3 archive (got header '{header}')");

            uint version = _r.ReadUInt32();
            if (version != 3)
                throw new InvalidDataException($"FAR archive version {version} unsupported (need 3)");

            uint manifestOffset = _r.ReadUInt32();
            _s.Seek(manifestOffset, SeekOrigin.Begin);
            uint numFiles = _r.ReadUInt32();

            for (int i = 0; i < numFiles; i++)
            {
                var e = new Far3Entry
                {
                    DecompressedSize = _r.ReadUInt32(),
                };
                byte b0 = _r.ReadByte();
                byte b1 = _r.ReadByte();
                byte b2 = _r.ReadByte();
                e.CompressedSize = (uint)(b0 | (b1 << 8) | (b2 << 16));
                e.DataType = _r.ReadByte();
                e.DataOffset = _r.ReadUInt32();
                e.IsCompressed = _r.ReadByte();
                e.AccessNumber = _r.ReadByte();
                e.FilenameLength = _r.ReadUInt16();
                e.TypeId = _r.ReadUInt32();
                e.FileId = _r.ReadUInt32();
                e.Filename = Encoding.ASCII.GetString(_r.ReadBytes(e.FilenameLength));
                Entries.Add(e);
            }
        }

        public static Far3Reader Open(string path)
            => new Far3Reader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read));

        // Quick sniff to decide whether a path is a FAR3 archive vs raw bytes,
        // without taking ownership of the stream.
        public static bool LooksLikeFar3(string path)
        {
            try
            {
                using var fs = File.OpenRead(path);
                Span<byte> head = stackalloc byte[8];
                int n = fs.Read(head);
                if (n < 8) return false;
                return head[0] == 'F' && head[1] == 'A' && head[2] == 'R' && head[3] == '!' &&
                       head[4] == 'b' && head[5] == 'y' && head[6] == 'A' && head[7] == 'Z';
            }
            catch { return false; }
        }

        public Far3Entry FindByFilename(string name)
        {
            // Filenames in FAR3 are case-sensitive but the live engine compares
            // lowercase. Match the engine convention.
            string needle = name.ToLowerInvariant();
            foreach (var e in Entries)
                if (e.Filename.ToLowerInvariant() == needle) return e;
            return null;
        }

        // Mirror of FAR3Archive.GetEntry: handles both compressed and raw payloads.
        // Compressed format used by Maxis is the QFS/RefPack scheme — ID 0xFB10.
        public byte[] Read(Far3Entry entry)
        {
            _s.Seek(entry.DataOffset, SeekOrigin.Begin);

            if (entry.IsCompressed == 0x01)
            {
                _s.Seek(9, SeekOrigin.Current);                // skip persist hdr bytes
                uint filesize = _r.ReadUInt32();
                ushort compressionId = _r.ReadUInt16();

                if (compressionId == 0xFB10)
                {
                    byte d0 = _r.ReadByte();
                    byte d1 = _r.ReadByte();
                    byte d2 = _r.ReadByte();
                    uint decompressedSize = (uint)((d0 << 16) | (d1 << 8) | d2);

                    byte[] compressed = _r.ReadBytes((int)filesize);
                    return RefPack.Decompress(compressed, (int)decompressedSize);
                }

                // Compression flag set but payload is uncompressed (Maxis quirk):
                // rewind 15 bytes (9 skipped + 4 filesize + 2 compressionId) and read raw.
                _s.Seek(-15, SeekOrigin.Current);
                return _r.ReadBytes((int)entry.DecompressedSize);
            }

            return _r.ReadBytes((int)entry.DecompressedSize);
        }

        public void Dispose() => _r.Dispose();
    }
}