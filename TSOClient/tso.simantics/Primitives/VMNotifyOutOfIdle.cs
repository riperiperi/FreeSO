using FSO.SimAntics.Engine;
using FSO.Files.Utils;

namespace FSO.SimAntics.Primitives
{
    public class VMNotifyOutOfIdle : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            if (context.StackObject?.Thread != null)
            {
                context.VM.Scheduler.RescheduleInterrupt(context.StackObject);
                context.StackObject.Thread.Interrupt = true;
            }
            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMNotifyOutOfIdleOperand : VMPrimitiveOperand
    {
        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes){
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN)){

            }
        }

        public void Write(byte[] bytes) { }
        #endregion
    }
}
