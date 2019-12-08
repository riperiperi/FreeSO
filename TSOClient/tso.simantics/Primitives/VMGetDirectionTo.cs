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
using System.IO;

namespace FSO.SimAntics.Primitives
{
    public class VMGetDirectionTo : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMGetDirectionToOperand)args;

            var obj2 = context.StackObject;
            VMEntity obj1;
            //todo: wrong flag below?
            if ((operand.Flags & 1) == 0) obj1 = context.Caller;
            else obj1 = context.VM.GetObjectById(VMMemory.GetVariable(context, operand.ObjectScope, operand.OScopeData));

            var pos1 =  obj1.Position;
            var pos2 = obj2.Position;

            var direction = DirectionUtils.Normalize(Math.Atan2(pos2.x - pos1.x, pos1.y - pos2.y));

            var result = Math.Round((DirectionUtils.PosMod(direction, Math.PI*2)/Math.PI)*4);

            VMMemory.SetVariable(context, operand.ResultOwner, operand.ResultData, (short)result);

            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMGetDirectionToOperand : VMPrimitiveOperand
    {
        public short ResultData { get; set; }
        public VMVariableScope ResultOwner { get; set; }
        public byte Flags { get; set; }
        public VMVariableScope ObjectScope { get; set; }
        public short OScopeData { get; set; }

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                ResultData = io.ReadInt16();
                ResultOwner = (VMVariableScope)io.ReadUInt16();
                Flags = io.ReadByte();
                ObjectScope = (VMVariableScope)io.ReadByte();
                OScopeData = io.ReadInt16();

                if ((Flags & 1) == 0)
                {
                    ObjectScope = VMVariableScope.MyObject;
                    OScopeData = 11;
                }
                Flags |= 1;
            }
        }

        public void Write(byte[] bytes) {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(ResultData);
                io.Write((ushort)ResultOwner);
                io.Write(Flags);
                io.Write((byte)ObjectScope);
                io.Write(OScopeData);
            }
        }
        #endregion
    }
}