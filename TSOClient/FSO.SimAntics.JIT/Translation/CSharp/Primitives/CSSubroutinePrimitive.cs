using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.Engine;
using FSO.SimAntics.JIT.Runtime;
using FSO.SimAntics.JIT.Translation.Model;
using FSO.SimAntics.JIT.Translation.Primitives;
using FSO.SimAntics.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.JIT.Translation.CSharp.Primitives
{
    public class CSSubroutinePrimitive : AbstractTranslationPrimitive
    {
        public CSSubroutinePrimitive(BHAVInstruction instruction, byte index) : base(instruction, index)
        {
        }

        private static Tuple<bool, string, string, int> GetInfoFromId(CSTranslationContext ctx, ushort call)
        {
            SimAnticsModule module;
            CSTranslationContext ctx2;
            //find the bhav.
            if (call < 4096)
            {
                //global
                module = ctx.GlobalModule;
                ctx2 = ctx.GlobalContext as CSTranslationContext;
            }
            else if (call < 8192)
            {
                //local (can only be us)
                module = null;
                ctx2 = null;
            }
            else
            {
                //semiglobal
                module = ctx.SemiGlobalModule;
                ctx2 = ctx.SemiGlobalContext as CSTranslationContext;
            }
            
            if (ctx2 != null)
            {
                //in previously compiled context
                StructuredBHAV newBHAV;
                if (ctx2.BHAVInfo.TryGetValue(call, out newBHAV))
                {
                    return new Tuple<bool, string, string, int>(newBHAV.Yields, ctx2.NamespaceName, CSTranslationContext.FormatName(newBHAV.Source.ChunkLabel) + "_" + newBHAV.Source.ChunkID, Math.Max(4, (int)newBHAV.Source.Args));
                }
                return new Tuple<bool, string, string, int>(true, null, null, 4);
            }
            if (module != null)
            {
                //in compiled module
                var func = module.GetFunction(call);
                if (func == null) return new Tuple<bool, string, string, int>(true, null, null, 4);
                return new Tuple<bool, string, string, int>(func is IBHAV, module.GetType().Namespace, func.GetType().Name, (func as IInlineBHAV)?.ArgCount ?? 4);
            }
            else
            {
                StructuredBHAV newBHAV;
                if (ctx.BHAVInfo.TryGetValue(call, out newBHAV))
                {
                    return new Tuple<bool, string, string, int>(newBHAV.Yields, null, CSTranslationContext.FormatName(newBHAV.Source.ChunkLabel) + "_" + newBHAV.Source.ChunkID, Math.Max(4, (int)newBHAV.Source.Args));
                }
                return new Tuple<bool, string, string, int>(true, null, null, 4);
            }
        }

        public void InitInfo(CSTranslationContext ctx)
        {
            var info = GetInfoFromId(ctx, Instruction.Opcode);
            _CanYield = info.Item1;
            FuncFile = info.Item2;
            FuncClass = info.Item3;
            FuncArgs = info.Item4;
        }

        private bool _CanYield;
        private string FuncFile;
        private string FuncClass;
        public int FuncArgs;
        public override bool CanYield {
            get 
            {
                return _CanYield;
            }
        }

        public override PrimitiveReturnType ReturnType => (CanYield) ? PrimitiveReturnType.SimanticsSubroutine : PrimitiveReturnType.NativeExpressionTrueFalse;

        public override List<string> CodeGen(TranslationContext context)
        {
            var csContext = (CSTranslationContext)context;
            var csClass = csContext.CurrentClass;
            var operand = GetOperand<VMSubRoutineOperand>();

            var oargs = operand.Arguments;
            if (!CanYield && FuncArgs != oargs.Length)
            {
                oargs = new short[FuncArgs];
                Array.Copy(operand.Arguments, oargs, Math.Min(operand.Arguments.Length, FuncArgs));
            }

            csClass.UseTemps = true;
            var useTemp0 = operand.UseTemp0;
            int i = 0;
            var args = string.Join(", ", oargs.Select(x => {
                if (x == -1 && operand.UseTemp0)
                {

                    return $"temps[{i++}]";
                }
                i++;
                return x.ToString();
            }));

            if (CanYield)
            {
                //enqueue our subroutine and return control to the thread manager.
                //TODO: shortcut for passing new function without lookup? (we know where it is)

                //returns VMPrimitiveExitCode.CONTINUE
                return Line($"context.Thread.ExecuteSubRoutine(context, {Instruction.Opcode}, new VMSubRoutineOperand(new short[] {{{args}}})) /* {FuncClass ?? "unknown"} */");
            }
            else
            {
                if (FuncFile == null)
                {
                    if (FuncClass != null)
                    {
                        //function is in our module.
                        return Line($"{csContext.ModuleName}._{FuncClass}.Execute(context, {args})");
                    }
                }
                else
                {
                    csContext.Includes.Add($"using {FuncFile};");
                    csContext.RequiredModules.Add(FuncFile);
                    return Line($"{FuncFile.Substring(FuncFile.LastIndexOf('.') + 1)}Module._{FuncClass}.ExecuteExternal(context, {args})");
                }
                return Line($"true");
            }
        }
    }
}
