using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.Engine.Primitives;
using FSO.SimAntics.JIT.Translation.Model;

namespace FSO.SimAntics.JIT.Translation.CSharp.Primitives
{
    public class CSCreateObjectInstancePrimitive : CSFallbackPrimitive
    {
        private VMCreateObjectInstanceOperand Op;
        public CSCreateObjectInstancePrimitive(BHAVInstruction instruction, byte index) : base(instruction, index)
        {
            Op = GetOperand<VMCreateObjectInstanceOperand>();
        }

        public override bool CanYield => Op.ReturnImmediately;

        public override PrimitiveReturnType ReturnType => Op.ReturnImmediately ? PrimitiveReturnType.SimanticsSubroutine : PrimitiveReturnType.SimanticsTrueFalse;
    }
}
