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
    public class VMIdleForInput : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context) //TODO: Behaviour for being notified out of idle and interaction canceling
        {
            var operand = context.GetCurrentOperand<VMIdleForInputOperand>();
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

    public class VMIdleForInputOperand : VMPrimitiveOperand
    {
        public ushort StackVarToDec;
        public ushort AllowPush;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                StackVarToDec = io.ReadUInt16();
                AllowPush = io.ReadUInt16();
            }
        }
        #endregion
    }
}
