using FSO.SimAntics.Engine;
using FSO.Files.Utils;
using FSO.SimAntics.Engine.Scopes;
using System.IO;

namespace FSO.SimAntics.Primitives
{
    // Generally something bad has happened when this is called.

    public class VMBreakPoint : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            //todo: check condition
            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMBreakPointOperand : VMPrimitiveOperand
    {
        public short Data { get; set; }
        public VMVariableScope Scope { get; set; }

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                Data = io.ReadInt16();
                Scope = (VMVariableScope)io.ReadUInt16();
            }
        }

        public void Write(byte[] bytes)
        {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(Data);
                io.Write((ushort)Scope);
            }
        }
        #endregion
    }
}
