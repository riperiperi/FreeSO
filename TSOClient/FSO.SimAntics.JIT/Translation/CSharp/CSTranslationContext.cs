using FSO.SimAntics.JIT.Translation.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.JIT.Translation.CSharp
{
    public class CSTranslationContext : TranslationContext
    {
        public HashSet<string> Includes = new HashSet<string>()
        {
            "using System;",
            "using System.Collections.Generic;",
            "using System.Linq;",
            "using System.Text;",
            "using System.Threading.Tasks;",
            "using FSO.SimAntics;",
            "using FSO.SimAntics.JIT.Runtime;",
            "using FSO.SimAntics.Engine.Utils;", //VMMemory
            "using FSO.SimAntics.Primitives;",
            "using FSO.SimAntics.Engine.Primitives;",
            "using FSO.SimAntics.Engine;",
            "using FSO.SimAntics.Engine.Scopes;",
            "using FSO.SimAntics.Entities;",
            "using FSO.SimAntics.Model;",
            "using FSO.SimAntics.Model.Platform;",
            "using FSO.SimAntics.Model.TS1Platform;",
            "using FSO.SimAntics.Model.TSOPlatform;",
        };
        public HashSet<string> RequiredModules = new HashSet<string>()
        {

        };

        public string Filename;
        public string ModuleName;
        public string NamespaceName; //should be equal to the IFF name filtered.

        public List<CSTranslationClass> AllClasses = new List<CSTranslationClass>();
        public CSTranslationClass CurrentClass;
        public int MaxLoopCount = 1000000;

        public CSTranslationContext()
        {
            Primitives = new CSTranslationPrimitives();
        }

        public static string FormatName(string name)
        {
            //converts the given name into a class friendly format.
            var result = "";
            var nextCaps = true;
            for (int i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (!char.IsLetterOrDigit(c))
                {
                    nextCaps = true;
                }
                else
                {
                    if (nextCaps) result += char.ToUpperInvariant(c);
                    else result += c;
                    nextCaps = false;
                }
            }
            if (result.Length == 0) result = "Untitled";
            if (char.IsDigit(result[0])) result = "_" + result;
            return result;
        }
    }

    public class CSTranslationClass
    {
        public string ClassName; //should be equal to the subrouting name filtered, but plus chunk ID.
        public StructuredBHAV Structure;
        public bool InlineFunction;
        public int ArgCount;

        //within class

        //fallback definitions
        public Dictionary<SharedPrimitives, string> PrimitiveDefinitions = new Dictionary<SharedPrimitives, string>();
        public Dictionary<byte, string> OperandDefinitions = new Dictionary<byte, string>();

        //translated function
        public Dictionary<byte, CSTranslationInstruction> Instructions;

        //use flags for various features
        public bool UseTemps;
        public bool UseParams;
        public bool UseLocals;
        public bool HasGlobalSwitch;

        public Dictionary<SimanticsBlock, HashSet<string>> SwitchPreviousCases = new Dictionary<SimanticsBlock, HashSet<string>>();

        //useful helpers
        public string Interface => (InlineFunction) ? "IInlineBHAV" : "IBHAV";
        public string FunctionHead => (InlineFunction) ?
            "public override bool Execute(VMStackFrame context, ref byte instruction, params short[] args)" :
            "public VMPrimitiveExitCode Execute(VMStackFrame context, ref byte instruction)";
        public string TrueExp => (InlineFunction) ? "true" : "VMPrimitiveExitCode.RETURN_TRUE";
        public string FalseExp => (InlineFunction) ? "false" : "VMPrimitiveExitCode.RETURN_FALSE";
        public string ErrorExp => (InlineFunction) ? "false" : "VMPrimitiveExitCode.ERROR";

        public override string ToString()
        {
            return ClassName;
        }
    }

    public class CSTranslationInstruction
    {
        public bool Yields;
        public PrimitiveReturnType ReturnType;
        public List<string> Body;

        public CSTranslationInstruction(List<string> body, PrimitiveReturnType returnType, bool yields)
        {
            Body = body;
            ReturnType = returnType;
            Yields = yields;
        }
    }
}
