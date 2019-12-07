﻿/*
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
using System.IO;

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
                if (item is VMGameObject) item.WorldUI?.PrepareSlotInterpolation();
                var itemTest = context.StackObject.GetSlot(dest);
                if (itemTest == null)
                {
                    return (context.StackObject.PlaceInSlot(item, dest, true, context.VM.Context)) ? VMPrimitiveExitCode.GOTO_TRUE : VMPrimitiveExitCode.GOTO_FALSE;
                }
                else return VMPrimitiveExitCode.GOTO_FALSE; //cannot replace items currently in slots
            }
            else return VMPrimitiveExitCode.GOTO_FALSE;
        }
    }

    public class VMDropOntoOperand : VMPrimitiveOperand
    {
        public ushort SrcSlotMode { get; set; }
        public ushort SrcSlotNum { get; set; }
        public ushort DestSlotMode { get; set; }
        public ushort DestSlotNum { get; set; }

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

        public void Write(byte[] bytes) {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(SrcSlotMode);
                io.Write(SrcSlotNum);
                io.Write(DestSlotMode);
                io.Write(DestSlotNum);
            }
        }
        #endregion
    }
}
