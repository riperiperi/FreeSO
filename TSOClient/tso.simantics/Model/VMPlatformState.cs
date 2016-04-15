using FSO.SimAntics.NetPlay.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FSO.SimAntics.Model
{
    public abstract class VMPlatformState : VMSerializable
    {
        public abstract void Deserialize(BinaryReader reader);
        public abstract void SerializeInto(BinaryWriter writer);
        public abstract void Tick(VM vm, object owner);
    }
}
