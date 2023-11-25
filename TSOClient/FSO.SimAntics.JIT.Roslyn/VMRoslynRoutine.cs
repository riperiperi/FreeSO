using FSO.SimAntics.Engine;
using FSO.SimAntics.JIT.Runtime;

namespace FSO.SimAntics.JIT.Roslyn
{
    /// <summary>
    /// Compared to an AOT routine, a Roslyn routine can fall back to the interpreter if the IBHAV is not available yet.
    /// </summary>
    public class VMRoslynRoutine : VMRoutine
    {
        private delegate VMPrimitiveExitCode ExecuteDelegate(VMStackFrame frame, out VMInstruction instruction);
        private IBHAV Function;
        private IInlineBHAV IFunction;

        private ExecuteDelegate ExecuteFunction;

        public VMRoslynRoutine() : base()
        {
            ExecuteFunction = base.Execute;
        }

        public void SetJITRoutine(IBHAV bhav)
        {
            Function = bhav;
            ExecuteFunction = ExecuteJIT;
        }

        public void SetJITRoutine(IInlineBHAV bhav)
        {
            IFunction = bhav;
            ExecuteFunction = ExecuteJITInline;
        }

        public override VMPrimitiveExitCode Execute(VMStackFrame frame, out VMInstruction instruction)
        {
            return ExecuteFunction(frame, out instruction);
        }

        private VMPrimitiveExitCode ExecuteJIT(VMStackFrame frame, out VMInstruction instruction)
        {
            var result = Function.Execute(frame, ref frame.InstructionPointer);
            instruction = frame.GetCurrentInstruction();
            return result;
        }

        private VMPrimitiveExitCode ExecuteJITInline(VMStackFrame frame, out VMInstruction instruction)
        {
            var result = IFunction.Execute(frame, ref frame.InstructionPointer, frame.Args);
            instruction = frame.GetCurrentInstruction();
            return result ? VMPrimitiveExitCode.RETURN_TRUE : VMPrimitiveExitCode.RETURN_FALSE;
        }

    }
}
