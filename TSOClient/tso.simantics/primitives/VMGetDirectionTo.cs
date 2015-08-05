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
using FSO.Common.Utils;

namespace FSO.SimAntics.Primitives
{
    public class VMGetDirectionTo : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            var operand = context.GetCurrentOperand<VMGetDirectionToOperand>();

            var obj2 = context.StackObject;
            VMEntity obj1;

            obj1 = context.Caller;
            //todo: wrong flag below?
            //if ((operand.Flags & 1) > 0) obj1 = context.Caller;
            //else obj1 = context.VM.GetObjectById(VMMemory.GetVariable(context, (VMVariableScope)operand.ObjectScope, operand.OScopeData));


            //var pos1 = obj1.Position;
            var pos1 = obj1.Position;
            var pos2 = obj2.Position;

            var direction = DirectionUtils.Normalize(Math.Atan2(pos2.x - pos1.x, pos1.y - pos2.y));

            var result = Math.Round((DirectionUtils.PosMod(direction, Math.PI*2)/Math.PI)*4);

            VMMemory.SetVariable(context, (VMVariableScope)operand.ResultOwner, operand.ResultData, (short)result);

            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMGetDirectionToOperand : VMPrimitiveOperand
    {
        public ushort ResultData;
        public ushort ResultOwner;
        public byte Flags;
        public byte ObjectScope;
        public ushort OScopeData;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                ResultData = io.ReadUInt16();
                ResultOwner = io.ReadUInt16();
                Flags = io.ReadByte();
                ObjectScope = io.ReadByte();
                OScopeData = io.ReadUInt16();
            }
        }
        #endregion
    }
}