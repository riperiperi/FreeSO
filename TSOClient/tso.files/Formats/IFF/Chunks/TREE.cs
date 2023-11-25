using FSO.Files.Utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace FSO.Files.Formats.IFF.Chunks
{
    public class TREE : IffChunk
    {
        public static TREE GenerateEmpty(BHAV bhav)
        {
            var result = new TREE();
            result.ChunkLabel = "";
            result.ChunkID = bhav.ChunkID;
            result.AddedByPatch = true;
            result.ChunkProcessed = true;
            result.RuntimeInfo = ChunkRuntimeState.Modified;
            result.ChunkType = "TREE";

            result.CorrectConnections(bhav);
            return result;
            /*
            var additionID = bhav.Instructions.Length;

            Func<byte, short> resolveTrueFalse = (byte pointer) =>
            {
                switch (pointer)
                {
                    case 253:
                        return -1;
                    case 255:
                        //generate false 
                    case 254:
                        //generate true
                }
                if (pointer == 255) return -1;
                else if (pointer == 2)
            };

            //make an entry for each instruction. positions and sizes don't matter - we have a runtime flag to indicate they are not valid
            for (int i=0; i<bhav.Instructions.Length; i++)
            {
                var inst = bhav.Instructions[i];
                var box = new TREEBox(result);
                box.InternalID = i;
                box.PosisionInvalid = true;
                box.Type = TREEBoxType.Primitive;
                box.TruePointer = 
            }
            */
        }

        public List<TREEBox> Entries = new List<TREEBox>();
        public int PrimitiveCount => Entries.FindLastIndex(x => x.Type == TREEBoxType.Primitive) + 1;

        //runtime
        public uint TreeVersion = 0;

        public override void Read(IffFile iff, Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                var zero = io.ReadInt32();
                var version = io.ReadInt32();
                if (version > 1) throw new Exception("Unexpected TREE version: " + version);
                string magic = io.ReadCString(4); //HBGN
                if (magic != "EERT") throw new Exception("Magic number should be 'EERT', got " + magic);
                var entryCount = io.ReadInt32();
                Entries.Clear();
                for (int i=0; i<entryCount; i++)
                {
                    var box = new TREEBox(this);
                    box.Read(io, version);
                    box.InternalID = (short)i;
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

        public void ApplyPointerDelta(int delta, int after)
        {
            foreach (var box in Entries)
            {
                if (box.InternalID >= after) box.InternalID += (short)delta;
                if (box.TruePointer >= after) box.TruePointer += (short)delta;
                if (box.FalsePointer >= after) box.FalsePointer += (short)delta;
            }
        }

        public void CorrectConnections(BHAV bhav)
        {
            //make sure there are enough primitives for the bhav
            var realPrimCount = bhav.Instructions.Length;
            var treePrimCount = Entries.FindLastIndex(x => x.Type == TREEBoxType.Primitive) + 1;

            ApplyPointerDelta(realPrimCount-treePrimCount, treePrimCount);
            if (realPrimCount > treePrimCount)
            {
                //add new treeboxes
                for (int i=treePrimCount; i<realPrimCount; i++)
                {
                    var box = new TREEBox(this);
                    box.InternalID = (short)i;
                    box.PosisionInvalid = true;
                    box.Type = TREEBoxType.Primitive;
                    Entries.Insert(i, box);
                }
            }
            else if (treePrimCount > realPrimCount)
            {
                //remove treeboxes
                for (int i=treePrimCount; i>realPrimCount; i--)
                {
                    Entries.RemoveAt(i-1);
                }
            }

            //make sure connections for each of the primitives match the BHAV
            //if they don't, reconnect them or generate new boxes (true/false endpoints, maybe gotos in future)

            for (int i=0; i<realPrimCount; i++)
            {
                var prim = bhav.Instructions[i];
                var box = Entries[i];

                if (prim.TruePointer != GetTrueID(box.TruePointer))
                {
                    box.TruePointer = GetCorrectBox(prim.TruePointer);
                }
                if (prim.FalsePointer != GetTrueID((short)box.FalsePointer))
                {
                    box.FalsePointer = GetCorrectBox(prim.FalsePointer);
                }
            }
        }

        public void DeleteBox(TREEBox box)
        {
            //remove box. apply delta
            var id = box.InternalID;
            foreach (var box2 in Entries)
            {
                if (box2.TruePointer == id) box2.TruePointer = -1;
                if (box2.FalsePointer == id) box2.FalsePointer = -1;
            }
            Entries.RemoveAt(id);
            ApplyPointerDelta(-1, id);
        }

        public void InsertPrimitiveBox(TREEBox box)
        {
            var primEnd = PrimitiveCount;
            ApplyPointerDelta(1, primEnd);

            box.InternalID = (short)primEnd;
            Entries.Insert(primEnd, box);
        }

        public void InsertRemovedBox(TREEBox box)
        {
            var oldIndex = box.InternalID;
            ApplyPointerDelta(1, oldIndex);
            Entries.Insert(oldIndex, box);
        }

        public TREEBox MakeNewPrimitiveBox(TREEBoxType type)
        {
            var primEnd = PrimitiveCount;
            ApplyPointerDelta(1, primEnd);
            //find end of primitives and add box there. apply delta
            var box = new TREEBox(this);
            box.InternalID = (short)primEnd;
            box.PosisionInvalid = true;
            box.Type = type;
            Entries.Insert(primEnd, box);
            return box;
        }

        public TREEBox MakeNewSpecialBox(TREEBoxType type)
        {
            //add box at end. no delta needs to be applied.
            var box = new TREEBox(this);
            box.InternalID = (short)Entries.Count;
            box.PosisionInvalid = true;
            box.Type = type;
            Entries.Add(box);
            return box;
        }

        private short GetCorrectBox(byte realID)
        {
            switch (realID)
            {
                case 255:
                    //create false box
                    var f = MakeNewSpecialBox(TREEBoxType.False);
                    return f.InternalID;
                case 254:
                    //create true box
                    var t = MakeNewSpecialBox(TREEBoxType.True);
                    return t.InternalID;
                case 253:
                    return -1;
                default:
                    return realID;
            }
        }

        public byte GetTrueID(short boxID)
        {
            return GetBox(boxID)?.TrueID ?? 253;
        }

        public TREEBox GetBox(short pointer)
        {
            if (pointer < 0 || pointer >= Entries.Count) return null;
            return Entries[pointer];
        }
    }

    public class TREEBox
    {
        //runtime
        public short InternalID = -1;
        public bool PosisionInvalid; //forces a regeneration of position using the default tree algorithm
        public TREE Parent;
        public byte TrueID
        {
            get
            {
                switch (Type)
                {
                    case TREEBoxType.Primitive:
                        return (byte)InternalID;
                    case TREEBoxType.Goto:
                        return LabelTrueID(new HashSet<short>());
                    case TREEBoxType.Label:
                        return 253; //arrows cannot point to a label
                    case TREEBoxType.True:
                        return 254;
                    case TREEBoxType.False:
                        return 255;
                }
                return 253;
            }
        }

        public byte LabelTrueID(HashSet<short> visited)
        {
            if (Type != TREEBoxType.Goto) return TrueID;
            if (visited.Contains(InternalID)) return 253; //error
            visited.Add(InternalID);
            return Parent?.GetBox(Parent.GetBox(TruePointer)?.TruePointer ?? -1)?.LabelTrueID(visited) ?? 253;
        }

        //data
        public TREEBoxType Type;
        public ushort Unknown;
        public short Width;
        public short Height;
        public short X;
        public short Y;
        public short CommentSize = 0x10;
        public short TruePointer = -1;
        public short Special; //0 or -1... unknown.
        public int FalsePointer = -1;
        public string Comment = "";
        public int TrailingZero = 0;

        public TREEBox(TREE parent)
        {
            Parent = parent;
        }

        public void Read(IoBuffer io, int version)
        {
            Type = (TREEBoxType)io.ReadUInt16();
            Unknown = io.ReadUInt16();
            Width = io.ReadInt16();
            Height = io.ReadInt16();
            X = io.ReadInt16();
            Y = io.ReadInt16();
            CommentSize = io.ReadInt16();
            TruePointer = io.ReadInt16();
            Special = io.ReadInt16();
            FalsePointer = io.ReadInt32();
            Comment = io.ReadNullTerminatedString();
            if (Comment.Length % 2 == 0) io.ReadByte(); //padding to 2 byte align
            if (version > 0) TrailingZero = io.ReadInt32();

            if (!Enum.IsDefined(typeof(TREEBoxType), Type)) throw new Exception("Unexpected TREE box type: " + Type.ToString());
            if (Special < -1 || Special > 0) throw new Exception("Unexpected TREE special: " + Special);
            if (Unknown != 0) throw new Exception("Unexpected Unknown: " + Unknown);
            if (TrailingZero != 0) Console.WriteLine("Unexpected TrailingZero: " + TrailingZero);
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
            io.WriteInt16(TruePointer);
            io.WriteInt16(Special);
            io.WriteInt32(FalsePointer);
            io.WriteCString(Comment);
            if (Comment.Length % 2 == 0) io.WriteByte(0xCD); //padding to 2 byte align
            io.WriteInt32(TrailingZero);
        }

        public override string ToString()
        {
            return Type.ToString() + " (" + TruePointer + ((FalsePointer == -1) ? "" : ("/"+FalsePointer)) + "): " + Comment;
        }
    }

    public enum TREEBoxType : ushort
    {
        Primitive = 0,
        True = 1,
        False = 2,
        Comment = 3,
        Label = 4,
        Goto = 5 //no comment size, roughly primitive sized (180, 48), pointer goes to Label
    }
}
