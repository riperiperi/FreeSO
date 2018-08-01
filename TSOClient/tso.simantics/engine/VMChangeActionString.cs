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
using System.IO;

namespace FSO.SimAntics.Primitives
{
    public class VMChangeActionString : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMChangeActionStringOperand)args;
            STR table = null;
            switch (operand.Scope)
            {
                case 0:
                    table = context.ScopeResource.Get<STR>(operand.StringTable);
                    break;
                case 1:
                    table = context.Callee.SemiGlobal.Get<STR>(operand.StringTable);
                    break;
                case 2:
                    table = context.Global.Resource.Get<STR>(operand.StringTable);
                    break;
            }

            if (table != null)
            {
                var newName = VMDialogHandler.ParseDialogString(context, table.GetString(operand.StringID - 1), table);
                if (context.Thread.IsCheck && context.Thread.ActionStrings != null) {
                    context.Thread.ActionStrings.Add(new VMPieMenuInteraction()
                    {
                        Name = newName,
                        Param0 = context.StackObjectID
                    });
                } else
                    context.Thread.ActiveAction.Name = newName;
            }
            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMChangeActionStringOperand : VMPrimitiveOperand
    {
        public ushort StringTable;
        public ushort Scope;
        public byte StringID;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                StringTable = io.ReadUInt16();
                Scope = io.ReadUInt16();
                StringID = io.ReadByte();
            }
        }

        public void Write(byte[] bytes) {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(StringTable);
                io.Write(Scope);
                io.Write(StringID);
            }
        }
        #endregion
    }
}
