using FSO.SimAntics.Engine;
using FSO.SimAntics.NetPlay.Model;
using System.IO;

namespace FSO.SimAntics.Marshals.Threads
{
    public class VMStackFrameMarshal : VMSerializable
    {
        public ushort RoutineID;
        public ushort InstructionPointer;
        public short Caller;
        public short Callee;
        public short StackObject;
        public uint CodeOwnerGUID;
        public short[] Locals;
        public short[] Args;
        public VMSpecialResult SpecialResult;
        public bool ActionTree;

        public int Version;

        public VMStackFrameMarshal() { }
        public VMStackFrameMarshal(int version) { Version = version; }

        public virtual void Deserialize(BinaryReader reader)
        {
            RoutineID = reader.ReadUInt16();
            InstructionPointer = reader.ReadUInt16();
            Caller = reader.ReadInt16();
            Callee = reader.ReadInt16();
            StackObject = reader.ReadInt16();
            CodeOwnerGUID = reader.ReadUInt32();

            var localN = reader.ReadInt32();
            if (localN > -1)
            {
                Locals = VMSerializableUtils.ReadArray<short>(reader, localN);
            }

            var argsN = reader.ReadInt32();
            if (argsN > -1)
            {
                Args = VMSerializableUtils.ReadArray<short>(reader, argsN);
            }

            if (Version > 3) SpecialResult = (VMSpecialResult)reader.ReadByte();
            ActionTree = reader.ReadBoolean();
        }

        public virtual void SerializeInto(BinaryWriter writer)
        {
            writer.Write(RoutineID);
            writer.Write(InstructionPointer);
            writer.Write(Caller);
            writer.Write(Callee);
            writer.Write(StackObject);
            writer.Write(CodeOwnerGUID);
            writer.Write((Locals == null)?-1:Locals.Length);
            if (Locals != null) VMSerializableUtils.WriteArray(writer, Locals);
            writer.Write((Args == null) ? -1 : Args.Length);
            if (Args != null) VMSerializableUtils.WriteArray(writer, Args);
            writer.Write((byte)SpecialResult);
            writer.Write(ActionTree);
        }
    }
}
