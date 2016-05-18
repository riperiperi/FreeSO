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
using System.IO;

namespace FSO.SimAntics.Primitives
{

    public class VMGenericTSOCall : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMGenericTSOCallOperand)args;

            switch (operand.Call)
            {
                case VMGenericTSOCallMode.GetInteractionResult:
                    context.Thread.TempRegisters[0] = 2; //0=none, 1=reject, 2=accept, 3=pet
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTSOCallMode.GetIsPendingDeletion:
                    return VMPrimitiveExitCode.GOTO_FALSE;
                case VMGenericTSOCallMode.IsTemp0AvatarIgnoringTemp1Avatar:
                    context.Thread.TempRegisters[0] = 0;
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTSOCallMode.IsGlobalBroken:
                    return VMPrimitiveExitCode.GOTO_FALSE;
                case VMGenericTSOCallMode.GetLotOwner:
                case VMGenericTSOCallMode.StackObjectOwnerID:
                    context.Thread.TempRegisters[0] = context.Caller.ObjectID;
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTSOCallMode.SetActionIconToStackObject:
                    context.Thread.Queue[0].IconOwner = context.StackObject;
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTSOCallMode.HasTemporaryID:
                    return VMPrimitiveExitCode.GOTO_FALSE; //used by real game to check if object is persistently tracked probably.
                case VMGenericTSOCallMode.SwapMyAndStackObjectsSlots:
                    var cont1 = context.Caller.Container;
                    var cont2 = context.StackObject.Container;
                    var contS1 = context.Caller.ContainerSlot;
                    var contS2 = context.StackObject.ContainerSlot;
                    if (cont1 != null && cont2 != null)
                    {
                        cont1.ClearSlot(contS1);
                        cont2.ClearSlot(contS2);
                        cont1.PlaceInSlot(context.StackObject, contS1, false, context.VM.Context);
                        cont2.PlaceInSlot(context.Caller, contS2, false, context.VM.Context);
                    }
                    /*
                     * well here's some code to swap slots... but this function actually swaps containers
                    int total = Math.Min(context.StackObject.TotalSlots(), context.Caller.TotalSlots());
                    for (int i = 0; i < total; i++)
                    {
                        VMEntity temp1 = context.Caller.GetSlot(i);
                        VMEntity temp2 = context.StackObject.GetSlot(i);
                        if (temp1 != null) context.Caller.ClearSlot(i);
                        if (temp2 != null)
                        {
                            context.StackObject.ClearSlot(i);
                            context.Caller.PlaceInSlot(temp2, i, false, context.VM.Context); //slot to slot needs no cleanup
                        }
                        if (temp1 != null) context.StackObject.PlaceInSlot(temp1, i, false, context.VM.Context);
                    }
                    */
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTSOCallMode.ReturnLotCategory:
                    context.Thread.TempRegisters[0] = 6; //skills lot. see #Lot Types in global.iff
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTSOCallMode.TestStackObject:
                    return (context.StackObject != null) ? VMPrimitiveExitCode.GOTO_TRUE : VMPrimitiveExitCode.GOTO_FALSE;
                case VMGenericTSOCallMode.DoIOwnThisObject:
                    context.Thread.TempRegisters[0] = 1;
                    return VMPrimitiveExitCode.GOTO_TRUE;
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

        public void Write(byte[] bytes) {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write((byte)Call);
            }
        }
        #endregion
    }
}
