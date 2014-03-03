using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Simantics.engine;
using TSO.Files.utils;

namespace TSO.Simantics.primitives
{
    public class VMSpecialEffect : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            var operand = context.GetCurrentOperand<VMSpecialEffectOperand>();
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
