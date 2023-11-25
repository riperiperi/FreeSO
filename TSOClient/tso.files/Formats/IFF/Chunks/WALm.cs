using FSO.Files.Utils;
using System.Collections.Generic;
using System.IO;

namespace FSO.Files.Formats.IFF.Chunks
{
    /// <summary>
    /// WALm and FLRm chunks, used for mapping walls and floors in ARRY chunks to walls and floors in resource files (outwith floors.iff)
    /// </summary>
    public class WALm : IffChunk
    {
        public List<WALmEntry> Entries;

        public override void Read(IffFile iff, Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                var zero = io.ReadInt32();
                var version = io.ReadInt32(); //should be 0
                var walm = io.ReadInt32(); //mLAW/mRLF

                var count = io.ReadInt32();
                Entries = new List<WALmEntry>();

                for (int i=0; i<count; i++)
                {
                    //size of fields depends on chunk id.
                    Entries.Add(new WALmEntry(io, ChunkID));
                }
            }
        }
    }

    public class FLRm : WALm
    {
        //literally no difference
    }

    public class WALmEntry
    {
        public string Name;
        public int Unknown; //usually 1
        public byte ID;
        public byte[] Unknown2;
        public WALmEntry(IoBuffer io, int id)
        {
            Name = io.ReadNullTerminatedString();
            if (Name.Length % 2 == 0) io.ReadByte(); //pad to short width
            Unknown = io.ReadInt32(); //index in iff?
            ID = io.ReadByte();
            Unknown2 = io.ReadBytes(5 + id * 2);

            //id 0 seems to be an older format
            //unknown2 is 01 00 00 00 00 00
            //id 1 adds more fields
            //unknown2 is 01 01 00 00 00 00 00 00

            //related to number of walls or floors in the file?
        }
    }
}
