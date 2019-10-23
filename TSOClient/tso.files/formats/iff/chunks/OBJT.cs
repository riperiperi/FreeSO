using FSO.Files.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Files.Formats.IFF.Chunks
{
    public class OBJT : IffChunk
    {
        //another sims 1 masterpiece. A list of object info.
        public List<OBJTEntry> Entries;

        public override void Read(IffFile iff, Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                var zero = io.ReadInt32();
                var version = io.ReadInt32(); //should be 2/3
                var objt = io.ReadInt32(); //tjbo

                Entries = new List<OBJTEntry>();
                //single tile objects are named. multitile objects arent.

                while (io.HasMore)
                {
                    Entries.Add(new OBJTEntry(io, version));
                }
            }
        }
    }

    public class OBJTEntry
    {
        public uint GUID;
        public uint Unknown1;
        public uint Unknown2;
        public ushort Unknown3;
        public ushort Unknown4;
        public int Unknown5;
        public string Name;
        public OBJTEntry(IoBuffer io, int version)
        {
            //16 bytes of data
            GUID = io.ReadUInt32();
            if (GUID == 0) return;
            Unknown1 = io.ReadUInt32(); //7 a lot
            Unknown2 = io.ReadUInt32(); //131074 a lot
            Unknown3 = io.ReadUInt16(); //increases by one each time, but sometimes skips one. ID?
            Unknown4 = io.ReadUInt16(); //mostly 4, sometimes 8, sometimes 7 (dollhouse). catalog category? 
            //then the name, null terminated
            Name = io.ReadNullTerminatedString();
            if (Name.Length%2 == 0) io.ReadByte(); //pad to short width
            if (version > 2) io.ReadInt32();
        }

        public override string ToString()
        {
            return $"{Name} ({GUID.ToString("x8")})";
        }
    }
}
