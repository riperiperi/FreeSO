/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Files.Utils;
using FSO.SimAntics.Engine.Utils;
using FSO.SimAntics.Engine.Scopes;
using Microsoft.Xna.Framework;
using FSO.LotView.Model;
using System.IO;

namespace FSO.SimAntics.Engine.Primitives
{
    public class VMGotoRoutingSlot : VMPrimitiveHandler {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMGotoRoutingSlotOperand)args;
            
            var slot = VMMemory.GetSlot(context, operand.Type, operand.Data);
            var obj = context.StackObject;
            var avatar = context.Caller;

            //Routing slots must be type 3.
            if (slot.Type == 3)
            {
                var pathFinder = context.Thread.PushNewRoutingFrame(context, !operand.NoFailureTrees);
                var success = pathFinder.InitRoutes(slot, context.StackObject);

                return VMPrimitiveExitCode.CONTINUE;
            }
            else return VMPrimitiveExitCode.GOTO_FALSE;
        }
    }

    public class VMGotoRoutingSlotOperand : VMPrimitiveOperand
    {
        public ushort Data;
        public VMSlotScope Type;
        public byte Flags;

        public bool NoFailureTrees
        {
            get
            {
                return (Flags & 1) > 0;
            }
        }

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes){
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN)){
                Data = io.ReadUInt16();
                Type = (VMSlotScope)io.ReadUInt16();
                Flags = io.ReadByte();
            }
        }

        public void Write(byte[] bytes) {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(Data);
                io.Write((ushort)Type);
                io.Write(Flags);
            }
        }
        #endregion

        public override string ToString()
        {
            return "Go To Routing Slot (" + Type.ToString() + ": #" + Data + ")";
        }
    }

}
