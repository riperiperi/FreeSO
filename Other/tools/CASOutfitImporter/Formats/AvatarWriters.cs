using System;
using System.Collections.Generic;
using System.IO;

namespace CASOutfitImporter.Formats
{
    // Reference id: ulong with TypeID in low 32 bits, FileID in high 32 bits
    // (matches FAR3Provider/TSOAvatarContentProvider id encoding).
    internal readonly struct ContentRef
    {
        public readonly uint TypeId;
        public readonly uint FileId;
        public ContentRef(uint typeId, uint fileId) { TypeId = typeId; FileId = fileId; }
        public ulong PackedId => ((ulong)FileId << 32) | TypeId;
    }

    // Binding (.bnd) — mirror of FSO.Vitaboy.Binding.Write (BE).
    //   u32 version=1
    //   pascal bone
    //   u32 8        (mesh marker)
    //   u32 meshGroupId, u32 meshFileId, u32 meshTypeId
    //   u32 8        (texture marker)
    //   u32 texGroupId, u32 texFileId, u32 texTypeId
    internal static class BindingWriter
    {
        public static byte[] Write(string bone, ContentRef mesh, ContentRef texture)
        {
            using var ms = new MemoryStream();
            using (var w = new BeWriter(ms))
            {
                w.U32(1);
                w.PascalString(bone ?? "ROOT");
                w.U32(8);
                w.U32(0); w.U32(mesh.FileId); w.U32(mesh.TypeId);
                w.U32(8);
                w.U32(0); w.U32(texture.FileId); w.U32(texture.TypeId);
            }
            return ms.ToArray();
        }
    }

    // Appearance (.apr) — mirror of Appearance.Write (BE).
    //   u32 version=1
    //   u32 thumbFileId, u32 thumbTypeId
    //   u32 numBindings
    //   numBindings × (u32 fileId, u32 typeId)
    internal static class AppearanceWriter
    {
        public static byte[] Write(ContentRef thumbnail, IList<ContentRef> bindings)
        {
            using var ms = new MemoryStream();
            using (var w = new BeWriter(ms))
            {
                w.U32(1);
                w.U32(thumbnail.FileId);
                w.U32(thumbnail.TypeId);
                w.U32((uint)bindings.Count);
                foreach (var b in bindings)
                {
                    w.U32(b.FileId);
                    w.U32(b.TypeId);
                }
            }
            return ms.ToArray();
        }
    }

    // Outfit (.oft) — inverse of Outfit.Read (default IoBuffer = BE).
    //   u32 version
    //   u32 unknown
    //   u32 lightFileId,  u32 lightTypeId
    //   u32 mediumFileId, u32 mediumTypeId
    //   u32 darkFileId,   u32 darkTypeId
    //   u32 handGroup
    //   u32 region        (1 = head, 2 = body — empirical TSO convention)
    internal static class OutfitWriter
    {
        // Region values per TSO data convention; CAS only cares head vs body.
        public const uint RegionHead = 1;
        public const uint RegionBody = 2;

        public static byte[] Write(
            ContentRef light, ContentRef medium, ContentRef dark,
            uint handGroup, uint region,
            uint version = 3)
        {
            using var ms = new MemoryStream();
            using (var w = new BeWriter(ms))
            {
                w.U32(version);
                w.U32(0); // unknown / reserved
                w.U32(light.FileId);   w.U32(light.TypeId);
                w.U32(medium.FileId);  w.U32(medium.TypeId);
                w.U32(dark.FileId);    w.U32(dark.TypeId);
                w.U32(handGroup);
                w.U32(region);
            }
            return ms.ToArray();
        }
    }

    // PurchasableOutfit (.po) — mirror of PurchasableOutfit.Read (BE via Endian.SwapUInt32).
    //   u32 version
    //   u32 gender (0=male, 1=female)
    //   u32 assetIDSize=8
    //   u32 assetIDPrefix (Maxis-historic, ignored)
    //   u64 outfitAssetID  (this is the PACKED id of the .oft: (fileId << 32) | typeId)
    internal static class PurchasableWriter
    {
        public const uint GenderMale = 0;
        public const uint GenderFemale = 1;

        public static byte[] Write(uint gender, ContentRef outfit, uint version = 1)
        {
            using var ms = new MemoryStream();
            using (var w = new BeWriter(ms))
            {
                w.U32(version);
                w.U32(gender);
                w.U32(8);
                w.U32(0);
                w.U64(outfit.PackedId);
            }
            return ms.ToArray();
        }
    }

    // Collection (.col) — inverse of Collection.Read (BE).
    //   i32 count
    //   count × (i32 index, u32 fileId, u32 typeId)
    internal static class CollectionWriter
    {
        public sealed class Entry
        {
            public int Index;
            public uint FileId;
            public uint TypeId;
        }

        public static byte[] Write(IList<Entry> entries)
        {
            using var ms = new MemoryStream();
            using (var w = new BeWriter(ms))
            {
                w.I32(entries.Count);
                foreach (var e in entries)
                {
                    w.I32(e.Index);
                    w.U32(e.FileId);
                    w.U32(e.TypeId);
                }
            }
            return ms.ToArray();
        }

        // Reads an existing .col file (loose, not FAR3-wrapped). BE.
        public static List<Entry> Read(byte[] data)
        {
            using var ms = new MemoryStream(data);
            using var br = new BinaryReader(ms);
            int count = (int)BeWriter.SwapU32(br.ReadUInt32());
            var list = new List<Entry>(count);
            for (int i = 0; i < count; i++)
            {
                int idx = (int)BeWriter.SwapU32(br.ReadUInt32());
                uint fid = BeWriter.SwapU32(br.ReadUInt32());
                uint tid = BeWriter.SwapU32(br.ReadUInt32());
                list.Add(new Entry { Index = idx, FileId = fid, TypeId = tid });
            }
            return list;
        }
    }
}