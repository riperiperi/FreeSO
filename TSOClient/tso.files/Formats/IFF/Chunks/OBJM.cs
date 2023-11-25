using FSO.Files.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FSO.Files.Formats.IFF.Chunks
{
    public class OBJM : IffChunk
    {
        //work in progress

        //data body starts with 0x01, but what is after that is unknown.

        //empty body from house 0:
        // 01 00 00 00 | 00 00 00
        // 

        public ushort[] IDToOBJT;

        public Dictionary<int, MappedObject> ObjectData;

        public override void Read(IffFile iff, Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                io.ReadUInt32(); //pad
                var version = io.ReadUInt32();

                //house 00: 33 00 00 00
                //house 03: 3E 00 00 00
                //house 79: 45 00 00 00
                //completec:49 00 00 00
                //corresponds to house version?

                var MjbO = io.ReadUInt32();

                var compressionCode = io.ReadByte();
                if (compressionCode != 1) throw new Exception("hey what!!");

                var iop = new IffFieldEncode(io);

                /*
                var test1 = iop.ReadInt16();
                var testas = new ushort[test1*2];
                for (int i=0; i<test1*2; i++)
                {
                    testas[i] = iop.ReadUInt16();
                }*/

                var table = new List<ushort>();
                while (io.HasMore)
                {
                    var value = iop.ReadUInt16();
                    if (value == 0) break;
                    table.Add(value);
                }
                IDToOBJT = table.ToArray();

                var list = new List<short>();
                while (io.HasMore)
                {
                    list.Add(iop.ReadInt16());
                }

                var offsets = SearchForObjectData(list);
                for (int i=1; i<offsets.Count; i++)
                {
                    Console.WriteLine(offsets[i] - offsets[i-1]);
                }

                ObjectData = new Dictionary<int, MappedObject>();
                int lastOff = 0;
                foreach (var off in offsets)
                {
                    // 58 behind the object data...
                    // [-12, 0, -12, 0, -4,  0, -4, 0, -8, 0, -8, 0, 0, 210, 0, 0, 0, 0, 146, 0, -1, -1, 0, 0, 164, 13, 0, 1, 0, 0, 0, 1, 1, 0, 99, 0, 0, 0, 9, 9, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0]
                    // [-12, 0, -12, 0, -4,  0, -4, 0, -8, 0, -8, 0, 0, 210, 0, 0, 0, 0, 146, 0, -1, -1, 0, 0, 197, 13, 0, 1, 0, 0, 0, 1, 1, 0, 79, 0, 4, 2, 9, 9, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0]
                    // [-12, 0, -12, 0, -4,  0, -4, 0, -8, 0, -8, 0, 0, 210, 0, 0, 0, 0, 146, 0, -1, -1, 0, 0, 197, 13, 0, 1, 0, 0, 0, 1, 1, 0, 71, 0, 3, 2, 9, 9, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0]
                    // [  1, 0,   1, 0,  0,  0,256, 0, 48, 0,  0,-1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 43, 0, 19493, 0, 0, -122, -8, 83, 0, 0, 0, 0, -23174, 0, 0, 0, 0, 0, 0, 196, 0, 2, 0, 0, 0, 0, 0,
                    var endOff = off + 72;
                    var size = endOff - lastOff;
                    var data = list.Skip(lastOff).Take(size).ToArray();

                    var bas = size - 72;
                    var objID = data[bas+11]; //object id
                    var dir = data[bas + 1];
                    var parent = data[bas + 26];
                    var containerid = data[bas + 2];
                    var containerslot = data[bas + 2];

                    ObjectData[objID] = new MappedObject() { ObjectID = objID, Direction = dir, Data = data, ParentID = parent, ContainerID = containerid, ContainerSlot = containerslot };

                    lastOff = endOff;
                }
            }
        }

        public class MappedObject {
            public string Name;
            public uint GUID;
            public int ObjectID;
            public int Direction;
            public int ParentID;

            public int ContainerID;
            public int ContainerSlot;

            public short[] Data;

            public int ArryX;
            public int ArryY;
            public int ArryLevel;

            public override string ToString()
            {
                return Name ?? "(unreferenced)";
            }
        }

        public List<int> SearchForObjectData(List<short> data)
        {
            //we don't know exactly where the object data is in the format...
            //but we know objects should have a birth date, basically always 1997 or (1997-36) for npcs.
            //this should let us extract some important attributes like the structure of the data and object directions.

            var offsets = new List<int>();
            for (int i=0; i<data.Count-3; i++)
            {
                if ((data[i] == 1997 || data[i] == 1998 || data[i] == (1997-36)) && (data[i + 1] > 0 && data[i+1] < 13) && (data[i + 2] > 0 && data[i + 2] < 32)) {
                    offsets.Add(i - 45);
                }
            }

            return offsets;
        }
    }
}
