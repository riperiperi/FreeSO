using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Simantics.engine;
using TSO.Files.utils;
using TSO.Files.formats.iff.chunks;
using TSO.Content;

namespace TSO.Simantics.engine.primitives
{
    public class VMRemoveObjectInstance : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            var operand = context.GetCurrentOperand<VMRemoveObjectInstanceOperand>();
            VMEntity obj;
            if (operand.Target == 0) obj = context.Caller;
            else obj = context.StackObject;

            obj.Delete(operand.CleanupAll, context.VM.Context);

            if (obj == context.StackObject) context.StackObject = null;

            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMRemoveObjectInstanceOperand : VMPrimitiveOperand
    {
        public short Target;
        public byte Flags;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                Target = io.ReadInt16();
                Flags = io.ReadByte();
            }
        }
        #endregion

        public bool CleanupAll
        {
            get
            {
                return ((Flags & 2) == 2);
            }
        }
    }
}
