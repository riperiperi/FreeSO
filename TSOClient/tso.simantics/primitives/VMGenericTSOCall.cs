using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Simantics.engine;
using TSO.Files.utils;

namespace TSO.Simantics.primitives
{

    public class VMGenericTSOCall : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            var operand = context.GetCurrentOperand<VMGenericTSOCallOperand>();
            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMGenericTSOCallOperand : VMPrimitiveOperand
    {
        public byte Call;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                Call = io.ReadByte();
            }
        }
        #endregion
    }
}
