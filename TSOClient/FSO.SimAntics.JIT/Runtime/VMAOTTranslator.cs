using FSO.Content;
using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.Engine;

namespace FSO.SimAntics.JIT.Runtime
{
    public class VMAOTTranslator : VMTranslator
    {
        private AssemblyStore Assemblies;
        public VMAOTTranslator(AssemblyStore assemblies)
        {
            Assemblies = assemblies;
        }

        public override VMRoutine Assemble(BHAV bhav, GameIffResource res)
        {
            VMRoutine routine;
            var assembly = Assemblies.GetModuleFor(bhav.ChunkParent);
            object func = assembly?.GetFunction(bhav.ChunkID);
            if (func != null)
            {
                if (func is IBHAV)
                {
                    routine = new VMAOTRoutine((IBHAV)func);
                }
                else
                {
                    routine = new VMAOTInlineRoutine((IInlineBHAV)func);
                }
            }
            else
            {
                routine = new VMRoutine();
            }
            PopulateRoutineFields(bhav, routine);
            return routine;
        }
    }
}
