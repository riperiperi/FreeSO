using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Simantics.engine;
using TSO.Files.utils;
using TSO.Files.formats.iff.chunks;

namespace TSO.Simantics.primitives
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
