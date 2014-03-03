using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Simantics.engine;
using TSO.Files.utils;
using TSO.Simantics.engine.utils;
using TSO.Simantics.engine.scopes;

namespace TSO.Simantics.primitives
{
    public class VMSleep : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context){
            var operand = context.GetCurrentOperand<VMSleepOperand>();

            Trace("sleep: --(" + VMMemory.DescribeVariable(context, VMVariableScope.Local, operand.StackVarToDec) + ") != 0");

            var ticks = VMMemory.GetVariable(context, TSO.Simantics.engine.scopes.VMVariableScope.Local, operand.StackVarToDec);
            ticks--;

            if (ticks < 0)
            {
                return VMPrimitiveExitCode.GOTO_TRUE;
            }
            else
            {
                VMMemory.SetVariable(context, TSO.Simantics.engine.scopes.VMVariableScope.Local, operand.StackVarToDec, ticks);
                return VMPrimitiveExitCode.CONTINUE_NEXT_TICK;
            }
        }
    }

    public class VMSleepOperand : VMPrimitiveOperand
    {
        public ushort StackVarToDec;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes){
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN)){
                StackVarToDec = io.ReadUInt16();
            }
        }
        #endregion
    }
}
