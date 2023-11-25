using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.JIT.Translation.CSharp.Primitives;
using FSO.SimAntics.JIT.Translation.Model;
using FSO.SimAntics.JIT.Translation.Primitives;

namespace FSO.SimAntics.JIT.Translation.CSharp
{
    public class CSTranslationPrimitives : AbstractTranslationPrimitives
    {
        public CSTranslationPrimitives()
        {
            Map[SharedPrimitives.Expression] = typeof(CSExpressionPrimitive);
            Map[SharedPrimitives.CreateNewObjectInstance] = typeof(CSCreateObjectInstancePrimitive);
            Map[SharedPrimitives.Subroutine] = typeof(CSSubroutinePrimitive);
            Map[SharedPrimitives.SetToNext] = typeof(CSSetToNextPrimitive);
        }

        protected override AbstractTranslationPrimitive GetFallbackPrim(BHAVInstruction instruction, byte index)
        {
            return new CSFallbackPrimitive(instruction, index);
        }
    }
}
