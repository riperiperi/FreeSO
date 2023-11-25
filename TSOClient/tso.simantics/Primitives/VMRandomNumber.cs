using FSO.Files.Utils;
using FSO.SimAntics.Engine.Scopes;
using FSO.SimAntics.Engine.Utils;
using System.IO;

namespace FSO.SimAntics.Engine.Primitives
{
    public class VMRandomNumber : VMPrimitiveHandler {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMRandomNumberOperand)args;
            var rangeValue = (ushort)VMMemory.GetVariable(context, operand.RangeScope, operand.RangeData);
            var result = context.VM.Context.NextRandom(rangeValue);
            VMMemory.SetVariable(context, operand.DestinationScope, operand.DestinationData, (short)result);
            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMRandomNumberOperand : VMPrimitiveOperand
    {
        public short DestinationData { get; set; }
        public VMVariableScope DestinationScope { get; set; }
        public short RangeData { get; set; }
        public VMVariableScope RangeScope { get; set; }

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes){
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN)){
                DestinationData = io.ReadInt16();
                DestinationScope = (VMVariableScope)io.ReadUInt16();
                RangeData = io.ReadInt16();
                RangeScope = (VMVariableScope)io.ReadUInt16();
            }
        }

        public void Write(byte[] bytes) {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(DestinationData);
                io.Write((ushort)DestinationScope);
                io.Write(RangeData);
                io.Write((ushort)RangeScope);
            }
        }
        #endregion
    }
}
