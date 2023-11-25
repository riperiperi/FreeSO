using FSO.SimAntics.NetPlay.Model;
using System.IO;

namespace FSO.SimAntics.Model
{
    public abstract class VMPlatformState : VMSerializable
    {
        public int Version;
        public VMPlatformState() { }
        public VMPlatformState(int version) { Version = version; }
        public abstract void Deserialize(BinaryReader reader);
        public abstract void SerializeInto(BinaryWriter writer);
        public abstract void Tick(VM vm, object owner);
    }
}
