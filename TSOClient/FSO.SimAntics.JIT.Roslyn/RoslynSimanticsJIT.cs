using FSO.Content;
using FSO.SimAntics.JIT.Translation.CSharp;
using System.Collections.Generic;

namespace FSO.SimAntics.JIT.Roslyn
{
    /// <summary>
    /// The Roslyn SimAntics JIT asynchronously loads or compiles SimAntics routines as C# assemblies.
    /// </summary>
    public class RoslynSimanticsJIT
    {
        public RoslynSimanticsContext Context;
        private Dictionary<string, RoslynSimanticsModule> FormattedNameToRoslynModule = new Dictionary<string, RoslynSimanticsModule>();

        public RoslynSimanticsJIT()
        {
            Context = new RoslynSimanticsContext(this);
            Context.Init();
        }

        public RoslynSimanticsModule GetModuleFor(GameIffResource res)
        {
            var source = res.MainIff;
            var name = CSTranslationContext.FormatName(source.Filename.Substring(0, source.Filename.Length - 4));
            RoslynSimanticsModule result;
            lock (FormattedNameToRoslynModule) {
                if (FormattedNameToRoslynModule.TryGetValue(name, out result))
                {
                    return result;
                } else
                {
                    //try to create one!
                    result = new RoslynSimanticsModule(Context, res);
                    FormattedNameToRoslynModule[name] = result;
                    return result;
                }
            }
        }
    }
}
