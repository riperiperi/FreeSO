using FSO.Files.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Files.Formats.IFF.Chunks
{
    public class TREE : IffChunk
    {
        public List<TREEBox> Entries = new List<TREEBox>();

        public override void Read(IffFile iff, Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                var zero = io.ReadInt32();
                var version = io.ReadInt32();
                if (version != 1) throw new Exception("Unexpected TREE version: " + version);
                string magic = io.ReadCString(4); //HBGN
                if (magic != "EERT") throw new Exception("Magic number should be 'EERT', got " + magic);
                var entryCount = io.ReadInt32();
                Entries.Clear();
                for (int i=0; i<entryCount; i++)
                {
                    var box = new TREEBox();
                    box.Read(io);
                    Entries.Add(box);
                }
            }
        }

        public override bool Write(IffFile iff, Stream stream)
        {
            using (var io = IoWriter.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                io.WriteInt32(0);
                io.WriteInt32(1);
                io.WriteCString("EERT", 4);
                io.WriteInt32(Entries.Count);
                foreach (var entry in Entries)
                {
                    entry.Write(io);
                }
            }
            return true;
        }
    }

    public class TREEBox
    {
        public TREEBoxType Type;
        public ushort Unknown;
        public short Width;
        public short Height;
        public short X;
        public short Y;
        public short CommentSize = 10;
        public ushort Pointer;
        public short Special; //0 or -1... unknown.
        public int NegativeOne = -1;
        public string Comment = "";
        public int TrailingZero = 0;

        public void Read(IoBuffer io)
        {
            Type = (TREEBoxType)io.ReadUInt16();
            Unknown = io.ReadUInt16();
            Width = io.ReadInt16();
            Height = io.ReadInt16();
            X = io.ReadInt16();
            Y = io.ReadInt16();
            CommentSize = io.ReadInt16();
            Pointer = io.ReadUInt16();
            Special = io.ReadInt16();
            NegativeOne = io.ReadInt32();
            Comment = io.ReadNullTerminatedString();
            if (Comment.Length % 2 == 0) io.ReadByte(); //padding to 2 byte align
            TrailingZero = io.ReadInt32();

            if (!Enum.IsDefined(typeof(TREEBoxType), Type)) throw new Exception("Unexpected TREE box type: " + Type.ToString());
            if (Special < -1 || Special > 0) throw new Exception("Unexpected TREE special: " + Special);
            if (Unknown != 0) throw new Exception("Unexpected Unknown: " + Unknown);
            if (NegativeOne != -1) throw new Exception("Unexpected NegativeOne: " + -1);
            if (TrailingZero != 0) throw new Exception("Unexpected TrailingZero: " + 0);
        }

        public void Write(IoWriter io)
        {
            io.WriteUInt16((ushort)Type);
            io.WriteUInt16(Unknown);
            io.WriteInt16(Width);
            io.WriteInt16(Height);
            io.WriteInt16(X);
            io.WriteInt16(Y);
            io.WriteInt16(CommentSize);
            io.WriteUInt16(Pointer);
            io.WriteInt16(Special);
            io.WriteInt32(NegativeOne);
            io.WriteCString(Comment);
            if (Comment.Length % 2 == 0) io.WriteByte(0xCD); //padding to 2 byte align
            io.WriteInt32(TrailingZero);
        }
    }

    public enum TREEBoxType : ushort
    {
        Primitive = 0,
        True = 1,
        False = 2,
        Label = 3
    }
}
