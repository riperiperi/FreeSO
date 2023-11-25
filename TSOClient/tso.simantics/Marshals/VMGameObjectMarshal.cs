using System.IO;
using FSO.LotView.Model;

namespace FSO.SimAntics.Marshals
{
    public class VMGameObjectMarshal : VMEntityMarshal
    {
        public Direction Direction;
        public VMGameObjectDisableFlags Disabled;

        public VMGameObjectMarshal() { }
        public VMGameObjectMarshal(int version, bool ts1) : base(version, ts1) { }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Direction = (Direction)reader.ReadByte();
            if (Version > 9) Disabled = (VMGameObjectDisableFlags)reader.ReadByte();
        }
        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write((byte)Direction);
            writer.Write((byte)Disabled);
        }
    }
}
