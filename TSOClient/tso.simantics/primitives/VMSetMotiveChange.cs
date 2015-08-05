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
using FSO.SimAntics.Engine.Scopes;
using FSO.SimAntics.Model;
using FSO.SimAntics.Engine.Utils;

namespace FSO.SimAntics.Primitives
{
    public class VMSetMotiveChange : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            var operand = context.GetCurrentOperand<VMSetMotiveChangeOperand>();
            var avatar = ((VMAvatar)context.Caller);

            if ((operand.Flags & VMSetMotiveChangeFlags.ClearAll) > 0)
            {
                avatar.ClearMotiveChanges();
            }
            else
            {
                var PerHourChange = VMMemory.GetVariable(context, (VMVariableScope)operand.DeltaOwner, (ushort)operand.DeltaData);
                var MaxValue = VMMemory.GetVariable(context, (VMVariableScope)operand.MaxOwner, (ushort)operand.MaxData);
                avatar.SetMotiveChange(operand.Motive, PerHourChange, MaxValue);
            }

            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMSetMotiveChangeOperand : VMPrimitiveOperand {

        public VMVariableScope DeltaOwner;
        public ushort DeltaData;

        public VMVariableScope MaxOwner;
        public ushort MaxData;

        public VMSetMotiveChangeFlags Flags;
        public VMMotive Motive;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes){
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN)){

                DeltaOwner = (VMVariableScope)io.ReadByte();
                MaxOwner = (VMVariableScope)io.ReadByte();
                Motive = (VMMotive)io.ReadByte();
                Flags = (VMSetMotiveChangeFlags)io.ReadByte();

                DeltaData = io.ReadUInt16();
                MaxData = io.ReadUInt16();
            }
        }
        #endregion
    }

    [Flags]
    public enum VMSetMotiveChangeFlags {
        ClearAll = 1
    }
}
