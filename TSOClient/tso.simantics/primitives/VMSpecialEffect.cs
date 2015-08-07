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
    public class VMSpecialEffect : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMSpecialEffectOperand)args;
            //TODO: Implement this, fridge has one when u have no money
            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMSpecialEffectOperand : VMPrimitiveOperand
    {
        public ushort Timeout;
        public sbyte Size;
        public sbyte Zoom;
        public sbyte Flags;
        public sbyte StrIndex;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN)){
                Timeout = io.ReadUInt16();
                Size = (sbyte)io.ReadByte();
                Zoom = (sbyte)io.ReadByte();
                Flags = (sbyte)io.ReadByte();
                StrIndex = (sbyte)io.ReadByte();
            }
        }
        #endregion
    }
}
