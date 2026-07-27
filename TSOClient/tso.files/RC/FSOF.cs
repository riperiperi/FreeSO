using FSO.Common;
using FSO.Common.Utils;
using FSO.Files.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;

namespace FSO.Files.RC
{
    public class FSOF
    {
        // Constants for FSOF server validation
        private const int EXPECTED_FLOOR_WIDTH = 384;
        private const int EXPECTED_FLOOR_HEIGHT = 256;
        private const int EXPECTED_WALL_WIDTH = 512;
        private const int MAX_WALL_HEIGHT = 4096;

        private const int MAX_FLOOR_TRIS = 10000;
        private const int MAX_WALL_TRIS = 100000;

        private const float MAX_XZ_POSITION = 77;
        private const float MAX_Y_POSITION = 300;

        public int TexCompressionType; //RGBA8, DXT5

        public int FloorWidth;
        public int FloorHeight;
        public int WallWidth;
        public int WallHeight;

        public Color NightLightColor;

        public byte[] FloorTextureData;
        public byte[] WallTextureData;

        public byte[] NightFloorTextureData;
        public byte[] NightWallTextureData;

        public int[] FloorIndices;
        public DGRP3DVert[] FloorVertices;

        public int[] WallIndices;
        public DGRP3DVert[] WallVertices;

        //loaded data
        public Texture2D FloorTexture;
        public Texture2D WallTexture;
        public Texture2D NightFloorTexture;
        public Texture2D NightWallTexture;

        public VertexBuffer FloorVGPU;
        public IndexBuffer FloorIGPU;
        public int FloorPrims;

        public VertexBuffer WallVGPU;
        public IndexBuffer WallIGPU;
        public int WallPrims;

        public static int CURRENT_VERSION = 1;
        public int Version = CURRENT_VERSION;
        public bool Compressed = true;

        public void Save(Stream stream)
        {
            var io = IoWriter.FromStream(stream, ByteOrder.LITTLE_ENDIAN);
            io.WriteCString("FSOf", 4);
            io.WriteInt32(CURRENT_VERSION);

            io.WriteByte((byte)(Compressed ? 1 : 0));

            MemoryStream target = null;
            GZipStream compressed = null;
            var cio = io;
            if (Compressed)
            {
                //target = new MemoryStream();
                compressed = new GZipStream(stream, CompressionMode.Compress);
                cio = IoWriter.FromStream(compressed, ByteOrder.LITTLE_ENDIAN);
            }

            cio.WriteInt32(TexCompressionType);
            cio.WriteInt32(FloorWidth);
            cio.WriteInt32(FloorHeight);
            cio.WriteInt32(WallWidth);
            cio.WriteInt32(WallHeight);
            cio.WriteByte((byte)((NightFloorTextureData == null)?0:1)); //has night tex?

            cio.WriteInt32(FloorTextureData.Length);
            cio.WriteBytes(FloorTextureData);
            cio.WriteInt32(WallTextureData.Length);
            cio.WriteBytes(WallTextureData);

            if (NightFloorTextureData != null)
            {
                cio.WriteInt32(NightFloorTextureData.Length);
                cio.WriteBytes(NightFloorTextureData);
                cio.WriteInt32(NightWallTextureData.Length);
                cio.WriteBytes(NightWallTextureData);
                cio.WriteUInt32(NightLightColor.PackedValue);
            }

            WriteVerts(FloorVertices, FloorIndices, cio);
            WriteVerts(WallVertices, WallIndices, cio);

            if (Compressed)
            {
                compressed.Close();
            }
        }

        public void ValidateFSO(Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                var fsof = io.ReadCString(4);
                if (fsof != "FSOf") throw new Exception("Invalid FSOf!");
                Version = io.ReadInt32();
                Compressed = io.ReadByte() > 0;

                if (Version < 0 || Version > CURRENT_VERSION)
                {
                    throw new Exception("Unknown FSOF Version");
                }

                GZipStream compressed = null;
                var cio = io;
                if (Compressed)
                {
                    compressed = new GZipStream(stream, CompressionMode.Decompress);
                    cio = IoBuffer.FromStream(compressed, ByteOrder.LITTLE_ENDIAN);
                }

                TexCompressionType = cio.ReadInt32();
                if (!(TexCompressionType == 1))
                {
                    throw new Exception("FSO compression must be 1 (DXT)");
                }
                FloorWidth = cio.ReadInt32();
                FloorHeight = cio.ReadInt32();

                if (!(FloorWidth == EXPECTED_FLOOR_WIDTH && FloorHeight == EXPECTED_FLOOR_HEIGHT))
                {
                    throw new Exception("Unexpected dimensions for floor texture");
                }

                WallWidth = cio.ReadInt32();
                WallHeight = cio.ReadInt32();

                if (!(WallWidth == EXPECTED_WALL_WIDTH && WallHeight <= MAX_WALL_HEIGHT))
                {
                    throw new Exception("Unexpected dimensions for wall texture");
                }

                var hasNight = cio.ReadByte() > 0;

                if (!hasNight)
                {
                    throw new Exception("Must have night texture");
                }

                var floorTSize = cio.ReadInt32();
                cio.ReadBytes(floorTSize);
                var wallTSize = cio.ReadInt32();
                cio.ReadBytes(wallTSize);

                if (!(floorTSize == DXTSize(FloorWidth, FloorHeight) && wallTSize == DXTSize(WallWidth, WallHeight)))
                {
                    throw new Exception("Day wall/floor have incorrect size for DXT");
                }

                if (hasNight)
                {
                    floorTSize = cio.ReadInt32();
                    cio.ReadBytes(floorTSize);
                    wallTSize = cio.ReadInt32();
                    cio.ReadBytes(wallTSize);

                    if (!(floorTSize == DXTSize(FloorWidth, FloorHeight) && wallTSize == DXTSize(WallWidth, WallHeight)))
                    {
                        throw new Exception("Night wall/floor have incorrect size for DXT");
                    }

                    NightLightColor = new Color(cio.ReadUInt32());
                }

                var floor = ValidateVerts(cio, MAX_FLOOR_TRIS * 3, MAX_FLOOR_TRIS * 3);
                FloorVertices = floor.Item1;
                FloorIndices = floor.Item2;
                var wall = ValidateVerts(cio, MAX_WALL_TRIS * 3, MAX_WALL_TRIS * 3);
                WallVertices = wall.Item1;
                WallIndices = wall.Item2;
            }
        }

        private static int DXTSize(int width, int height)
        {
            width = ((width + 3) >> 2) << 2;
            height = ((height + 3) >> 2) << 2;

            return width * height;
        }

        public void Read(Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                var fsof = io.ReadCString(4);
                if (fsof != "FSOf") throw new Exception("Invalid FSOf!");
                Version = io.ReadInt32();
                Compressed = io.ReadByte() > 0;

                GZipStream compressed = null;
                var cio = io;
                if (Compressed)
                {
                    compressed = new GZipStream(stream, CompressionMode.Decompress);
                    cio = IoBuffer.FromStream(compressed, ByteOrder.LITTLE_ENDIAN);
                }

                TexCompressionType = cio.ReadInt32();
                FloorWidth = cio.ReadInt32();
                FloorHeight = cio.ReadInt32();
                WallWidth = cio.ReadInt32();
                WallHeight = cio.ReadInt32();
                var hasNight = cio.ReadByte() > 0;

                var floorTSize = cio.ReadInt32();
                FloorTextureData = cio.ReadBytes(floorTSize);
                var wallTSize = cio.ReadInt32();
                WallTextureData = cio.ReadBytes(wallTSize);

                if (hasNight)
                {
                    floorTSize = cio.ReadInt32();
                    NightFloorTextureData = cio.ReadBytes(floorTSize);
                    wallTSize = cio.ReadInt32();
                    NightWallTextureData = cio.ReadBytes(wallTSize);
                    NightLightColor = new Color(cio.ReadUInt32());
                }

                var floor = ReadVerts(cio);
                FloorVertices = floor.Item1;
                FloorIndices = floor.Item2;
                var wall = ReadVerts(cio);
                WallVertices = wall.Item1;
                WallIndices = wall.Item2;
            }
        }

        private SurfaceFormat GetTexFormat()
        {
            return (TexCompressionType == 1) ? SurfaceFormat.Dxt5 : SurfaceFormat.Color;
        }

        public void LoadGPU(GraphicsDevice gd)
        {
            var format = GetTexFormat();
            if (format == SurfaceFormat.Dxt5 && !FSOEnvironment.TexCompressSupport)
            {
                //todo: software decode DXT5
                FloorTexture = new Texture2D(gd, FloorWidth, FloorHeight, false, SurfaceFormat.Color);
                FloorTexture.SetData(TextureUtils.DXT5Decompress(FloorTextureData, FloorWidth, FloorHeight));
                WallTexture = new Texture2D(gd, WallWidth, WallHeight, false, SurfaceFormat.Color);
                WallTexture.SetData(TextureUtils.DXT5Decompress(WallTextureData, WallWidth, WallHeight));
                if (NightFloorTextureData != null)
                {
                    NightFloorTexture = new Texture2D(gd, FloorWidth, FloorHeight, false, SurfaceFormat.Color);
                    NightFloorTexture.SetData(TextureUtils.DXT5Decompress(NightFloorTextureData, FloorWidth, FloorHeight));
                    NightWallTexture = new Texture2D(gd, WallWidth, WallHeight, false, SurfaceFormat.Color);
                    NightWallTexture.SetData(TextureUtils.DXT5Decompress(NightWallTextureData, WallWidth, WallHeight));
                }
            }
            else
            {
                FloorTexture = new Texture2D(gd, FloorWidth, FloorHeight, false, format);
                FloorTexture.SetData(FloorTextureData);
                WallTexture = new Texture2D(gd, WallWidth, WallHeight, false, format);
                WallTexture.SetData(WallTextureData);
                if (NightFloorTextureData != null)
                {
                    NightFloorTexture = new Texture2D(gd, FloorWidth, FloorHeight, false, format);
                    NightFloorTexture.SetData(NightFloorTextureData);
                    NightWallTexture = new Texture2D(gd, WallWidth, WallHeight, false, format);
                    NightWallTexture.SetData(NightWallTextureData);
                }
            }

            if (FloorVertices.Length > 0)
            {
                FloorVGPU = new VertexBuffer(gd, typeof(DGRP3DVert), FloorVertices.Length, BufferUsage.None);
                FloorVGPU.SetData(FloorVertices);
                FloorIGPU = new IndexBuffer(gd, IndexElementSize.ThirtyTwoBits, FloorIndices.Length, BufferUsage.None);
                FloorIGPU.SetData(FloorIndices);
                FloorPrims = FloorIndices.Length / 3;
            }

            if (WallVertices.Length > 0)
            {
                WallVGPU = new VertexBuffer(gd, typeof(DGRP3DVert), WallVertices.Length, BufferUsage.None);
                WallVGPU.SetData(WallVertices);
                WallIGPU = new IndexBuffer(gd, IndexElementSize.ThirtyTwoBits, WallIndices.Length, BufferUsage.None);
                WallIGPU.SetData(WallIndices);
                WallPrims = WallIndices.Length / 3;
            }
        }

        public void Dispose()
        {
            FloorTexture?.Dispose();
            WallTexture?.Dispose();
            NightFloorTexture?.Dispose();
            NightWallTexture?.Dispose();
            FloorVGPU?.Dispose();
            FloorIGPU?.Dispose();
            WallVGPU?.Dispose();
            WallIGPU?.Dispose();
        }

        private static Tuple<DGRP3DVert[], int[]> ValidateVerts(IoBuffer io, int maxVerts, int maxIndices)
        {
            var vertCount = io.ReadInt32();
            if (vertCount > maxVerts)
            {
                throw new Exception("Too many vertices");
            }

            var readVerts = ReadArray<DGRP3DVert>(io, vertCount);

            var indCount = io.ReadInt32();
            if (indCount > maxIndices)
            {
                throw new Exception("Too many indices");
            }

            var indices = ReadArray<int>(io, indCount);

            bool valid = true;
            foreach (int ind in indices)
            {
                if (ind < 0 || ind >= vertCount)
                {
                    valid = false;
                    break;
                }
            }

            if (!valid)
            {
                throw new Exception("Indices go out of bounds");
            }

            // Basic dimensions check

            float maxNormalMagnitude = 1.01f * 1.01f;

            foreach (var vert in readVerts)
            {
                if (float.IsNaN(vert.Position.X) || float.IsNaN(vert.Position.Y) || float.IsNaN(vert.Position.Z) || float.IsNaN(vert.Normal.X) || float.IsNaN(vert.Normal.Y) || float.IsNaN(vert.Normal.Z) || float.IsNaN(vert.TextureCoordinate.X) || float.IsNaN(vert.TextureCoordinate.Y))
                {
                    valid = false;
                    break;
                }

                if (vert.Position.X < 0 || vert.Position.Z < 0 || vert.Position.X > MAX_XZ_POSITION || vert.Position.Z > MAX_XZ_POSITION || vert.Position.Y < -MAX_Y_POSITION || vert.Position.Y > MAX_Y_POSITION)
                {
                    valid = false;
                    break;
                }

                if (vert.Normal.LengthSquared() > maxNormalMagnitude)
                {
                    valid = false;
                    break;
                }

                if (vert.TextureCoordinate.X < 0.0 || vert.TextureCoordinate.X > 1.0 || vert.TextureCoordinate.Y < 0.0 || vert.TextureCoordinate.Y > 1.0)
                {
                    valid = false;
                    break;
                }
            }

            if (!valid)
            {
                throw new Exception("Vertex data out of range");
            }

            return new Tuple<DGRP3DVert[], int[]>(readVerts, indices);
        }

        private Tuple<DGRP3DVert[], int[]> ReadVerts(IoBuffer io)
        {
            var vertCount = io.ReadInt32();
            var readVerts = ReadArray<DGRP3DVert>(io, vertCount);

            var indCount = io.ReadInt32();
            var indices = ReadArray<int>(io, indCount);

            return new Tuple<DGRP3DVert[], int[]>(readVerts, indices);
        }

        private void WriteVerts(DGRP3DVert[] verts, int[] indices, IoWriter io)
        {
            io.WriteInt32(verts.Length);
            WriteArray(io, verts);

            io.WriteInt32(indices.Length);
            WriteArray(io, indices);
        }

        public static T[] ReadArray<T>(IoBuffer reader, int size) where T : unmanaged
        {
            var result = new T[size];
            var bytes = MemoryMarshal.Cast<T, byte>(result);

            reader.ReadBytes(bytes);

            return result;
        }

        public static void WriteArray<T>(IoWriter writer, T[] data) where T : unmanaged
        {
            var bytes = MemoryMarshal.Cast<T, byte>(data);

            writer.WriteBytes(bytes);
        }
    }
}
