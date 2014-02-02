using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.simantics.engine;

namespace tso.simantics.primitives
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
