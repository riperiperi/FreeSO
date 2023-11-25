using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.JIT.Translation.Primitives;
using System;
using System.Collections.Generic;

namespace FSO.SimAntics.JIT.Translation.CSharp.Primitives
{
    public class CSFallbackPrimitive : AbstractTranslationPrimitive
    {
        public CSFallbackPrimitive(BHAVInstruction instruction, byte index) : base(instruction, index)
        {
        }

        public override List<string> CodeGen(TranslationContext context)
        {
            var csContext = (CSTranslationContext)context;
            var csClass = csContext.CurrentClass;

            var descriptor = VMContext.Primitives[(byte)Primitive];

            if (descriptor == null) return Line($"new VMPrimitiveExitCode() /** missing opcode {(byte)Primitive} **/");

            //find fallback
            var fallbackName = descriptor.GetHandler().GetType().Name;
            var fallbackOperand = descriptor.OperandModel.Name;

            //add operand to context
            //example:
            // new VMSetToNextOperand().Read(new byte[8])
            csClass.OperandDefinitions.Add(Index, $"private {fallbackOperand} operand{Index} = Op.Read<{fallbackOperand}>(new byte[] {{{String.Join(", ", Instruction.Operand)}}});");

            if (!csClass.PrimitiveDefinitions.ContainsKey(Primitive)) {
                csClass.PrimitiveDefinitions.Add(Primitive, $"private {fallbackName} _prim_{fallbackName} = new {fallbackName}();");
            }

            return Line($"_prim_{fallbackName}.Execute(context, operand{Index})");
        }
    }
}
