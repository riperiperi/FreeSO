using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Simantics.engine;
using TSO.Files.utils;

namespace TSO.Simantics.primitives
{
    public class VMGrab : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            var operand = context.GetCurrentOperand<VMGrabOperand>();

            if (context.Caller.GetSlot(0) == null)
                context.Caller.PlaceInSlot(context.StackObject, 0);
            else
                return VMPrimitiveExitCode.GOTO_FALSE;

            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMGrabOperand : VMPrimitiveOperand //empty :(
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
