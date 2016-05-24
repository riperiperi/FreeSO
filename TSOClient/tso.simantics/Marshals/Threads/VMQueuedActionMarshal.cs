using FSO.SimAntics.NetPlay.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using FSO.SimAntics.Engine;
using FSO.Files.Formats.IFF.Chunks;

namespace FSO.SimAntics.Marshals.Threads
{
    public class VMQueuedActionMarshal : VMSerializable
    {
        public ushort RoutineID;
        public ushort CheckRoutineID;
        public short Callee;
        public short StackObject; 
        public short IconOwner; 

        public uint CodeOwnerGUID;
        public string Name;
        public short[] Args; //NULLable

        public int InteractionNumber = -1; 
        public bool Cancelled;

        public short Priority;
        public VMQueueMode Mode;
        public TTABFlags Flags;
        public TSOFlags Flags2;

        public ushort UID; 

        public VMActionCallbackMarshal Callback; //NULLable

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(RoutineID);
            writer.Write(CheckRoutineID);
            writer.Write(Callee);
            writer.Write(StackObject);
            writer.Write(IconOwner);

            writer.Write(CodeOwnerGUID);
            writer.Write(Name != null);
            if (Name != null) writer.Write(Name);
            writer.Write((Args==null)?-1:Args.Length);
            if (Args != null) foreach (var item in Args) { writer.Write(item); }

            writer.Write(InteractionNumber);
            writer.Write(Cancelled);

            writer.Write((short)Priority);
            writer.Write((byte)Mode);
            writer.Write((uint)Flags);
            writer.Write((uint)Flags2);

            writer.Write(UID);

            writer.Write(Callback != null);
            if (Callback != null) Callback.SerializeInto(writer);
        }

        public void Deserialize(BinaryReader reader)
        {
            RoutineID = reader.ReadUInt16();
            Callee = reader.ReadInt16();
            StackObject = reader.ReadInt16();
            IconOwner = reader.ReadInt16();

            CodeOwnerGUID = reader.ReadUInt32();
            if (reader.ReadBoolean()) Name = reader.ReadString();

            var argsLen = reader.ReadInt32();
            if (argsLen > -1)
            {
                Args = new short[argsLen];
                for (int i = 0; i < argsLen; i++) Args[i] = reader.ReadInt16();
            }

            InteractionNumber = reader.ReadInt32();
            Cancelled = reader.ReadBoolean();

            Priority = reader.ReadInt16();
            Mode = (VMQueueMode)reader.ReadByte();
            Flags = (TTABFlags)reader.ReadUInt32();
            Flags2 = (TSOFlags)reader.ReadUInt32();

            UID = reader.ReadUInt16();
            
            if (reader.ReadBoolean())
            {
                Callback = new VMActionCallbackMarshal();
                Callback.Deserialize(reader);
            }
        }
    }

    public class VMActionCallbackMarshal : VMSerializable
    {
        public int Type;

        //type 1 variables
        public short Target;
        public short Interaction;
        public bool SetParam;
        public short StackObject;
        public short Caller;
        public bool IsTree;

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(Type);
            writer.Write(Target);
            writer.Write(Interaction);
            writer.Write(SetParam);
            writer.Write(StackObject);
            writer.Write(Caller);
            writer.Write(IsTree);
        }

        public void Deserialize(BinaryReader reader)
        {
            Type = reader.ReadInt32();
            Target = reader.ReadInt16();
            Interaction = reader.ReadByte();
            SetParam = reader.ReadBoolean();
            StackObject = reader.ReadInt16();
            Caller = reader.ReadInt16();
            IsTree = reader.ReadBoolean();
        }
    }
}
