using FSO.Files.Utils;
using System;
using System.IO;
using System.Linq;

namespace FSO.Files.Formats.IFF.Chunks
{
    public class PIFF : IffChunk
    {
        public static ushort CURRENT_VERSION = 2;
        public ushort Version = CURRENT_VERSION;
        public string SourceIff;
        public string Comment = "";
        public PIFFEntry[] Entries;

        public PIFF()
        {
            ChunkType = "PIFF";
        }

        public void AppendAddedChunks(IffFile file)
        {
            foreach (var chunk in file.SilentListAll())
            {
                if (chunk == this) continue;
                var entries = Entries.ToList();
                entries.Add(new PIFFEntry()
                {
                    ChunkID = chunk.ChunkID,
                    ChunkLabel = chunk.ChunkLabel,
                    ChunkFlags = chunk.ChunkFlags,
                    EntryType = PIFFEntryType.Add,
                    NewDataSize = (uint)(chunk.ChunkData?.Length ?? 0),
                    Type = chunk.ChunkType
                });
                Entries = entries.ToArray();
            }
        }

        public override void Read(IffFile iff, Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                Version = io.ReadUInt16();
                SourceIff = io.ReadVariableLengthPascalString();
                if (Version > 1) Comment = io.ReadVariableLengthPascalString();
                Entries = new PIFFEntry[io.ReadUInt16()];
                for (int i=0; i<Entries.Length; i++)
                {
                    var e = new PIFFEntry();
                    e.Type = io.ReadCString(4);
                    e.ChunkID = io.ReadUInt16();
                    if (Version > 1) e.Comment = io.ReadVariableLengthPascalString();
                    e.EntryType = (PIFFEntryType)io.ReadByte();

                    if (e.EntryType == PIFFEntryType.Patch)
                    {
                        e.ChunkLabel = io.ReadVariableLengthPascalString();
                        e.ChunkFlags = io.ReadUInt16();
                        if (Version > 0) e.NewChunkID = io.ReadUInt16();
                        else e.NewChunkID = e.ChunkID;
                        e.NewDataSize = io.ReadUInt32();

                        var size = io.ReadUInt32();
                        e.Patches = new PIFFPatch[size];
                        uint lastOff = 0;
                        
                        for (int j=0; j<e.Patches.Length; j++)
                        {
                            var p = new PIFFPatch();

                            p.Offset = lastOff + io.ReadVarLen();
                            lastOff = p.Offset;
                            p.Size = io.ReadVarLen();
                            p.Mode = (PIFFPatchMode)io.ReadByte();

                            if (p.Mode == PIFFPatchMode.Add) p.Data = io.ReadBytes(p.Size);
                            e.Patches[j] = p;
                        }
                    }
                    Entries[i] = e;
                }
            }
        }

        public override bool Write(IffFile iff, Stream stream)
        {
            using (var io = IoWriter.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                io.WriteUInt16(CURRENT_VERSION);
                io.WriteVariableLengthPascalString(SourceIff);
                io.WriteVariableLengthPascalString(Comment);
                io.WriteUInt16((ushort)Entries.Length);
                foreach (var ent in Entries)
                {
                    io.WriteCString(ent.Type, 4);
                    io.WriteUInt16(ent.ChunkID);
                    io.WriteVariableLengthPascalString(ent.Comment);
                    io.WriteByte((byte)(ent.EntryType));

                    if (ent.EntryType == PIFFEntryType.Patch)
                    {
                        io.WriteVariableLengthPascalString(ent.ChunkLabel); //0 length means no replacement
                        io.WriteUInt16(ent.ChunkFlags);
                        io.WriteUInt16(ent.NewChunkID);
                        io.WriteUInt32(ent.NewDataSize);
                        io.WriteUInt32((uint)ent.Patches.Length);

                        uint lastOff = 0;
                        foreach (var p in ent.Patches)
                        {
                            io.WriteVarLen(p.Offset-lastOff);
                            lastOff = p.Offset;
                            io.WriteVarLen(p.Size);
                            io.WriteByte((byte)p.Mode);
                            if (p.Mode == PIFFPatchMode.Add) io.WriteBytes(p.Data);
                        }
                    }
                }
            }
            return true;
        }
    }
    
    public class PIFFEntry
    {
        public string Type;
        public ushort ChunkID;
        public ushort NewChunkID;
        public PIFFEntryType EntryType;
        public string Comment = "";

        public string ChunkLabel;
        public ushort ChunkFlags;
        public uint NewDataSize;

        public PIFFPatch[] Patches;

        public byte[] Apply(byte[] src)
        {
            var result = new byte[NewDataSize];
            uint srcPtr = 0;
            uint destPtr = 0;
            int i = 0;
            foreach (var p in Patches)
            {
                var copyCount = p.Offset - destPtr;
                Array.Copy(src, srcPtr, result, destPtr, copyCount);
                srcPtr += copyCount; destPtr += copyCount;
                if (p.Mode == PIFFPatchMode.Add)
                {
                    Array.Copy(p.Data, 0, result, destPtr, p.Size);
                    destPtr += p.Size;
                } else
                {
                    srcPtr += p.Size;
                }
                i++;
            }
            var remainder = NewDataSize - destPtr;
            if (remainder != 0) Array.Copy(src, srcPtr, result, destPtr, remainder);

            return result;
        }
    }

    public enum PIFFEntryType : byte
    {
        Patch,
        Remove,
        Add
    }

    public struct PIFFPatch
    {
        public uint Offset;
        public uint Size;
        public PIFFPatchMode Mode;
        public byte[] Data;
    }

    public enum PIFFPatchMode : byte
    {
        Remove = 0,
        Add = 1
    }
}
