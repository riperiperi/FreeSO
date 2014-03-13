using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Simantics.engine;
using TSO.Files.utils;

namespace TSO.Simantics.primitives
{
    // This primitive allows the sim to look at objects or other people eg. when talking to them. Not important right now
    // but crucial for tv/eating conversations to make sense

    public class VMLookTowards : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMLookTowardsOperand : VMPrimitiveOperand
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
