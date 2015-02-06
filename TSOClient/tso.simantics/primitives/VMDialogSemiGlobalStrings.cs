using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Simantics.engine;
using TSO.Files.utils;
using TSO.Files.formats.iff.chunks;

namespace TSO.Simantics.primitives
{
    public class VMDialogSemiGlobalStrings : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            var operand = context.GetCurrentOperand<VMDialogStringsOperand>();
            VMDialogHandler.ShowDialog(context, operand, context.Callee.SemiGlobal.Resource.Get<STR>(301));
            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }
}
