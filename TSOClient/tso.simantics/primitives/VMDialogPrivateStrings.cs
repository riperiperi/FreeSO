using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.simantics.engine;
using tso.files.utils;

namespace tso.simantics.primitives
{
    public class VMDialogPrivateStrings : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            /**
             * It seems as though dialogs might be sometimes blocking. This will need consideration when
             * a server is introduced
             */
            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMDialogPrivateStringsOperand : VMPrimitiveOperand
    {
        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN)){

            }
        }
        #endregion
    }
}
