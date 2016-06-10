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
        public byte TerrainType;
        public byte PropertyCategory;
        public int Size = 8;

        public uint OwnerID;
        public HashSet<uint> Roommates = new HashSet<uint>();
        public HashSet<uint> BuildRoommates = new HashSet<uint>();

        public override void Deserialize(BinaryReader reader)
        {
            Name = reader.ReadString();
            LotID = reader.ReadUInt32();
            TerrainType = reader.ReadByte();
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
            writer.Write(TerrainType);
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
