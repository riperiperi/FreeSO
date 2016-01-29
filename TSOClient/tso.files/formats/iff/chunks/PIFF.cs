using FSO.Files.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FSO.Files.Formats.IFF.Chunks
{
    public class PIFF : IffChunk
    {
        public string SourceIff;
        public PIFFEntry[] Entries;

        public PIFF()
        {
            ChunkType = "PIFF";
        }

        public override void Read(IffFile iff, Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                SourceIff = io.ReadVariableLengthPascalString();
                Entries = new PIFFEntry[io.ReadUInt16()];
                for (int i=0; i<Entries.Length; i++)
                {
                    var e = new PIFFEntry();
                    e.Type = io.ReadCString(4);
                    e.ChunkID = io.ReadUInt16();
                    e.Delete = io.ReadByte()>0;
                    
                    if (!e.Delete)
                    {
                        e.ChunkLabel = io.ReadCString(64).TrimEnd('\0');
                        e.ChunkFlags = io.ReadUInt16();
                        e.NewDataSize = io.ReadUInt32();

                        e.Patches = new PIFFPatch[io.ReadUInt32()];
                        for (int j=0; j<e.Patches.Length; j++)
                        {
                            var p = new PIFFPatch();
                            
                            p.Offset = io.ReadUInt32();
                            p.Size = io.ReadUInt32();
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
                io.WriteVariableLengthPascalString(SourceIff);
                io.WriteUInt16((ushort)Entries.Length);
                foreach (var ent in Entries)
                {
                    io.WriteCString(ent.Type, 4);
                    io.WriteUInt16(ent.ChunkID);
                    io.WriteByte((byte)(ent.Delete ? 1 : 0));

                    if (!ent.Delete)
                    {
                        io.WriteCString(ent.ChunkLabel, 64);
                        io.WriteUInt16(ent.ChunkFlags);
                        io.WriteUInt32(ent.NewDataSize);
                        io.WriteUInt32((uint)ent.Patches.Length);

                        foreach (var p in ent.Patches)
                        {
                            io.WriteUInt32(p.Offset);
                            io.WriteUInt32(p.Size);
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
        public bool Delete;

        public string ChunkLabel;
        public ushort ChunkFlags;
        public uint NewDataSize;

        public PIFFPatch[] Patches;

        public byte[] Apply(byte[] src)
        {
            var result = new byte[NewDataSize];
            uint srcPtr = 0;
            uint destPtr = 0;
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
            }
            var remainder = NewDataSize - destPtr;
            Array.Copy(src, srcPtr, result, destPtr, remainder);

            return result;
        }
    }

    public class PIFFPatch
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
