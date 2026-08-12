using FSO.SimAntics.Engine;
using FSO.Files.Utils;

namespace FSO.SimAntics.Primitives
{
    public class VMSleep : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMSleepOperand)args;
            var thread = context.Thread;
            var idleStart = thread.ScheduleIdleStart;
            var vm = context.VM;
            var scheduler = vm.Scheduler;

            ref short arg = ref context.Args.GetRef(operand.StackVarToDec);

            arg -= (short)((idleStart != 0 && idleStart < scheduler.CurrentTickID) ? (scheduler.CurrentTickID - idleStart) : 1);

            if (thread.Interrupt)
            {
                thread.ScheduleIdleStart = 0;
                thread.Interrupt = false;
                return VMPrimitiveExitCode.GOTO_TRUE;
            }

            if (arg <= -1) { 
                thread.ScheduleIdleStart = 0;
                vm.Context.NextRandom(1); //rng cycle - for desync detect
                return (context.Caller.Dead)?VMPrimitiveExitCode.GOTO_TRUE_NEXT_TICK:VMPrimitiveExitCode.GOTO_TRUE;
            }
            else
            {
                thread.ScheduleIdleStart = scheduler.CurrentTickID;
                scheduler.ScheduleTickIn(context.Caller, (uint)arg+1);
                return VMPrimitiveExitCode.CONTINUE_FUTURE_TICK;
            }
        }
    }

    public class VMSleepOperand : VMPrimitiveOperand
    {
        public short StackVarToDec { get; set; }

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes){
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN)){
                StackVarToDec = io.ReadInt16();
            }
        }

        public void Write(byte[] bytes) {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(StackVarToDec);
            }
        }
        #endregion
    }
}
