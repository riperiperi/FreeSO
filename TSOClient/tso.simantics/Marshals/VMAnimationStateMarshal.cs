using FSO.SimAntics.NetPlay.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using FSO.Vitaboy;

namespace FSO.SimAntics.Marshals
{
    public class VMAnimationStateMarshal : VMSerializable
    {
        public string Anim;
        public float CurrentFrame;
        public short EventCode;
        public bool EventFired;
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
            EventCode = reader.ReadInt16();
            EventFired = reader.ReadBoolean();
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
            writer.Write(EventCode);
            writer.Write(EventFired);
            writer.Write(EndReached);
            writer.Write(PlayingBackwards);
            writer.Write(Speed);
            writer.Write(Weight);
            writer.Write(Loop);
        }
    }
}
