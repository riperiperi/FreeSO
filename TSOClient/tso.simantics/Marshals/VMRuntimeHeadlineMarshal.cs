using FSO.SimAntics.NetPlay.Model;
using FSO.SimAntics.Primitives;
using System.IO;

namespace FSO.SimAntics.Marshals
{
    public class VMRuntimeHeadlineMarshal : VMSerializable
    {
        public VMSetBalloonHeadlineOperand Operand;
        public short Target;
        public short IconTarget;
        public sbyte Index;
        public int Duration;
        public int Anim;

        public void SerializeInto(BinaryWriter writer)
        {
            var op = new byte[8];
            Operand.Write(op);
            writer.Write(op);
            writer.Write(Target);
            writer.Write(IconTarget);
            writer.Write(Index);
            writer.Write(Duration);
            writer.Write(Anim);
        }

        public void Deserialize(BinaryReader reader)
        {
            Operand = new VMSetBalloonHeadlineOperand();
            Operand.Read(reader.ReadBytes(8));
            Target = reader.ReadInt16();
            IconTarget = reader.ReadInt16();
            Index = reader.ReadSByte();
            Duration = reader.ReadInt32();
            Anim = reader.ReadInt32();
        }
    }
}
