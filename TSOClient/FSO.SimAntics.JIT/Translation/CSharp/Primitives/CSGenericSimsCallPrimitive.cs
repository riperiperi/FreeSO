using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.JIT.Translation.Model;
using FSO.SimAntics.Model;
using FSO.SimAntics.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
