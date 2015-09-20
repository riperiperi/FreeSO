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

namespace FSO.SimAntics.Primitives
{
    public class VMDropOnto : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMDropOntoOperand)args;
            var src = (operand.SrcSlotMode == 1) ? (ushort)context.Args[operand.SrcSlotNum] : operand.SrcSlotNum;
            var dest = (operand.DestSlotMode == 1) ? (ushort)context.Args[operand.DestSlotNum] : operand.DestSlotNum;

            var item = context.Caller.GetSlot(src);
            if (item != null)
            {
                var itemTest = context.StackObject.GetSlot(dest);
                if (itemTest == null)
                {
                    context.Caller.ClearSlot(src);
                    context.StackObject.PlaceInSlot(item, dest, false, context.VM.Context); //slot to slot needs no cleanup
                    return VMPrimitiveExitCode.GOTO_TRUE;
                }
                else return VMPrimitiveExitCode.GOTO_FALSE; //cannot replace items currently in slots
            }
            else return VMPrimitiveExitCode.GOTO_FALSE;
        }
    }

    public class VMDropOntoOperand : VMPrimitiveOperand
    {
        public ushort SrcSlotMode;
        public ushort SrcSlotNum;
        public ushort DestSlotMode;
        public ushort DestSlotNum;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                SrcSlotMode = io.ReadUInt16();
                SrcSlotNum = io.ReadUInt16();
                DestSlotMode = io.ReadUInt16();
                DestSlotNum = io.ReadUInt16();
            }
        }
        #endregion
    }
}
