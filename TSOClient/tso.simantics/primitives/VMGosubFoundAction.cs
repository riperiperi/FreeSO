using FSO.Files.Utils;
using FSO.SimAntics.Engine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.Primitives
{
    public class VMGosubFoundAction : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args) //TODO: Behaviour for being notified out of idle and interaction canceling
        {
            var operand = (VMGosubFoundActionOperand)args;

            if (context.Thread.Queue.Count > 0) context.StackObject = context.Thread.Queue[0].Callee;
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
