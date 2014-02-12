using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.simantics.engine;
using tso.files.utils;

namespace tso.simantics.primitives
{
    public class VMStopAllSounds : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            var operand = context.GetCurrentOperand<VMStopAllSoundsOperand>();
            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMStopAllSoundsOperand : VMPrimitiveOperand
    {

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
            }
        }
        #endregion
    }
}
