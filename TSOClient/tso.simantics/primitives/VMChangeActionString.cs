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
using FSO.Files.Formats.IFF.Chunks;

namespace FSO.SimAntics.Primitives
{
    public class VMChangeActionString : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            var operand = context.GetCurrentOperand<VMChangeActionStringOperand>();
            var table = context.CodeOwner.Get<STR>(operand.StringTable);
            if (table != null) context.Thread.Queue[0].Name = table.GetString(operand.StringID - 1);
            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMChangeActionStringOperand : VMPrimitiveOperand
    {
        public ushort StringTable;
        public ushort Unknown;
        public byte StringID;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                StringTable = io.ReadUInt16();
                Unknown = io.ReadUInt16();
                StringID = io.ReadByte();
            }
        }
        #endregion
    }
}
