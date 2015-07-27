using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TSO.Simantics.net.model
{
    public abstract class VMNetCommandBodyAbstract : VMSerializable
    {
        public abstract bool Execute(VM vm);

        public abstract void Deserialize(BinaryReader reader);
        public abstract void SerializeInto(BinaryWriter writer);
    }
}
