using FSO.Files.Utils;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FSO.Files.Formats.IFF.Chunks
{
    /// <summary>
    /// This chunk defines all neighbours in a neighbourhood. 
    /// A neighbour is a specific version of a sim object with associated relationships and person data. (skills, person type)
    /// 
    /// These can be read within SimAntics without the avatar actually present. This is used to find and spawn suitable sims on 
    /// ped portals as visitors, and also drive phone calls to other sims in the neighbourhood.
    /// When neighbours are spawned, they assume the attributes saved here. A TS1 global call allows the game to save these attributes.
    /// </summary>
    public class NBRS : IffChunk
    {
        public List<Neighbour> Entries = new List<Neighbour>();
        public Dictionary<short, Neighbour> NeighbourByID = new Dictionary<short, Neighbour>();
        public Dictionary<uint, short> DefaultNeighbourByGUID = new Dictionary<uint, short>();

        public uint Version;

        /// <summary>
        /// Reads a NBRS chunk from a stream.
        /// </summary>
        /// <param name="iff">An Iff instance.</param>
        /// <param name="stream">A Stream object holding a NBRS chunk.</param>
        public override void Read(IffFile iff, Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                io.ReadUInt32(); //pad
                Version = io.ReadUInt32(); //0x49 for latest game
                string magic = io.ReadCString(4); //SRBN
                var count = io.ReadUInt32();

                for (int i=0; i<count; i++)
                {
                    if (!io.HasMore) break;
                    var neigh = new Neighbour(io);
                    Entries.Add(neigh);
                    if (neigh.Unknown1 > 0)
                    {
                        NeighbourByID.Add(neigh.NeighbourID, neigh);
                        DefaultNeighbourByGUID[neigh.GUID] = neigh.NeighbourID;
                    }
                }
            }
            Entries = Entries.OrderBy(x => x.NeighbourID).ToList();
            foreach (var entry in Entries)
                entry.RuntimeIndex = Entries.IndexOf(entry);
        }

        /// <summary>
        /// Writes a NBRS chunk to a stream.
        /// </summary>
        /// <param name="iff">An Iff instance.</param>
        /// <param name="stream">A destination stream.</param>
        public override bool Write(IffFile iff, Stream stream)
        {
            using (var io = IoWriter.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                io.WriteUInt32(0);
                io.WriteUInt32(0x49);
                io.WriteCString("SRBN", 4);
                io.WriteInt32(Entries.Count);
                foreach (var n in NeighbourByID.Values)
                {
                    n.Save(io);
                }
            }
            return true;
        }

        public void AddNeighbor(Neighbour nb) {
            Entries.Add(nb);
            Entries = Entries.OrderBy(x => x.NeighbourID).ToList();
            foreach (var entry in Entries)
                entry.RuntimeIndex = Entries.IndexOf(entry);

            NeighbourByID.Add(nb.NeighbourID, nb);
            DefaultNeighbourByGUID[nb.GUID] = nb.NeighbourID;
        }

        public short GetFreeID()
        {
            //find the lowest id that is free
            short newID = 1;
            for (int i = 0; i < Entries.Count; i++)
            {
                if (Entries[i].NeighbourID == newID) newID++;
                else if (Entries[i].NeighbourID < newID) continue;
                else break;
            }
            return newID;
        }
    }

    public class Neighbour
    {
        public int Unknown1 = 1; //1
        public int Version = 0xA; //0x4, 0xA
        //if 0xA, unknown3 follows
        //0x4 indicates person data size of 0xa0.. (160 bytes, or 80 entries)
        public int Unknown3 = 9; //9
        public string Name;
        public int MysteryZero = 0;
        public int PersonMode; //0/5/9
        public short[] PersonData; //can be null

        public short NeighbourID;
        public uint GUID;
        public int UnknownNegOne = -1; //negative 1 usually

        public Dictionary<int, List<short>> Relationships;

        public int RuntimeIndex; //used for fast continuation of Set to Next

        public Neighbour() { }

        public Neighbour(IoBuffer io)
        {
            Unknown1 = io.ReadInt32();
            if (Unknown1 != 1) { return; }
            Version = io.ReadInt32();
            if (Version == 0xA)
            {
                //TODO: what version does this truly start?
                Unknown3 = io.ReadInt32();
                if (Unknown3 != 9) { }
            }
            Name = io.ReadNullTerminatedString();
            if (Name.Length % 2 == 0) io.ReadByte();
            MysteryZero = io.ReadInt32();
            if (MysteryZero != 0) { }
            PersonMode = io.ReadInt32();
            if (PersonMode > 0)
            {
                var size = (Version == 0x4) ? 0xa0 : 0x200;
                PersonData = new short[88];
                int pdi = 0;
                for (int i=0; i<size; i+=2)
                {
                    if (pdi >= 88)
                    {
                        io.ReadBytes(size - i);
                        break;
                    }
                    PersonData[pdi++] = io.ReadInt16();
                }
            }

            NeighbourID = io.ReadInt16();
            GUID = io.ReadUInt32();
            UnknownNegOne = io.ReadInt32();
            if (UnknownNegOne != -1) { }

            var entries = io.ReadInt32();
            Relationships = new Dictionary<int, List<short>>();
            for (int i=0; i<entries; i++)
            {
                var keyCount = io.ReadInt32();
                if (keyCount != 1) { }
                var key = io.ReadInt32();
                var values = new List<short>();
                var valueCount = io.ReadInt32();
                for (int j=0; j<valueCount; j++)
                {
                    values.Add((short)io.ReadInt32());
                }
                Relationships.Add(key, values);
            }
        }

        public override string ToString()
        {
            return Name;
        }

        public void Save(IoWriter io)
        {
            io.WriteInt32(Unknown1);
            io.WriteInt32(Version);
            if (Version == 0xA) io.WriteInt32(Unknown3);
            io.WriteNullTerminatedString(Name);
            if (Name.Length % 2 == 0) io.WriteByte(0);
            io.WriteInt32(MysteryZero);
            io.WriteInt32(PersonMode);
            if (PersonMode > 0)
            {
                var size = (Version == 0x4) ? 0xa0 : 0x200;
                int pdi = 0;
                for (int i = 0; i < size; i += 2)
                {
                    if (pdi >= 88)
                    {
                        io.WriteInt16(0);
                    }
                    else
                    {
                        io.WriteInt16(PersonData[pdi++]);
                    }
                }
            }

            io.WriteInt16(NeighbourID);
            io.WriteUInt32(GUID);
            io.WriteInt32(UnknownNegOne);

            io.WriteInt32(Relationships.Count);
            foreach (var rel in Relationships)
            {
                io.WriteInt32(1); //keycount (1)
                io.WriteInt32(rel.Key);
                io.WriteInt32(rel.Value.Count);
                foreach (var val in rel.Value)
                {
                    io.WriteInt32(val);
                }
            }
        }
    }
}
