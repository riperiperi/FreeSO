using FSO.Common.Utils;
using FSO.Content;
using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.Engine;
using FSO.SimAntics.JIT.Runtime;

namespace FSO.SimAntics.JIT.Roslyn
{
    public class VMRoslynTranslator : VMTranslator
    {
        private RoslynSimanticsJIT Assemblies;
        public VMRoslynTranslator(RoslynSimanticsJIT assemblies)
        {
            Assemblies = assemblies;
        }

        private VMRoutine RoutineFromModule(BHAV bhav, SimAnticsModule module)
        {
            object func = module.GetFunction(bhav.ChunkID);
            VMRoutine routine;
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

        public override VMRoutine Assemble(BHAV bhav, GameIffResource res)
        {
            VMRoutine routine;
            // firstly, try to find the assembly

            //var assembly = Assemblies.GetModuleFor(bhav.ChunkParent);

            // three possibilities: 
            // - module is loading (null for now): create a JIT routine, which will be updated with the appropriate function when it is available
            // - module is finished loading: create an AOT routine, which hsa the function baked in
            // - forced interpreter (not impl yet)

            //if this bhav has a cached jit module, just use that rather than doing a lookup.

            var aot = (SimAnticsModule)bhav.ChunkParent.CachedJITModule;
            if (aot != null) return RoutineFromModule(bhav, aot);

            var assembly = Assemblies.GetModuleFor(res);
            if (assembly != null)
            {
                if (assembly.Module != null)
                {
                    return RoutineFromModule(bhav, assembly.Module);
                }
                // need to load it or wait for loading.
                var result = new VMRoslynRoutine();
                PopulateRoutineFields(bhav, result);
                assembly.GetModuleAsync().ContinueWith(moduleT =>
                {
                    //TODO: thread safe for server
                    // the main concern is injecting a JIT routine while the interpreter is running that function.
                    // in that case, it could attempt to enter into the the JIT routine at an invalid entry point 
                    // (JIT execution can only enter from the start, loop points or after yield)
                    GameThread.InUpdate(() =>
                    {
                        SimAnticsModule module = (moduleT.IsFaulted) ? null : moduleT.Result;
                        object func = module?.GetFunction(bhav.ChunkID);
                        if (func != null)
                        {
                            if (func is IBHAV)
                            {
                                result.SetJITRoutine((IBHAV)func);
                            }
                            else
                            {
                                result.SetJITRoutine((IInlineBHAV)func);
                            }
                        }
                        else
                        {
                            // failed... stay on interpreter
                        }
                    });
                });
                return result;
            }

            routine = new VMRoutine();
            PopulateRoutineFields(bhav, routine);
            return routine;
        }
    }
}
