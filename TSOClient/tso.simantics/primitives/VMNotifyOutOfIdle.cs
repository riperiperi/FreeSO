using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Simantics.engine;
using TSO.Files.utils;

namespace TSO.Simantics.primitives
{
    public class VMNotifyOutOfIdle : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            context.StackObject.Interrupt = true;
            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMNotifyOutOfIdleOperand : VMPrimitiveOperand
    {
        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes){
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN)){

            }
        }
        #endregion
    }
}
