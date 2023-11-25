using System.IO;
using FSO.Files.Utils;

namespace FSO.Files.Formats.IFF.Chunks
{
    /// <summary>
    /// Provides labels for BCON constants with the same resource ID.
    /// </summary>
    public class TRCN : IffChunk
    {
        public int Version;
        public TRCNEntry[] Entries;

        /// <summary>
        /// Reads a BCON chunk from a stream.
        /// </summary>
        /// <param name="iff">An Iff instance.</param>
        /// <param name="stream">A Stream instance holding a BCON.</param>
        public override void Read(IffFile iff, Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                var zero = io.ReadInt32();
                Version = io.ReadInt32();
                var magic = io.ReadInt32();
                var count = io.ReadInt32();
                Entries = new TRCNEntry[count];
                for (int i=0; i<count; i++)
                {
                    var entry = new TRCNEntry();
                    entry.Read(io, Version, i > 0 && Version > 0);
                    Entries[i] = entry;
                }
            }
        }

        public override bool Write(IffFile iff, Stream stream)
        {
            using (var io = IoWriter.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                io.WriteInt32(0);
                io.WriteInt32(2); //we write out version 2
                io.WriteInt32(0); //todo: NCRT ascii
                io.WriteInt32(Entries.Length);
                foreach (var entry in Entries)
                {
                    entry.Write(io);
                }

                return true;
            }
        }
    }

    public class TRCNEntry
    {
        public int Flags;
        public int Unknown;
        public string Label = "";
        public string Comment = "";

        public byte RangeEnabled; //v1+ only
        public short LowRange;
        public short HighRange = 100;

        public void Read(IoBuffer io, int version, bool odd)
        {
            Flags = io.ReadInt32();
            Unknown = io.ReadInt32();
            Label = (version > 1) ? io.ReadVariableLengthPascalString() : io.ReadNullTerminatedString();
            if (version < 2 && ((Label.Length % 2 == 0) ^ odd)) io.ReadByte();
            Comment = (version > 1) ? io.ReadVariableLengthPascalString() : io.ReadNullTerminatedString();
            if (version < 2 && (Comment.Length % 2 == 0)) io.ReadByte();

            if (version > 0)
            {
                RangeEnabled = io.ReadByte();
                LowRange = io.ReadInt16();
                HighRange = io.ReadInt16();
                //io.ReadByte();
            }
        }
        
        public void Write(IoWriter io)
        {
            io.WriteInt32(Flags);
            io.WriteInt32(Unknown);
            io.WriteVariableLengthPascalString(Label);
            io.WriteVariableLengthPascalString(Comment);

            io.WriteByte(RangeEnabled);
            io.WriteInt16(LowRange);
            io.WriteInt16(HighRange);
        }
    }
}
