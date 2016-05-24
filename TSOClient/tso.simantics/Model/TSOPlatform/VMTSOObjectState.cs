using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FSO.SimAntics.Model.TSOPlatform
{
    public class VMTSOObjectState : VMTSOEntityState
    {
        //TODO: repair
        public uint OwnerID;

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            OwnerID = reader.ReadUInt32();
        }

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(OwnerID);
        }

        public override void Tick(VM vm, object owner)
        {
            base.Tick(vm, owner);
        }
    }
}
