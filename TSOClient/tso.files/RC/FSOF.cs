using FSO.Files.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Files.RC
{
    public class FSOF
    {
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

        public VertexBuffer WallVGPU;
        public IndexBuffer WallIGPU;

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

        private Tuple<DGRP3DVert[], int[]> ReadVerts(IoBuffer io)
        {
            var vertCount = io.ReadInt32();
            var bytes = io.ReadBytes(vertCount * Marshal.SizeOf(typeof(DGRP3DVert)));
            var readVerts = new DGRP3DVert[vertCount];
            var pinnedHandle = GCHandle.Alloc(readVerts, GCHandleType.Pinned);
            Marshal.Copy(bytes, 0, pinnedHandle.AddrOfPinnedObject(), bytes.Length);
            pinnedHandle.Free();

            var indCount = io.ReadInt32();
            var indices = ToTArray<int>(io.ReadBytes(indCount * 4));

            return new Tuple<DGRP3DVert[], int[]>(readVerts, indices);
        }

        private void WriteVerts(DGRP3DVert[] verts, int[] indices, IoWriter io)
        {
            io.WriteInt32(verts.Length);
            foreach (var vert in verts)
            {
                io.WriteFloat(vert.Position.X);
                io.WriteFloat(vert.Position.Y);
                io.WriteFloat(vert.Position.Z);
                io.WriteFloat(vert.TextureCoordinate.X);
                io.WriteFloat(vert.TextureCoordinate.Y);
                io.WriteFloat(vert.Normal.X);
                io.WriteFloat(vert.Normal.Y);
                io.WriteFloat(vert.Normal.Z);
            }

            io.WriteInt32(indices.Length);
            io.WriteBytes(ToByteArray(indices.ToArray()));
        }

        private static T[] ToTArray<T>(byte[] input)
        {
            var result = new T[input.Length / Marshal.SizeOf(typeof(T))];
            Buffer.BlockCopy(input, 0, result, 0, input.Length);
            return result;
        }

        private static byte[] ToByteArray<T>(T[] input)
        {
            var result = new byte[input.Length * Marshal.SizeOf(typeof(T))];
            Buffer.BlockCopy(input, 0, result, 0, result.Length);
            return result;
        }
    }
}
