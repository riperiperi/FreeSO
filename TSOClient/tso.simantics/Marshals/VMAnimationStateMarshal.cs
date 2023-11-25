using FSO.SimAntics.NetPlay.Model;
using System.IO;

namespace FSO.SimAntics.Marshals
{
    public class VMAnimationStateMarshal : VMSerializable
    {
        public string Anim;
        public float CurrentFrame;
        public short[] EventQueue;
        public byte EventsRun;
        public bool EndReached;
        public bool PlayingBackwards;
        public float Speed;
        public float Weight;
        public bool Loop;
        //time property list is restored from anim

        public VMAnimationStateMarshal() { }
        public VMAnimationStateMarshal(BinaryReader reader)
        {
            Deserialize(reader);
        }
        public void Deserialize(BinaryReader reader)
        {
            Anim = reader.ReadString();
            CurrentFrame = reader.ReadSingle();
            var size = reader.ReadByte();
            EventQueue = new short[size];
            for (int i = 0; i < EventQueue.Length; i++) EventQueue[i] = reader.ReadInt16();
            EventsRun = reader.ReadByte();
            EndReached = reader.ReadBoolean();
            PlayingBackwards = reader.ReadBoolean();
            Speed = reader.ReadSingle();
            Weight = reader.ReadSingle();
            Loop = reader.ReadBoolean();
        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(Anim);
            writer.Write(CurrentFrame);
            writer.Write((byte)EventQueue.Length);
            foreach (var evt in EventQueue) writer.Write(evt);
            writer.Write(EventsRun);
            writer.Write(EndReached);
            writer.Write(PlayingBackwards);
            writer.Write(Speed);
            writer.Write(Weight);
            writer.Write(Loop);
        }
    }
}
