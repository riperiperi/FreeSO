using FSO.Files.Utils;
using FSO.SimAntics.Engine;
using System.IO;

namespace FSO.SimAntics.Primitives
{
    public class VMGosubFoundAction : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args) //TODO: Behaviour for being notified out of idle and interaction canceling
        {
            if (context.Thread.ActiveAction != null) context.StackObject = context.Thread.ActiveAction.Callee;
            //if we're main, attempt to run a queued interaction. We just idle if this fails.
            if (!context.ActionTree && context.Thread.AttemptPush())
            {
                return VMPrimitiveExitCode.CONTINUE; //control handover
                //TODO: does this forcefully end the rest of the idle? (force a true return, must loop back to run again)
            }

            return VMPrimitiveExitCode.GOTO_FALSE;
        }
    }

    public class VMGosubFoundActionOperand : VMPrimitiveOperand
    {
        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
            }
        }

        public void Write(byte[] bytes)
        {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
            }
        }
        #endregion
    }
}
