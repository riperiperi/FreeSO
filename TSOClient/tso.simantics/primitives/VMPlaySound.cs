using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Simantics.engine;

namespace TSO.Simantics.primitives
{
    public class VMPlaySound : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            var operand = context.GetCurrentOperand<VMPlaySoundOperand>();
            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMPlaySoundOperand : VMPrimitiveOperand {

        #region VMPrimitiveOperand Members

        public void Read(byte[] bytes){
        }

        #endregion
    }
}
