using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.JIT.Model
{
    public enum ModuleSource
    {

        /// <summary>
        /// Generated from a previous run of the game, perhaps in another system, and precompiled into one assembly.
        /// Assembly should contain modules for most object/global iffs in the game, but not necessarily all.
        /// Modules contain a checksum of the source IFF "execution relevant resources", eg. BHAV and BCON.
        /// If this mismatches the runtime, AOT is ignored and we fallback on interpreter or generating an assembly with JIT.
        /// </summary>
        AOT,

        /// <summary>
        /// Generated while the game is running, perhaps cached from a previous run. Compiled with CSharpCodeProvider.
        /// Only available on platforms with Roslyn and that support JIT. (mobile and .NET core are currently out)
        /// A JIT assembly should only contain one module; for the iff it was produced for.
        /// Always takes priority over an AOT module, as it may include recent changes to an object or global.
        /// </summary>
        JIT,
    }
}
