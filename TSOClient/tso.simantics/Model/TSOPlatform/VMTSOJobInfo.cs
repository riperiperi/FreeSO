using FSO.SimAntics.NetPlay.Model;
using System.IO;

namespace FSO.SimAntics.Model.TSOPlatform
{
    public class VMTSOJobInfo : VMSerializable
    {
        public short Experience { get; set; }
        public short Level { get; set; }
        public short SickDays { get; set; }
        public short StatusFlags { get; set; }

        public void Deserialize(BinaryReader reader)
        {
            Experience = reader.ReadInt16();
            Level = reader.ReadInt16();
            SickDays = reader.ReadInt16();
            StatusFlags = reader.ReadInt16();
        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(Experience);
            writer.Write(Level);
            writer.Write(SickDays);
            writer.Write(StatusFlags);
        }
    }
}
