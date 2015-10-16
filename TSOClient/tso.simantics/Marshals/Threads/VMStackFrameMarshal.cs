using FSO.SimAntics.NetPlay.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public ushort[] Locals;
        public short[] Args;

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
                Locals = new ushort[localN];
                for (int i = 0; i < localN; i++) Locals[i] = reader.ReadUInt16();
            }

            var argsN = reader.ReadInt32();
            if (argsN > -1)
            {
                Args = new short[argsN];
                for (int i = 0; i < argsN; i++) Args[i] = reader.ReadInt16();
            }
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
            if (Locals != null) foreach (var item in Locals) writer.Write(item);
            writer.Write((Args == null) ? -1 : Args.Length);
            if (Args != null) foreach (var item in Args) writer.Write(item);
        }
    }
}
