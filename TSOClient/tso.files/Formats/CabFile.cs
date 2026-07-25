using FSO.Files.Utils;
using ICSharpCode.SharpZipLib.Zip.Compression;
using System.IO.Compression;

namespace FSO.Files.Formats
{
    public enum CabFlags : ushort
    {
        HasPrevious = 1,
        HasNext = 2,
        HasReserve = 4,
    }

    public class CabFileEntry
    {
        public uint Size;
        public uint Offset;
        public ushort FolderID;
        public ushort Unknown1;
        public uint Unknown2;
        public string Filename;
    }

    public class CabFolderEntry
    {
        public uint BlockOffset;
        public ushort BlockCount;
        public ushort CompressionType;
        public string FolderReserve;

        public CabBlock[] Blocks;
    }

    public class CabBlock
    {
        public ushort CompressedSize;
        public ushort UncompressedSize;
        public byte[] CompressedData;

        public byte[] Decompress()
        {
            // First two bytes are 0x43 0x4B for MSZip

            using var inputStream = new MemoryStream([..CompressedData]);
            using var outputStream = new MemoryStream(UncompressedSize);

            inputStream.Position = 2;

            using var decompressor = new DeflateStream(inputStream, CompressionMode.Decompress);
            decompressor.CopyTo(outputStream);

            return outputStream.ToArray();
        }
    }

    public class CabBlockDecompressor
    {
        private readonly MemoryStream InputStream;
        private readonly MemoryStream OutputStream;
        private readonly Inflater Inflater;

        private int HeaderBytesRead;

        public CabBlockDecompressor()
        {
            InputStream = new MemoryStream();
            OutputStream = new MemoryStream();

            Inflater = new Inflater(true);
        }

        public bool AddBlock(CabBlock block)
        {
            var basePos = InputStream.Position;
            var size = block.UncompressedSize;
            bool hasMore = size == 0;

            // If continuing the mszip, we don't need to skip the input data.
            InputStream.Write(block.CompressedData.AsSpan(Math.Min(2 - HeaderBytesRead, block.CompressedSize)));

            HeaderBytesRead = Math.Min(2, HeaderBytesRead + block.CompressedSize);

            if (!hasMore)
            {
                Inflater.SetInput(InputStream.ToArray());

                var result = new byte[size];
                Inflater.Inflate(result);
                Inflater.Reset();

                InputStream.SetLength(0);
                InputStream.Position = 0;

                OutputStream.Write(result);

                HeaderBytesRead = 0;
            }

            return hasMore;
        }

        public bool AddBlocks(CabBlock[] blocks)
        {
            bool hasMore = false;
            foreach (var block in blocks)
            {
                hasMore = AddBlock(block);
            }

            return hasMore;
        }

        public byte[] GetData(int offset, int size)
        {
            OutputStream.Position = offset;

            var result = new byte[size];
            OutputStream.Read(result, 0, size);

            OutputStream.Seek(0, SeekOrigin.End);

            return result;
        }

        public byte[] ToArray()
        {
            return OutputStream.ToArray();
        }
    }

    public class CabFile
    {
        public uint Size;
        public uint OffsetFiles;
        public byte MajorVersion;
        public byte MinorVersion;
        public ushort FolderCount;
        public ushort FileCount;
        public CabFlags Flags;
        public ushort SetID;
        public ushort ICabinet;

        // Reserve options
        private ushort CabinetResBytes;
        private byte FolderResBytes;
        private byte DataResBytes;

        public string CabReserve;

        // Prev options
        public string PrevCabName;
        public string PrevCabDisk;

        // Next options
        public string NextCabName;
        public string NextCabDisk;

        public CabFileEntry[] Files;
        public CabFolderEntry[] Folders;

        public CabFile(string filepath, bool withBlocks = true)
        {
            using (var stream = File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                this.Read(stream, withBlocks);
            }
        }

        public void Read(Stream stream, bool withBlocks)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                var magic = io.ReadUInt32();
                var reserved1 = io.ReadInt32();
                Size = io.ReadUInt32();
                var reserved2 = io.ReadInt32();
                OffsetFiles = io.ReadUInt32();
                var reserved3 = io.ReadInt32();
                MajorVersion = io.ReadByte();
                MinorVersion = io.ReadByte();

                FolderCount = io.ReadUInt16();
                FileCount = io.ReadUInt16();
                Flags = (CabFlags)io.ReadUInt16();
                SetID = io.ReadUInt16();
                ICabinet = io.ReadUInt16();

                if (Flags.HasFlag(CabFlags.HasReserve))
                {
                    CabinetResBytes = io.ReadUInt16();
                    FolderResBytes = io.ReadByte();
                    DataResBytes = io.ReadByte();

                    CabReserve = io.ReadCString(CabinetResBytes, true);
                }

                if (Flags.HasFlag(CabFlags.HasPrevious))
                {
                    PrevCabName = io.ReadNullTerminatedString();
                    PrevCabDisk = io.ReadNullTerminatedString();
                }

                if (Flags.HasFlag(CabFlags.HasNext))
                {
                    NextCabName = io.ReadNullTerminatedString();
                    NextCabDisk = io.ReadNullTerminatedString();
                }

                var folders = new CabFolderEntry[FolderCount];

                for (int i = 0; i < folders.Length; i++)
                {
                    folders[i] = ReadFolder(io);
                }

                var files = new CabFileEntry[FileCount];

                for (int i = 0; i < files.Length; i++)
                {
                    files[i] = ReadFile(io);
                }

                if (withBlocks)
                {
                    // Jump around and read the blocks for all the folders.
                    // We could read these only when needed, but it's honestly easier this way and the user needs a lot of RAM to run the game anyways.

                    for (int i = 0; i < folders.Length; i++)
                    {
                        var folder = folders[i];
                        var blocks = new CabBlock[folder.BlockCount];

                        io.Seek(SeekOrigin.Begin, folder.BlockOffset);

                        for (int j = 0; j < blocks.Length; j++)
                        {
                            blocks[j] = ReadBlock(io);
                        }

                        folder.Blocks = blocks;
                    }
                }

                Folders = folders;
                Files = files;
            }
        }

        private CabBlock ReadBlock(IoBuffer io)
        {
            io.ReadInt32(); // Checksum
            var compressedSize = io.ReadUInt16();
            return new CabBlock()
            {
                CompressedSize = compressedSize,
                UncompressedSize = io.ReadUInt16(),
                CompressedData = io.ReadBytes(compressedSize)
            };
        }

        private CabFolderEntry ReadFolder(IoBuffer io)
        {
            return new CabFolderEntry()
            {
                BlockOffset = io.ReadUInt32(),
                BlockCount = io.ReadUInt16(),
                CompressionType = io.ReadUInt16(),
                FolderReserve = Flags.HasFlag(CabFlags.HasReserve) ? io.ReadCString(FolderResBytes, true) : null
            };
        }

        private CabFileEntry ReadFile(IoBuffer io)
        {
            return new CabFileEntry()
            {
                Size = io.ReadUInt32(),
                Offset = io.ReadUInt32(),
                FolderID = io.ReadUInt16(),
                Unknown1 = io.ReadUInt16(),
                Unknown2 = io.ReadUInt32(),
                Filename = io.ReadNullTerminatedString().Replace('\\', '/')
            };
        }
    }
}
