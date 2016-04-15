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
        public string Name;
        public uint LotID;
        public string TerrainType;
        public string PropertyCategory;
        public int Size;

        public uint OwnerID;
        public uint[] Roommates;
        public uint[] BuildRoommates;

        public override void Deserialize(BinaryReader reader)
        {
            throw new NotImplementedException();
        }

        public override void SerializeInto(BinaryWriter writer)
        {
            throw new NotImplementedException();
        }

        public override void Tick(VM vm, object owner)
        {
            
        }
    }
}
