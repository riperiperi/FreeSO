using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CASOutfitImporter.Formats
{
    // Big-endian binary reader matching FSO IoBuffer behavior. All avatar metadata
    // files are big-endian (default ByteOrder = BIG_ENDIAN in IoBuffer).
    internal sealed class BeReader : IDisposable
    {
        private readonly BinaryReader _r;
        public Stream BaseStream { get; }
        public BeReader(Stream s) { BaseStream = s; _r = new BinaryReader(s); }

        public byte U8() => _r.ReadByte();
        public ushort U16() => BeWriter.SwapU16(_r.ReadUInt16());
        public short  I16() => (short)BeWriter.SwapU16((ushort)_r.ReadInt16());
        public uint   U32() => BeWriter.SwapU32(_r.ReadUInt32());
        public int    I32() => (int)BeWriter.SwapU32((uint)_r.ReadInt32());
        public ulong  U64() => BeWriter.SwapU64(_r.ReadUInt64());
        public float  F32() => _r.ReadSingle(); // FloatSwap=false matches IoBuffer

        public string PascalString()
        {
            byte len = _r.ReadByte();
            return Encoding.ASCII.GetString(_r.ReadBytes(len));
        }

        public byte[] ReadBytes(int n) => _r.ReadBytes(n);
        public long Position => BaseStream.Position;
        public long Length => BaseStream.Length;

        public void Dispose() => _r.Dispose();
    }

    // Inverse of BindingWriter.Write.
    internal sealed class BindingFile
    {
        public uint Version;
        public string Bone;
        public uint MeshGroupId, MeshFileId, MeshTypeId;
        public uint TextureGroupId, TextureFileId, TextureTypeId;

        public static BindingFile Read(byte[] data)
        {
            using var r = new BeReader(new MemoryStream(data));
            var b = new BindingFile { Version = r.U32() };
            if (b.Version != 1) throw new InvalidDataException($"binding version {b.Version} unsupported");
            b.Bone = r.PascalString();
            uint meshTag = r.U32();
            if (meshTag != 8) throw new InvalidDataException($"binding meshTag={meshTag}, expected 8");
            b.MeshGroupId = r.U32(); b.MeshFileId = r.U32(); b.MeshTypeId = r.U32();
            uint texTag = r.U32();
            if (texTag != 8) throw new InvalidDataException($"binding texTag={texTag}, expected 8");
            b.TextureGroupId = r.U32(); b.TextureFileId = r.U32(); b.TextureTypeId = r.U32();
            return b;
        }
    }

    // Inverse of AppearanceWriter.Write.
    internal sealed class AppearanceFile
    {
        public uint Version;
        public ContentRef Thumbnail;
        public ContentRef[] Bindings;

        public static AppearanceFile Read(byte[] data)
        {
            using var r = new BeReader(new MemoryStream(data));
            var a = new AppearanceFile { Version = r.U32() };
            uint thumbFile = r.U32();
            uint thumbType = r.U32();
            a.Thumbnail = new ContentRef(thumbType, thumbFile);
            uint n = r.U32();
            a.Bindings = new ContentRef[n];
            for (int i = 0; i < n; i++)
            {
                uint fid = r.U32();
                uint tid = r.U32();
                a.Bindings[i] = new ContentRef(tid, fid);
            }
            return a;
        }
    }

    // Inverse of OutfitWriter.Write.
    internal sealed class OutfitFile
    {
        public uint Version;
        public uint Unknown;
        public ContentRef Light, Medium, Dark;
        public uint HandGroup;
        public uint Region;

        public static OutfitFile Read(byte[] data)
        {
            using var r = new BeReader(new MemoryStream(data));
            var o = new OutfitFile { Version = r.U32(), Unknown = r.U32() };
            uint lf = r.U32(), lt = r.U32();
            uint mf = r.U32(), mt = r.U32();
            uint df = r.U32(), dt = r.U32();
            o.Light  = new ContentRef(lt, lf);
            o.Medium = new ContentRef(mt, mf);
            o.Dark   = new ContentRef(dt, df);
            o.HandGroup = r.U32();
            o.Region    = r.U32();
            return o;
        }
    }

    // Inverse of PurchasableWriter.Write.
    internal sealed class PurchasableFile
    {
        public uint Version;
        public uint Gender;
        public uint AssetIdSize;
        public uint Prefix;
        public ulong OutfitPackedId;

        public ContentRef Outfit
            => new ContentRef((uint)(OutfitPackedId & 0xFFFFFFFF), (uint)(OutfitPackedId >> 32));

        public static PurchasableFile Read(byte[] data)
        {
            using var r = new BeReader(new MemoryStream(data));
            var p = new PurchasableFile
            {
                Version = r.U32(),
                Gender = r.U32(),
                AssetIdSize = r.U32(),
                Prefix = r.U32(),
                OutfitPackedId = r.U64()
            };
            if (p.AssetIdSize != 8) throw new InvalidDataException($"purchasable assetIdSize={p.AssetIdSize}, expected 8");
            return p;
        }
    }

    // Mesh.Read for bmf=false (TSO .mesh path). We only need to validate the file
    // parses cleanly to EOF; geometry data is consumed without retention.
    internal sealed class MeshSummary
    {
        public int Version;
        public List<string> BoneNames;
        public int FaceCount;
        public int BindingCount;
        public int RealVertexCount;
        public int BlendVertexCount;
        public long BytesRead;
        public long FileLength;

        public static MeshSummary Read(byte[] data)
        {
            using var r = new BeReader(new MemoryStream(data));
            var m = new MeshSummary { FileLength = data.Length };
            m.Version = r.I32();
            int boneCount = r.I32();
            m.BoneNames = new List<string>(boneCount);
            for (int i = 0; i < boneCount; i++) m.BoneNames.Add(r.PascalString());

            m.FaceCount = r.I32();
            for (int i = 0; i < m.FaceCount * 3; i++) r.I32();

            m.BindingCount = r.I32();
            for (int i = 0; i < m.BindingCount; i++)
            {
                r.I32(); r.I32(); r.I32(); r.I32(); r.I32();
            }

            m.RealVertexCount = r.I32();
            for (int i = 0; i < m.RealVertexCount; i++) { r.F32(); r.F32(); }

            m.BlendVertexCount = r.I32();
            for (int i = 0; i < m.BlendVertexCount; i++) { r.I32(); r.I32(); }

            int realVertexCount2 = r.I32();
            // FSO ignores realVertexCount2 and iterates RealVertexCount real verts
            // followed by BlendVertexCount blend verts — replicate exactly.
            for (int i = 0; i < m.RealVertexCount; i++) { r.F32(); r.F32(); r.F32(); r.F32(); r.F32(); r.F32(); }
            for (int i = 0; i < m.BlendVertexCount; i++) { r.F32(); r.F32(); r.F32(); r.F32(); r.F32(); r.F32(); }

            m.BytesRead = r.Position;
            return m;
        }
    }
}