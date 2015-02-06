using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Simantics.engine;
using TSO.Files.utils;
using TSO.Simantics.engine.utils;
using TSO.Simantics.engine.scopes;
using TSO.Common.utils;

namespace TSO.Simantics.primitives
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

            var direction = DirectionUtils.Normalize(Math.Atan2(Math.Floor(pos2.X) - Math.Floor(pos1.X), Math.Floor(pos1.Y) - Math.Floor(pos2.Y)));

            var result = DirectionUtils.PosMod(Math.Round(direction*8), 8);

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