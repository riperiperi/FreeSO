using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.Model;
using FSO.SimAntics.Primitives;

namespace FSO.SimAntics.JIT.Translation.CSharp.Primitives
{
    public class CSGenericSimsCallPrimitive : CSFallbackPrimitive
    {
        private VMGenericTS1CallOperand Op;
        public CSGenericSimsCallPrimitive(BHAVInstruction instruction, byte index) : base(instruction, index)
        {
            Op = GetOperand<VMGenericTS1CallOperand>();
        }

        public override bool CanYield => VM.GlobTS1 && Op.Call == VMGenericTS1CallMode.ChangeToLotInTemp0;
        
    }
}
