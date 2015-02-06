using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Simantics.engine;
using TSO.Files.utils;
using TSO.Simantics.model;

namespace TSO.Simantics.primitives
{

    public class VMGenericTSOCall : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            var operand = context.GetCurrentOperand<VMGenericTSOCallOperand>();

            if (
                operand.Call == VMGenericTSOCallMode.GetIsPendingDeletion || 
                operand.Call == VMGenericTSOCallMode.IsTemp0AvatarIgnoringTemp1Avatar ||
                operand.Call == VMGenericTSOCallMode.IsGlobalBroken
                ) return VMPrimitiveExitCode.GOTO_FALSE;
            else if (operand.Call == VMGenericTSOCallMode.SwapMyAndStackObjectsSlots)
            {
                int total = Math.Min(context.StackObject.TotalSlots(), context.Caller.TotalSlots());
                for (int i = 0; i < total; i++)
                {
                    VMEntity temp1 = context.Caller.GetSlot(i);
                    VMEntity temp2 = context.StackObject.GetSlot(i);
                    context.Caller.ClearSlot(i);
                    context.StackObject.ClearSlot(i);
                    context.Caller.PlaceInSlot(temp2, i);
                    context.StackObject.PlaceInSlot(temp1, i);
                }
            }
            else if (operand.Call == VMGenericTSOCallMode.TestStackObject) return (context.StackObject != null) ? VMPrimitiveExitCode.GOTO_TRUE : VMPrimitiveExitCode.GOTO_FALSE;
            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMGenericTSOCallOperand : VMPrimitiveOperand
    {
        public VMGenericTSOCallMode Call;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                Call = (VMGenericTSOCallMode)io.ReadByte();
            }
        }
        #endregion
    }
}
