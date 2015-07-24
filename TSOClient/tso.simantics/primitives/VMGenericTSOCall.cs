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

            switch (operand.Call)
            {
                case VMGenericTSOCallMode.GetIsPendingDeletion:
                    return VMPrimitiveExitCode.GOTO_FALSE;
                case VMGenericTSOCallMode.IsTemp0AvatarIgnoringTemp1Avatar:
                    context.Thread.TempRegisters[0] = 0;
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTSOCallMode.IsGlobalBroken:
                    return VMPrimitiveExitCode.GOTO_FALSE;
                case VMGenericTSOCallMode.GetLotOwner:
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTSOCallMode.SetActionIconToStackObject:
                    context.Thread.Queue[0].IconOwner = context.StackObject;
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTSOCallMode.HasTemporaryID:
                    return VMPrimitiveExitCode.GOTO_FALSE; //used by real game to check if object is persistently tracked probably.
                case VMGenericTSOCallMode.SwapMyAndStackObjectsSlots:
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
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTSOCallMode.TestStackObject:
                    return (context.StackObject != null) ? VMPrimitiveExitCode.GOTO_TRUE : VMPrimitiveExitCode.GOTO_FALSE;
                default:
                    return VMPrimitiveExitCode.GOTO_TRUE;
            }
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
