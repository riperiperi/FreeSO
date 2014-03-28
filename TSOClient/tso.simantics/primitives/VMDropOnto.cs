using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Simantics.engine;
using TSO.Files.utils;

namespace TSO.Simantics.primitives
{
    public class VMDropOnto : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            var operand = context.GetCurrentOperand<VMDropOntoOperand>();
            var src = (operand.SrcSlotMode == 1) ? (ushort)context.Args[operand.SrcSlotNum] : operand.SrcSlotNum;
            var dest = (operand.DestSlotMode == 1) ? (ushort)context.Args[operand.DestSlotNum] : operand.DestSlotNum;

            var item = context.Caller.GetSlot(src);
            if (item != null)
            {
                var itemTest = context.StackObject.GetSlot(dest);
                if (itemTest == null)
                {
                    context.Caller.ClearSlot(src);
                    context.StackObject.PlaceInSlot(item, dest);
                    ((VMAvatar)context.Caller).CarryAnimation = null;

                    return VMPrimitiveExitCode.GOTO_TRUE;
                }
                else return VMPrimitiveExitCode.GOTO_FALSE; //cannot replace items currently in slots
            }
            else return VMPrimitiveExitCode.GOTO_FALSE;
        }
    }

    public class VMDropOntoOperand : VMPrimitiveOperand
    {
        public ushort SrcSlotMode;
        public ushort SrcSlotNum;
        public ushort DestSlotMode;
        public ushort DestSlotNum;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                SrcSlotMode = io.ReadUInt16();
                SrcSlotNum = io.ReadUInt16();
                DestSlotMode = io.ReadUInt16();
                DestSlotNum = io.ReadUInt16();
            }
        }
        #endregion
    }
}
