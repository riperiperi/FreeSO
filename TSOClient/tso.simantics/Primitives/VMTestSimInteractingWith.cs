using System.Linq;
using FSO.SimAntics.Engine;
using FSO.Files.Utils;

namespace FSO.SimAntics.Primitives
{
    public class VMTestSimInteractingWith : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            //if caller's active interaction is with stack object, return true.
            var callerActive = context.Caller.Thread.Stack.LastOrDefault();
            return (callerActive != null && callerActive.ActionTree && context.Caller.Thread.ActiveAction.Callee == context.StackObject) 
                ? VMPrimitiveExitCode.GOTO_TRUE:VMPrimitiveExitCode.GOTO_FALSE;
        }
    }

    public class VMTestSimInteractingWithOperand : VMPrimitiveOperand
    {
        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                //nothing! zip! zilch! nada!
            }
        }

        public void Write(byte[] bytes) { }
        #endregion
    }
}
