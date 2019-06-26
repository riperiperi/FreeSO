using FSO.SimAntics.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.JIT.Runtime
{
    public class VMAOTRoutine : VMRoutine
    {
        private IBHAV Function;

        public VMAOTRoutine(IBHAV func) : base()
        {
            Function = func;
        }

        public override VMPrimitiveExitCode Execute(VMStackFrame frame, out VMInstruction instruction)
        {
            var result = Function.Execute(frame, ref frame.InstructionPointer);
            instruction = frame.GetCurrentInstruction();
            return result;
        }
    }

    public class VMAOTInlineRoutine : VMRoutine
    {
        private IInlineBHAV Function;

        public VMAOTInlineRoutine(IInlineBHAV func) : base()
        {
            Function = func;
        }

        public override VMPrimitiveExitCode Execute(VMStackFrame frame, out VMInstruction instruction)
        {
            var result = Function.Execute(frame, ref frame.InstructionPointer, frame.Args);
            instruction = frame.GetCurrentInstruction();
            return result ? VMPrimitiveExitCode.RETURN_TRUE : VMPrimitiveExitCode.RETURN_FALSE;
        }
    }
}
