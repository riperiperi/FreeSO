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
using FSO.SimAntics.Engine.Utils;
using FSO.SimAntics.Engine.Scopes;

namespace FSO.SimAntics.Primitives
{
    public class VMGetDistanceTo : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            var operand = context.GetCurrentOperand<VMGetDistanceToOperand>();

            var obj1 = context.StackObject;
            VMEntity obj2;
            if ((operand.Flags & 1) == 0) obj2 = context.Caller;
            else obj2 = context.VM.GetObjectById(VMMemory.GetVariable(context, (VMVariableScope)operand.ObjectScope, operand.OScopeData));

            var pos1 = obj1.Position;
            var pos2 = obj2.Position;

            var result = (short)Math.Floor(Math.Sqrt(Math.Pow(pos1.x - pos2.x, 2) + Math.Pow(pos1.y - pos2.y, 2))/16.0);

            context.Thread.TempRegisters[operand.TempNum] = result;        
            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMGetDistanceToOperand : VMPrimitiveOperand
    { 
        public ushort TempNum;
        public byte Flags;
        public byte ObjectScope;
        public ushort OScopeData;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                TempNum = io.ReadUInt16();
                Flags = io.ReadByte();
                ObjectScope = io.ReadByte();
                OScopeData = io.ReadUInt16();
            }
        }
        #endregion
    }
}
