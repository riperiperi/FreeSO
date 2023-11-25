namespace FSO.SimAntics.Engine
{
    public abstract class VMPrimitiveHandler
    {
        protected void Trace(string message){
            System.Diagnostics.Debug.WriteLine(message);
        }

        public abstract VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand operand);
    }
}
