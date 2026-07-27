using FSO.Common;
using FSO.Common.Rendering;
using FSO.Common.Serialization;
using FSO.Common.Utils;
using FSO.Files.RC;
using FSO.Files.Utils;
using ICSharpCode.SharpZipLib.GZip;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace FSO.Files.Formats.IFF.Chunks
{
    public enum MTX2Format : byte
    {
        RGBA = 0,
        DXT1 = 1,
        DXT5 = 2
    }

    public enum MTX2CompressionType : byte
    {
        None = 0,
        GZip = 1,
    }

    public class MTX2 : IffChunk, IDGRP3DTextureHolder
    {
        public const int CURRENT_VERSION = 1;

        private byte[] Data;

        public int Version = CURRENT_VERSION;
        public int Width;
        public int Height;
        public MTX2Format Format;
        public MTX2CompressionType Compression;
        public int[] LevelOffsets;

        private byte[] Decoded;
        private Texture2D Cached;

        private bool HasDecoded = false;

        public MTX2()
        {

        }

        public override void Read(IffFile iff, Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                var mtx2 = io.ReadCString(4);
                if (mtx2 != "MTX2") throw new Exception("Invalid MTX2!");
                Version = io.ReadInt32();
                Width = io.ReadInt32();
                Height = io.ReadInt32();
                Format = (MTX2Format)io.ReadByte();
                Compression = (MTX2CompressionType)io.ReadByte();
                var levelCount = io.ReadInt32();
                LevelOffsets = new int[levelCount];
                for (int i = 0; i < levelCount; i++)
                {
                    LevelOffsets[i] = io.ReadInt32();
                }
                int dataLength = io.ReadInt32();
                Data = io.ReadBytes(dataLength);
            }
        }

        public override bool Write(IffFile iff, Stream stream)
        {
            using (var io = IoWriter.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                io.WriteCString("MTX2", 4);
                io.WriteInt32(Version);
                io.WriteInt32(Width);
                io.WriteInt32(Height);
                io.WriteByte((byte)Format);
                io.WriteByte((byte)Compression);
                io.WriteInt32(LevelOffsets.Length);
                for (int i = 0; i < LevelOffsets.Length; i++)
                {
                    io.WriteInt32(LevelOffsets[i]);
                }

                if (Data == null)
                {
                    // Encode data
                    Data = Compress(Decoded);
                }

                io.WriteInt32(Data.Length);
                io.WriteBytes(Data);
            }
            return true;
        }

        private byte[] Compress(byte[] data)
        {
            switch (Compression)
            {
                case MTX2CompressionType.None:
                    return data;
                case MTX2CompressionType.GZip:
                    using (var compressed = new MemoryStream())
                    using (var cStream = new GZipStream(compressed, CompressionMode.Compress))
                    using (var srcStream = new MemoryStream(data))
                    {
                        srcStream.CopyTo(cStream);

                        cStream.Close();

                        return compressed.ToArray();
                    }
                default:
                    throw new NotSupportedException($"Unknown MTX2 compression type {Compression}");
            }
        }

        private byte[] Decompress(byte[] data)
        {
            switch (Compression)
            {
                case MTX2CompressionType.None:
                    return data;
                case MTX2CompressionType.GZip:
                    using (var compressed = new MemoryStream(data))
                    using (var cStream = new GZipStream(compressed, CompressionMode.Decompress))
                    using (var dstStream = new MemoryStream())
                    {
                        cStream.CopyTo(dstStream);

                        return dstStream.ToArray();
                    }
                default:
                    throw new NotSupportedException($"Unknown MTX2 compression type {Compression}");
            }
        }

        private int DecodingState;

        public void Decode(GraphicsDevice gd)
        {
            var exch = Interlocked.CompareExchange(ref DecodingState, 1, 0);
            if (exch > 0)
            {
                // Can't decode more than once.
                SpinWait wait = default;
                while (exch == 1)
                {
                    wait.SpinOnce();
                    exch = Volatile.Read(ref DecodingState);
                }

                return;
            }

            Decoded = Decompress(Data);

            if (!IffFile.RETAIN_CHUNK_DATA)
            {
                Data = null;
            }

            Interlocked.Exchange(ref DecodingState, 2);
            HasDecoded = true;
        }

        private SurfaceFormat GetSurfaceFormat()
        {
            return Format switch
            {
                MTX2Format.RGBA => SurfaceFormat.Color,
                MTX2Format.DXT1 => SurfaceFormat.Dxt1,
                MTX2Format.DXT5 => SurfaceFormat.Dxt5,
                _ => throw new NotSupportedException($"Unknown MTX2 format {Format}")
            };
        }

        private TextureData<T>[] GetTextureData<T>(int multiplier = 1) where T : unmanaged
        {
            return [.. LevelOffsets.Select((x, index) =>
            {
                var decoded = Decoded.AsSpan();
                return new TextureData<T>(
                    index,
                    MemoryMarshal.Cast<byte, T>(
                        decoded[x..(index == LevelOffsets.Length - 1 ? decoded.Length : LevelOffsets[index + 1])]
                        ).ToArray(),
                    multiplier);
            })];
        }

        public Texture2D GetTexture(GraphicsDevice gd)
        {
            if (Cached == null)
            {
                if (!HasDecoded)
                {
                    Decode(gd);
                }

                if (Decoded != null)
                {
                    bool compressed = Format != MTX2Format.RGBA;

                    int alignedWidth = compressed ? TextureUtils.AlignUp(Width, 4) : Width;
                    int alignedHeight = compressed ? TextureUtils.AlignUp(Height, 4) : Height;

                    Cached = new Texture2D(gd, alignedWidth, alignedHeight, LevelOffsets.Length > 1, GetSurfaceFormat());

                    if (Format == MTX2Format.RGBA)
                    {
                        TextureUtils.UploadTexData(Cached, GetTextureData<Color>());
                    }
                    else
                    {
                        TextureUtils.UploadTexData(Cached, GetTextureData<byte>(Format == MTX2Format.DXT1 ? 1 : 1));
                    }

                    if (alignedWidth != Width || alignedHeight != Height)
                    {
                        Cached.Tag = new TextureInfo(Cached, Width, Height);
                    }

                    Decoded = null;
                }
            }

            return Cached;
        }

        public void SetData(MTX2Format format, TextureData<byte>[] data)
        {
            Format = format;

            var size = data.Sum(level => level.Data.Length);

            var result = new byte[size];
            LevelOffsets = new int[data.Length];
            int offset = 0;

            for (int i = 0; i < data.Length; i++)
            {
                var item = data[i].Data;

                LevelOffsets[i] = offset;
                item.AsSpan().CopyTo(result.AsSpan(offset, item.Length));

                offset += item.Length;
            }

            Decoded = result;
        }
    }
}
