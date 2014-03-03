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
            /** Is my thread idle or active? **/
            if (context.Thread.State == VMThreadState.Active){
                context.VM.ThreadIdle(context.Thread);
            }else{
                context.VM.ThreadActive(context.Thread);
            }
            return VMPrimitiveExitCode.GOTO_TRUE_NEXT_TICK;
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
