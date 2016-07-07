using FSO.SimAntics.NetPlay.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FSO.SimAntics.Model.TSOPlatform
{
    public class VMTSOLotState : VMPlatformState
    {
        public string Name = "Lot";
        public uint LotID;
        public VMTSOSurroundingTerrain Terrain = new VMTSOSurroundingTerrain();
        public byte PropertyCategory;
        public int Size = 8;

        public uint OwnerID;
        public HashSet<uint> Roommates = new HashSet<uint>();
        public HashSet<uint> BuildRoommates = new HashSet<uint>();

        public VMTSOLotState() { }
        public VMTSOLotState(int version) : base(version) { }

        public override void Deserialize(BinaryReader reader)
        {
            Name = reader.ReadString();
            LotID = reader.ReadUInt32();
            if (Version > 6) {
                Terrain = new VMTSOSurroundingTerrain();
                Terrain.Deserialize(reader);
            } else {
                reader.ReadByte(); //old Terrain Type
            }
            PropertyCategory = reader.ReadByte();
            Size = reader.ReadInt32();

            OwnerID = reader.ReadUInt32();
            Roommates = new HashSet<uint>();
            var roomCount = reader.ReadInt16();
            for (int i = 0; i < roomCount; i++) Roommates.Add(reader.ReadUInt32());
            BuildRoommates = new HashSet<uint>();
            var broomCount = reader.ReadInt16();
            for (int i = 0; i < broomCount; i++) BuildRoommates.Add(reader.ReadUInt32());
        }

        public override void SerializeInto(BinaryWriter writer)
        {
            writer.Write(Name);
            writer.Write(LotID);
            Terrain.SerializeInto(writer);
            writer.Write(PropertyCategory);
            writer.Write(Size);

            writer.Write(OwnerID);
            writer.Write((short)Roommates.Count);
            foreach (var roomie in Roommates) writer.Write(roomie);
            writer.Write((short)BuildRoommates.Count);
            foreach (var roomie in BuildRoommates) writer.Write(roomie);
        }

        public override void Tick(VM vm, object owner)
        {
            
        }
    }
}
