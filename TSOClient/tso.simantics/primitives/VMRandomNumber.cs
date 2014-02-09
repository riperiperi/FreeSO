using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.files.utils;
using tso.simantics.engine.scopes;
using tso.simantics.engine.utils;

namespace tso.simantics.engine.primitives
{
    public class VMRandomNumber : VMPrimitiveHandler {
        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            var operand = context.GetCurrentOperand<VMRandomNumberOperand>();

            var description = "random: less than " + VMMemory.DescribeVariable(context, operand.RangeScope, operand.RangeData);
            description += " written to " + VMMemory.DescribeVariable(context, operand.DestinationScope, operand.DestinationData);
            Trace(description);


            //TODO: Make this deterministic
            var rangeValue = VMMemory.GetVariable(context, operand.RangeScope, operand.RangeData);

            var rand = new Random();
            var result = rand.Next(rangeValue);
            VMMemory.SetVariable(context, operand.DestinationScope, operand.DestinationData, (short)result);
            Trace("set " + operand.DestinationScope + " #" + operand.DestinationData + " to random " + result);
            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMRandomNumberOperand : VMPrimitiveOperand
    {
        public ushort DestinationData;
        public VMVariableScope DestinationScope;
        public ushort RangeData;
        public VMVariableScope RangeScope;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes){
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN)){
                DestinationData = io.ReadUInt16();
                DestinationScope = (VMVariableScope)io.ReadUInt16();
                RangeData = io.ReadUInt16();
                RangeScope = (VMVariableScope)io.ReadUInt16();
            }
        }
        #endregion
    }
}
