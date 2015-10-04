/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.SimAntics.Engine;
using FSO.Files.Utils;
using FSO.SimAntics.Model;

namespace FSO.SimAntics.Primitives
{

    public class VMGenericTSOCall : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMGenericTSOCallOperand)args;

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
                    context.Thread.TempRegisters[0] = context.Caller.ObjectID;
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
                        context.Caller.PlaceInSlot(temp2, i, false, context.VM.Context); //slot to slot needs no cleanup
                        context.StackObject.PlaceInSlot(temp1, i, false, context.VM.Context);
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
