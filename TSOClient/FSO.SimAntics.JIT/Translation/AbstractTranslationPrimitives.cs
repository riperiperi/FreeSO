using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.JIT.Translation.Model;
using FSO.SimAntics.JIT.Translation.Primitives;
using System;
using System.Collections.Generic;

namespace FSO.SimAntics.JIT.Translation
{
    public class AbstractTranslationPrimitives
    {
        protected Dictionary<SharedPrimitives, Type> Map = new Dictionary<SharedPrimitives, Type>();

        protected virtual AbstractTranslationPrimitive GetFallbackPrim(BHAVInstruction instruction, byte index)
        {
            return new AbstractTranslationPrimitive(instruction, index);
        }

        public AbstractTranslationPrimitive GetPrimitive(BHAVInstruction instruction, byte index)
        {
            Type primType;
            var prim = (SharedPrimitives)Math.Min((ushort)255, instruction.Opcode);

            if (!Map.TryGetValue(prim, out primType))
                return GetFallbackPrim(instruction, index);
            else
                return (AbstractTranslationPrimitive)Activator.CreateInstance(primType, new object[] { instruction, index });
        }
    }
}
