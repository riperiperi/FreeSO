using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.JIT.Runtime;
using FSO.SimAntics.JIT.Translation.CSharp.Engine;
using FSO.SimAntics.JIT.Translation.CSharp.Primitives;
using FSO.SimAntics.JIT.Translation.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.SimAntics.JIT.Translation.CSharp
{
    public class CSTranslator
    {
        public CSTranslationContext Context;
        public static uint JITVersion = 1;
        public CSTranslator()
        {
            Context = new CSTranslationContext();
        }

        private string GetIndentation(int level)
        {
            return "".PadRight(level * 4, ' ');
        }

        public CSTranslationClass TranslateBHAV(StructuredBHAV bhav)
        {
            var csClass = new CSTranslationClass();
            csClass.ClassName = CSTranslationContext.FormatName(bhav.Source.ChunkLabel) + "_" + bhav.Source.ChunkID;
            csClass.Structure = bhav;
            csClass.InlineFunction = !bhav.Yields;
            csClass.ArgCount = Math.Max(4, (int)bhav.Source.Args); //possible to make this tighter by analysing func
            Context.CurrentBHAV = bhav.Source;
            Context.CurrentClass = csClass;

            csClass.Instructions = bhav.Instructions.ToDictionary(x => x.Key, x =>
            {
                var inst = x.Value;
                return new CSTranslationInstruction(inst.Translator.CodeGen(Context), inst.ReturnType, inst.Yields);
            });

            return csClass;
        }

        private int _IndentLevel;
        private int IndentLevel
        {
            set
            {
                if (value > 50) throw new Exception("Indent level too high!");
                Indent = GetIndentation(value);
                _IndentLevel = value;
            }
            get
            {
                return _IndentLevel;
            }
        } 
        private string Indent;
        private StringBuilder Builder;
        private byte LastInstruction;

        private void WriteLines(List<string> lines, bool appendLastSemicolon)
        {
            var ind = 0;
            foreach (var line in lines)
            {
                ind++;
                WriteLine(line + ((appendLastSemicolon && ind == lines.Count)?";":""));
            }
        }

        private void WriteLine(string line)
        {
            Builder.Append(Indent);
            Builder.AppendLine(line);
        }

        private void WriteLine()
        {
            Builder.AppendLine();
        }

        private void UpdateInstruction()
        {
            WriteLine($"instruction = {LastInstruction};");
        }

        private void OutputReturnBlockOrJump(byte instruction, CSTranslationClass cls)
        {
            switch (instruction)
            {
                case 255:
                    //return false
                    UpdateInstruction();
                    WriteLine($"return {cls.FalseExp};");
                    break;
                case 254:
                    //return true
                    UpdateInstruction();
                    WriteLine($"return {cls.TrueExp};");
                    break;
                case 253:
                    UpdateInstruction();
                    WriteLine($"return {cls.ErrorExp};");
                    break;
                default:
                    var block = cls.Structure.RootBlock.Body.FirstOrDefault(x => x.StartInstructionIndex == instruction);
                    if (block == null) goto case 255;
                    if (block.Parent != cls.Structure.RootBlock)
                    {
                        //ready for a statement block.
                        OutputBlock(block, cls);
                    }
                    else
                    {
                        LastInstruction = instruction;
                        UpdateInstruction();
                        //jump back into the global switch
                    }
                    break;
            }
        }

        private void OutputBinaryBranch(BHAVInstruction inst, CSTranslationClass cls)
        {
            WriteLine("{");
            IndentLevel++;
            OutputReturnBlockOrJump(inst.TruePointer, cls);
            IndentLevel--;
            WriteLine("}");
            WriteLine("else");
            WriteLine("{");
            IndentLevel++;
            OutputReturnBlockOrJump(inst.FalsePointer, cls);
            IndentLevel--;
            WriteLine("}");
        }

        private void OutputBlock(SimanticsBlock block, CSTranslationClass cls)
        {
            for (int i=0; i<block.IBody.Count - 1; i++)
            {
                var inst = block.IBody[i];
                var body = cls.Instructions[inst.Index];
                switch (body.ReturnType)
                {
                    case PrimitiveReturnType.SimanticsTrue:
                    case PrimitiveReturnType.SimanticsTrueFalse: //truefalse result discarded, same destination
                        WriteLines(body.Body, true); break;
                    case PrimitiveReturnType.NativeStatementTrue:
                    case PrimitiveReturnType.NativeStatementTrueFalse: //truefalse result discarded, same destination
                        WriteLines(body.Body, false); break;
                    case PrimitiveReturnType.NativeExpressionTrueFalse:
                        //we have a problem... the expression needs to evaluate, but we don't do anything with the result.
                        //put it in the boolean temp and ignore it.
                        if (body.Body.Count == 1)
                            WriteLine($"_bResult = {body.Body.First()};");
                        else
                            WriteLines(body.Body, true);
                        break;
                    case PrimitiveReturnType.SimanticsSubroutine:
                        //a subroutine that yields but doesn't branch.
                    default:
                        throw new Exception("Non-statement in the middle of a sequence (detected areas with no branching)?");
                }
            }
            
            var lastInst = block.IBody[block.IBody.Count - 1];
            var lastBody = cls.Instructions[lastInst.Index];
            LastInstruction = block.LastInstructionIndex;
            
            string caseConst = null;
            if (block.BlockType == SimanticsBlockType.Switch 
                || block.BlockType == SimanticsBlockType.SwitchCase 
                || block.BlockType == SimanticsBlockType.SwitchLast)
            {
                caseConst = CSScopeMemory.GetConstant(Context, block.SwitchOperand.RhsOwner, block.SwitchOperand.RhsData);
            }

            switch (block.BlockType)
            {
                case SimanticsBlockType.InstructionSequence:
                case SimanticsBlockType.IfElse:
                    //just set the instruction based on the value and return back to the while loop
                    if (lastBody.ReturnType == PrimitiveReturnType.SimanticsSubroutine || lastInst.Yields)
                    {
                        UpdateInstruction();
                        if (lastBody.Body.Count > 1)
                        {
                            WriteLines(lastBody.Body, false);
                            WriteLine($"return _sResult;");
                        }
                        else
                        {
                            WriteLine($"return {lastBody.Body.First()};");
                        }
                        break;
                    }

                    switch (lastBody.ReturnType)
                    {
                        case PrimitiveReturnType.NativeStatementTrueFalse:
                            if (lastInst.Instruction.TruePointer == lastInst.Instruction.FalsePointer) goto case PrimitiveReturnType.NativeStatementTrue;
                            WriteLines(lastBody.Body, false);
                            WriteLine("if (_bResult) ");
                            OutputBinaryBranch(lastInst.Instruction, cls);
                            break;
                        case PrimitiveReturnType.NativeExpressionTrueFalse:
                            if (lastInst.Instruction.TruePointer == lastInst.Instruction.FalsePointer)
                            {
                                WriteLine($"_bResult = {lastBody.Body.First()}; //true and false do the same thing");
                                OutputReturnBlockOrJump(lastInst.Instruction.TruePointer, cls);
                            }
                            else
                            {
                                WriteLine($"if ({lastBody.Body.First()}) ");
                                OutputBinaryBranch(lastInst.Instruction, cls);
                            }
                            break;
                        case PrimitiveReturnType.SimanticsTrueFalse:
                            if (lastInst.Instruction.TruePointer == lastInst.Instruction.FalsePointer) goto case PrimitiveReturnType.SimanticsTrue;
                            WriteLine($"if ({lastBody.Body.First()} == VMPrimitiveExitCode.GOTO_TRUE) ");
                            OutputBinaryBranch(lastInst.Instruction, cls);
                            break;
                        case PrimitiveReturnType.SimanticsStatement:
                            if (lastInst.Instruction.TruePointer == lastInst.Instruction.FalsePointer)
                            {
                                WriteLines(lastBody.Body, false);
                                OutputReturnBlockOrJump(lastInst.Instruction.TruePointer, cls);
                            }
                            else
                            {
                                WriteLines(lastBody.Body, false);
                                WriteLine($"if (_sResult == VMPrimitiveExitCode.GOTO_TRUE) ");
                                OutputBinaryBranch(lastInst.Instruction, cls);
                            }
                            break;
                        case PrimitiveReturnType.NativeStatementTrue:
                            WriteLines(lastBody.Body, false);
                            OutputReturnBlockOrJump(lastInst.Instruction.TruePointer, cls);
                            break;
                        case PrimitiveReturnType.SimanticsTrue:
                            WriteLines(lastBody.Body, true);
                            OutputReturnBlockOrJump(lastInst.Instruction.TruePointer, cls);
                            break;
                    }

                    if (block.Parent == cls.Structure.RootBlock && cls.HasGlobalSwitch) WriteLine("break;");
                    break;

                case SimanticsBlockType.Switch:
                    string switchLhs = CSScopeMemory.GetExpression(Context, block.SwitchOperand.LhsOwner, block.SwitchOperand.LhsData, true);
                    WriteLine($"switch ({switchLhs}) ");
                    WriteLine("{");
                    IndentLevel++;
                    WriteLine($"case {caseConst}:");
                    IndentLevel++;
                    var set = new HashSet<string> { caseConst };
                    cls.SwitchPreviousCases[block] = set;
                    OutputReturnBlockOrJump(lastInst.Instruction.TruePointer, cls);
                    WriteLine($"break;");
                    IndentLevel--;
                    OutputReturnBlockOrJump(lastInst.Instruction.FalsePointer, cls);
                    break;
                case SimanticsBlockType.SwitchCase:
                    var cset = cls.SwitchPreviousCases[FindSwitchBlockStart(block)];

                    if (cset.Contains(caseConst))
                    {
                        WriteLine($"// case {caseConst} duplicated! skipped block at {lastInst.Instruction.FalsePointer}");
                    }
                    else
                    {
                        cset.Add(caseConst);
                        WriteLine($"case {caseConst}:");
                        IndentLevel++;
                        OutputReturnBlockOrJump(lastInst.Instruction.TruePointer, cls);
                        WriteLine($"break;");
                        IndentLevel--;
                    }
                    OutputReturnBlockOrJump(lastInst.Instruction.FalsePointer, cls);
                    break;
                case SimanticsBlockType.SwitchLast:
                    var lset = cls.SwitchPreviousCases[FindSwitchBlockStart(block)];

                    if (lset.Contains(caseConst))
                    {
                        WriteLine($"// case {caseConst} duplicated! skipped block at {lastInst.Instruction.FalsePointer}");
                    }
                    else
                    {
                        lset.Add(caseConst);
                        WriteLine($"case {caseConst}:");
                        IndentLevel++;
                        OutputReturnBlockOrJump(lastInst.Instruction.TruePointer, cls);
                        WriteLine($"break;");
                        IndentLevel--;
                    }
                    WriteLine($"default:");
                    IndentLevel++;
                    OutputReturnBlockOrJump(lastInst.Instruction.FalsePointer, cls);
                    WriteLine($"break;");
                    IndentLevel--;
                    IndentLevel--;
                    WriteLine("}");
                    if (FindSwitchBlockStart(block).Parent == cls.Structure.RootBlock && cls.HasGlobalSwitch) WriteLine("break;");
                    break;
                default:
                    throw new Exception($"Block type {block.BlockType.ToString()} not yet implemented.");
            }
            //our last instruction 
        }

        private SimanticsBlock FindSwitchBlockStart(SimanticsBlock end)
        {
            while (end.Parent != null)
            {
                if (end.BlockType == SimanticsBlockType.Switch) return end;
                end = end.Parent;
            }
            return null;
        }

        public void OutputClass(CSTranslationClass cls)
        {
            Context.CurrentClass = cls;
            Indent = GetIndentation(1);
            WriteLine($"public class {cls.ClassName} : {cls.Interface}");
            WriteLine("{");
            IndentLevel = 2;

            WriteLines(cls.PrimitiveDefinitions.Values.ToList(), false);
            if (cls.PrimitiveDefinitions.Count > 0) WriteLine();
            WriteLines(cls.OperandDefinitions.Values.ToList(), false);
            if (cls.OperandDefinitions.Count > 0) WriteLine();

            if (cls.InlineFunction)
            {
                WriteLine($"public override int ArgCount => {cls.ArgCount};");
                WriteLine();
            }

            WriteLine(cls.FunctionHead);
            WriteLine("{");
            IndentLevel = 3;

            WriteLine("bool _bResult;");
            WriteLine("VMPrimitiveExitCode _sResult;");
            if (cls.UseParams && !cls.InlineFunction) WriteLine("var args = context.Args;");

            if (cls.InlineFunction)
            {
                if (cls.UseLocals)
                {
                    WriteLine($"var locals = new short[{cls.Structure.Source.Locals}];");
                    WriteLine($"context.Locals = locals;");
                } else
                {
                    WriteLine($"context.Locals = new short[{cls.Structure.Source.Locals}];");
                }
            }
            else
            {
                if (cls.UseLocals)
                    WriteLine("var locals = context.Locals;");
            }
            if (cls.UseTemps) WriteLine("var temps = context.Thread.TempRegisters;");

            WriteLine();

            var root = cls.Structure.RootBlock;
            var topLevel = root.Body.Where(x => x.Parent == root);

            var useLoop = topLevel.Count() > 1 || topLevel.FirstOrDefault()?.FromBlocks.Count > 0;
            if (useLoop)
            {
                WriteLine("int _loops = 0;");
                WriteLine($"while (++_loops < {Context.MaxLoopCount})");
                WriteLine("{");
                IndentLevel = 4;
            }

            if (topLevel.Count() > 1)
            {
                cls.HasGlobalSwitch = true;
                WriteLine("switch (instruction)");
                WriteLine("{");

                IndentLevel++;
                foreach (var seq in topLevel)
                {
                    
                    WriteLine($"case {seq.StartInstructionIndex}:");
                    IndentLevel++;
                    OutputBlock(seq, cls);
                    IndentLevel--;
                }

                WriteLine("default:");
                IndentLevel++;
                WriteLine("throw new JITMissInstructionException(instruction);");
                IndentLevel--;

                IndentLevel--;
                WriteLine("}");
            }
            else if (topLevel.Count() == 1)
            {
                cls.HasGlobalSwitch = false;
                OutputBlock(topLevel.First(), cls);
            } else
            {
                cls.HasGlobalSwitch = false;
                WriteLine($"return {cls.TrueExp};");
            }

            if (useLoop)
            {
                IndentLevel = 3;
                WriteLine("}");
                WriteLine("throw new JITLoopException();");
            }

            IndentLevel = 2;
            WriteLine("}");

            IndentLevel = 1;
            WriteLine("}");

            WriteLine();
        }

        public string BuildCSFile()
        {
            Builder = new StringBuilder();

            Builder.AppendLine("// FreeSO SimAntics JIT Source Output");
            Builder.AppendLine("// Translator by @riperiperi on GitHub");
            Builder.AppendLine();

            foreach (var include in Context.Includes)
            {
                Builder.AppendLine(include);
            }
            Builder.AppendLine();

            Builder.AppendLine($"namespace {Context.NamespaceName}");
            Builder.AppendLine("{");
            IndentLevel = 1;

            WriteLine($"public class {Context.Filename}Module : SimAnticsModule");
            WriteLine("{");

            IndentLevel++;

            // write compilation info
            // first is hashes for the source files. If any of these change, this file should be recompiled.
            var myRes = (Context.ObjectRes ?? Context.SemiGlobalRes ?? Context.GlobalRes);
            WriteLine($"public override uint SourceHash => {myRes.MainIff.ExecutableHash};");
            if (Context.SemiGlobalRes != null) WriteLine($"public override uint SourceSemiglobalHash => {Context.SemiGlobalRes.MainIff.ExecutableHash};");
            if (Context.GlobalRes != null) WriteLine($"public override uint SourceGlobalHash => {Context.GlobalRes.MainIff.ExecutableHash};");

            // second is the JIT version - this should be incremented when anything in the JIT compiler is changed.
            WriteLine($"public override uint JITVersion => {JITVersion};");

            foreach (var cls in Context.AllClasses)
            {
                WriteLine($"public static {cls.Interface} _{cls.ClassName} = new {cls.ClassName}();");
            }
            IndentLevel--;

            WriteLine("}");

            Builder.AppendLine();

            foreach (var cls in Context.AllClasses)
                OutputClass(cls);

            Builder.AppendLine("}");
            return Builder.ToString();
        }

        private bool PropagateYieldFromCalls(StructuredBHAV bhav)
        {
            bhav.VisitCount++;
            if (bhav.Yields) return true;
            bool yields = false;
            foreach (var call in bhav.Calls)
            {
                SimAnticsModule module;
                IBHAVInfo ctx2;
                //find the bhav.
                if (call < 4096)
                {
                    //global
                    module = Context.GlobalModule;
                    ctx2 = Context.GlobalContext;
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
                    module = Context.SemiGlobalModule;
                    ctx2 = Context.SemiGlobalContext;
                }

                if (ctx2 != null)
                {
                    yields = ctx2.BHAVYields(call);
                }
                else if (module != null)
                {
                    if (module.FunctionYields(call) != false)
                    {
                        yields = true; //also true if we're missing the function
                        break;
                    }
                }
                else
                {
                    StructuredBHAV newBHAV;
                    if (Context.BHAVInfo.TryGetValue(call, out newBHAV))
                    {
                        if (newBHAV.VisitCount == 0)
                        {
                            if (PropagateYieldFromCalls(newBHAV))
                            {
                                yields = true;
                                break;
                            }
                        }
                        else
                        {
                            //already evaluated (or is being evaluated), take its word for it.
                            if (newBHAV.Yields)
                            {
                                yields = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        yields = true; //if we don't know if it yields, err on the side of caution.
                        break;
                    }
                }
            }
            bhav.Yields = yields;
            return yields;
        }

        public string TranslateIff(IffFile file)
        {
            Context.Filename = CSTranslationContext.FormatName(file.Filename.Replace(".iff", ""));
            Context.ModuleName = Context.Filename + "Module";
            Context.NamespaceName = "FSO.Scripts." + Context.Filename;
            Context.CurrentFile = file;

            var bhavs = file.List<BHAV>() ?? new List<BHAV>();
            foreach (var bhav in bhavs)
            {
                Context.CurrentBHAV = bhav;
                var sbhav = new StructuredBHAV(bhav);
                sbhav.Analyse(Context);
                Context.BHAVInfo[bhav.ChunkID] = sbhav;
            }

            foreach (var bhav in Context.BHAVInfo.Values)
                PropagateYieldFromCalls(bhav);

            foreach (var bhav in Context.BHAVInfo.Values) {
                foreach (var inst in bhav.Instructions.Values)
                {
                    (inst.Translator as CSSubroutinePrimitive)?.InitInfo(Context);
                }
            }

            foreach (var bhav in Context.BHAVInfo.Values)
                bhav.BuildStructure();

            foreach (var bhav in Context.BHAVInfo.Values)
            {
                Context.AllClasses.Add(TranslateBHAV(bhav));
            }

            return BuildCSFile();
        }
    }
}
