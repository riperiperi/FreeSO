using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.Engine.Primitives;
using FSO.SimAntics.Engine.Scopes;
using FSO.SimAntics.JIT.Translation.Model;
using FSO.SimAntics.JIT.Translation.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FSO.SimAntics.JIT.Translation
{
    public class StructuredBHAV
    {
        public BHAV Source;
        public Dictionary<byte, BlockAnalysisInstruction> Instructions;
        public SimanticsBlock RootBlock;

        public HashSet<ushort> Calls = new HashSet<ushort>();
        public HashSet<ushort> CalledBy = new HashSet<ushort>();
        public bool Yields = false;
        public int VisitCount = 0; //used for yield propagation

        public StructuredBHAV(BHAV bhav)
        {
            Source = bhav;
            Instructions = new Dictionary<byte, BlockAnalysisInstruction>();
        }

        private BlockAnalysisInstruction GetInstruction(TranslationContext ctx, byte index)
        {
            BlockAnalysisInstruction result;
            if (!Instructions.TryGetValue(index, out result))
            {
                result = new BlockAnalysisInstruction(ctx, Source.Instructions[index], index);
                Instructions[index] = result;
            }
            return result;
        }

        private void BuildExecutionGraph(TranslationContext ctx)
        {
            var toProcess = new Queue<Tuple<byte, byte>>();
            toProcess.Enqueue(new Tuple<byte, byte>(255, 0)); //function entry

            //build execution graph

            while (toProcess.Count > 0)
            {
                var process = toProcess.Dequeue();
                if (process.Item2 >= Source.Instructions.Length)
                {
                    var fromInst = GetInstruction(ctx, process.Item1);
                    fromInst.To.AddInst(null);
                    continue;
                }

                var toInst = GetInstruction(ctx, process.Item2);
                if (process.Item1 < 253)
                {
                    var fromInst = GetInstruction(ctx, process.Item1);
                    fromInst.To.AddInst(toInst);
                    toInst.From.Add(fromInst);
                } else
                {
                    toInst.From.Add(null); //likely from the start of the function. needed for loop detection
                }

                if (toInst.Yields || toInst.ReturnType == PrimitiveReturnType.SimanticsSubroutine) Yields = true;
                if (toInst.Translator.Primitive == SharedPrimitives.Subroutine)
                {
                    Calls.Add(toInst.Translator.Instruction.Opcode);
                }

                //follow true branch
                //if (toInst.Instruction.TruePointer >= Source.Instructions.Length) toInst.To.AddInst(null);
                if (toInst.VisitCount == 0) toProcess.Enqueue(new Tuple<byte, byte>(process.Item2, toInst.Instruction.TruePointer));

                if (toInst.SimpleReturnType != PrimitiveReturnType.SimanticsTrue)
                {
                    //follow false branch
                    //if (toInst.Instruction.FalsePointer >= Source.Instructions.Length) toInst.To.AddInst(null);
                    if (toInst.VisitCount == 0) toProcess.Enqueue(new Tuple<byte, byte>(process.Item2, toInst.Instruction.FalsePointer));
                }

                toInst.VisitCount = 1;
            }
        }

        private void ResetVisited()
        {
            foreach (var inst in Instructions)
            {
                inst.Value.VisitCount = 0;
            }

            if (RootBlock != null) ResetBlock(RootBlock);
        }

        private void ResetBlock(SimanticsBlock block)
        {
            block.VisitCount = 0;
            foreach (var sblock in block.Body)
            {
                ResetBlock(sblock);
            }
        }

        private void PrepareMainBlock()
        {
            RootBlock = new SimanticsBlock()
            {
                BlockType = SimanticsBlockType.BlockSequence,
                //IBody = Instructions.Values.ToList()
            };
        }

        private void IdentifySequences()
        {
            //O(n) scan through instructions
            //keep track of a "start" instruction, initially the first instruction.
            //and build a list of instructions following it.
            //whenever the new instruction has more than one input (or yields), complete the sequence (minus new) and start a new one on this instruction
            //whenever the new instruction has more than one output, complete the sequence (including new) and start on the following
            var firstSequence = new SimanticsBlock()
            {
                BlockType = SimanticsBlockType.InstructionSequence,
                Parent = RootBlock,
                StartInstructionIndex = 0,
            };
            var toProcess = new Queue<Tuple<byte, SimanticsBlock>>();
            toProcess.Enqueue(new Tuple<byte, SimanticsBlock>(0, firstSequence));
            Instructions[0].VisitCount = 1;
            while (toProcess.Count > 0)
            {
                var process = toProcess.Dequeue();
                var inst = Instructions[process.Item1];
                var currentSequence = process.Item2;
                if (currentSequence != null && (inst.From.Count > 1 || inst.Yields))
                {
                    //something jumps back in here, likely a loop or yield.
                    if (currentSequence.IBody.Count > 0)
                    {
                        RootBlock.Body.Add(currentSequence);
                        currentSequence.LastInstructionIndex = currentSequence.IBody.Last().Index;
                        currentSequence = null;
                        /*
                        currentSequence = new SimanticsBlock()
                        {
                            BlockType = SimanticsBlockType.InstructionSequence,
                            Parent = RootBlock,
                            StartInstructionIndex = inst.Index,
                        };
                        */
                    }
                }

                if (currentSequence == null)
                {
                    currentSequence = new SimanticsBlock()
                    {
                        BlockType = SimanticsBlockType.InstructionSequence,
                        Parent = RootBlock,
                        StartInstructionIndex = inst.Index,
                    };
                }
                currentSequence.IBody.Add(inst);
                var nextSequence = currentSequence;
                if (inst.To.Count > 1 || inst.ReturnType == PrimitiveReturnType.SimanticsSubroutine)
                {
                    RootBlock.Body.Add(currentSequence);
                    currentSequence.LastInstructionIndex = currentSequence.IBody.Last().Index;
                    nextSequence = null;
                }

                var hadDest = false;
                foreach (var dest in inst.To)
                {
                    if (dest != null && dest.VisitCount == 0)
                    {
                        toProcess.Enqueue(new Tuple<byte, SimanticsBlock>(dest.Index, nextSequence));
                        dest.VisitCount++;
                        hadDest = true;
                    }
                }
                if (!hadDest && nextSequence != null)
                {
                    RootBlock.Body.Add(currentSequence);
                    currentSequence.LastInstructionIndex = currentSequence.IBody.Last().Index;
                }
            }

            foreach (var sequence in RootBlock.Body)
            {
                var firstInst = sequence.IBody.First();
                var lastInst = sequence.IBody.Last();

                sequence.FromBlocks = RootBlock.Body.Where(x => firstInst.From.Contains(x.IBody.Last())).ToList();

                sequence.NextBlocks = lastInst.To.Select(x => (x == null) ? null : RootBlock.Body.First(y => y.StartInstructionIndex == x.Index)).ToList();
            }
        }

        /*private SimanticsBlock VerifyLoop(SimanticsBlock firstLoop)
        {
            //loop structuring - there should NOT 
        }*/

        private void IdentifyLoops()
        {
            //any block that eventually jumps back to itself
            //and no jumps into the middle
            //is eligible being a loop

            ResetVisited();
            //if else obviously starts with out of 2
            //when an if-else is entered, incoming should ALWAYS be 1 or 2 (for loop)
            //it ends whenever the incoming is broken, or when

            var toProcess = new Queue<SimanticsBlock>();
            toProcess.Enqueue(RootBlock.Body.First());
            while (toProcess.Count > 0)
            {
                var block = toProcess.Dequeue();
                if (block.FromBlocks.Count > 1)
                {

                }
            }
        }

        private void IdentifySwitch()
        {
            //specific detection:
            //mulltiple 1 instruction sequences:
            // - instruction is Expression, Equals,
            // - sequence of instructions have same left hand side, "constant" right (literals only for now, but soon constants will also work).
            //branches followed until incoming > 1

            ResetVisited();

            //current, switchParent
            var toProcess = new Queue<Tuple<SimanticsBlock, SimanticsBlock>>();
            toProcess.Enqueue(new Tuple<SimanticsBlock, SimanticsBlock>(RootBlock.Body.First(), null));
            while (toProcess.Count > 0)
            {
                var process = toProcess.Dequeue();
                var block = process.Item1;
                var switchParent = process.Item2;
                var nullAdvance = true;

                if (switchParent == null)
                {
                    if (block.BlockType == SimanticsBlockType.IfElse)
                    {
                        var branch = block.IBody.Last();
                        var exp = branch.Translator;
                        if (exp.Primitive == SharedPrimitives.Expression)
                        {
                            var expOperand = exp.GetOperand<VMExpressionOperand>();
                            if (expOperand.Operator == VMExpressionOperator.Equals
                                && (expOperand.RhsOwner == VMVariableScope.Literal || expOperand.RhsOwner == VMVariableScope.Tuning))
                            {
                                //begin switch statement advancing.
                                //switch advances down false - though we can search true for nested switch statements too.
                                block.SwitchOperand = expOperand;
                                var t = block.NextBlocks[0];
                                var f = block.NextBlocks[1];
                                if (t != null && t.VisitCount == 0)
                                {
                                    toProcess.Enqueue(new Tuple<SimanticsBlock, SimanticsBlock>(t, null)); t.VisitCount++;
                                }
                                if (f != null && f.VisitCount == 0)
                                {
                                    toProcess.Enqueue(new Tuple<SimanticsBlock, SimanticsBlock>(f, block)); f.VisitCount++;
                                }
                                nullAdvance = false;
                            }
                        }
                    }
                }
                else
                {
                    //we've started looking for a switch statement. 
                    //cases should be one expression with the same lhs as the first, equals test, and a constant rhs.
                    if (block.BlockType == SimanticsBlockType.IfElse && block.IBody.Count == 1 && block.Parent != RootBlock)
                    {
                        //can we continue?
                        var branch = block.IBody.Last();
                        var exp = branch.Translator;
                        if (exp.Primitive == SharedPrimitives.Expression)
                        {
                            var expOperand = exp.GetOperand<VMExpressionOperand>();
                            if (expOperand.Operator == VMExpressionOperator.Equals
                                && (expOperand.RhsOwner == VMVariableScope.Literal || expOperand.RhsOwner == VMVariableScope.Tuning)
                                && expOperand.LhsOwner == switchParent.SwitchOperand.LhsOwner
                                && expOperand.LhsData == switchParent.SwitchOperand.LhsData)
                            {
                                //yes.
                                nullAdvance = false;
                                if (switchParent.BlockType != SimanticsBlockType.Switch)
                                {
                                    //get started, more than one case means we're good to make this a switch.
                                    switchParent.BlockType = SimanticsBlockType.Switch;
                                }
                                block.SwitchOperand = expOperand;
                                if (block.Parent.BlockType == SimanticsBlockType.SwitchLast) block.Parent.BlockType = SimanticsBlockType.SwitchCase;
                                block.BlockType = SimanticsBlockType.SwitchLast;

                                var t = block.NextBlocks[0];
                                var f = block.NextBlocks[1];
                                if (t != null && t.VisitCount == 0)
                                {
                                    toProcess.Enqueue(new Tuple<SimanticsBlock, SimanticsBlock>(t, null)); t.VisitCount++;
                                }
                                if (f != null && f.VisitCount == 0)
                                {
                                    toProcess.Enqueue(new Tuple<SimanticsBlock, SimanticsBlock>(f, switchParent)); f.VisitCount++;
                                }
                                nullAdvance = false;
                            }
                        }
                    }

                    if (nullAdvance)
                    {
                        //default branch
                        if (switchParent.BlockType == SimanticsBlockType.Switch)
                        {
                            //we established a switch with more than one case.
                            //we've come out of the last false branch into another block, presumably a default branch.
                            //try scanning from this block again for a new switch.
                            
                        }
                        toProcess.Enqueue(new Tuple<SimanticsBlock, SimanticsBlock>(block, null));
                        continue;
                    }
                }

                if (nullAdvance)
                {
                    var t = block.NextBlocks[0];
                    var f = (block.NextBlocks.Count > 1) ? block.NextBlocks[1] : null;
                    if (t != null && t.VisitCount == 0)
                    {
                        toProcess.Enqueue(new Tuple<SimanticsBlock, SimanticsBlock>(t, null)); t.VisitCount++;
                    }
                    if (f != null && f.VisitCount == 0)
                    {
                        toProcess.Enqueue(new Tuple<SimanticsBlock, SimanticsBlock>(f, null)); f.VisitCount++;
                    }
                }
            }
        }

        private void IdentifyIfElse()
        {
            ResetVisited();
            //if-else starts on any block with more than one exit and ends when one of our branches branches into the other. 

            var branchDepths = new List<byte>();

            //from block, to block
            var toProcess = new Queue<SimanticsBlock>();
            toProcess.Enqueue(RootBlock.Body.First());
            RootBlock.Body.First().VisitCount = 1;
            while (toProcess.Count > 0)
            {
                var process = toProcess.Dequeue();
                var block = process;

                var safeToBranch = true;
                foreach (var next in block.NextBlocks)
                {
                    if (next == null) continue;
                    if (next.VisitCount > 0)//next.BlockType == SimanticsBlockType.IfElse)
                    {
                        //---------we actually rejoin an if-else block...
                        //we rejoin a block that has already been processed...
                        //this means that both us and the target must not be part of an if/else structure.
                        //this doesn't make any sense for a tree structure, so we need to mess with flow control.

                        //case 1: top level attempts to jump into if/else:
                        //         - dissolve if/else into top level sequences
                        //case 2: if/else attempts to jump into if/else:
                        //        AND they are not our parent
                        //         - dissolve both if/else, as we require non-standard jumps
                        //        IF they are our parent
                        //         - loop between us and parent.
                        //         - (how the fuck)
                        //case 3: ???

                        next.BlockType = SimanticsBlockType.InstructionSequence;
                        next.Parent = RootBlock;
                        block.Parent = RootBlock;
                        safeToBranch = false;
                    }
                    if (next == block)
                    {
                        //actually a loop! this cannot be an if/else
                        //TODO: make a do { } while() loop
                        safeToBranch = false;
                    }
                }
                var lastInst = block.IBody.Last();
                if (safeToBranch && block.NextBlocks.Count(x => x == null || !x.IBody.First().Yields) > 1 
                    && !(lastInst.Yields || lastInst.ReturnType == PrimitiveReturnType.SimanticsSubroutine))
                {
                    block.BlockType = SimanticsBlockType.IfElse;
                    foreach (var next in block.NextBlocks)
                    {
                        if (next == null) continue;
                        next.Parent = block;
                        if (next.VisitCount == 0)
                        {
                            toProcess.Enqueue(next);
                            next.VisitCount++;
                        }
                    }
                } else
                {
                    foreach (var next in block.NextBlocks)
                    {
                        if (next == null) continue;
                        if (next.VisitCount == 0)
                        {
                            toProcess.Enqueue(next);
                            next.VisitCount++;
                        }
                    }
                }
            }
        }

        public void Analyse(TranslationContext ctx)
        {
            if (Source.Instructions.Length > 0)
            {
                var first = GetInstruction(ctx, 0);
                BuildExecutionGraph(ctx);
                ResetVisited();
            }
        }

        public void BuildStructure()
        {
            //now we need to scan through for structures.
            //priority order:
            //sequences
            //loops
            //switch statements
            //if/else
            
            PrepareMainBlock();
            if (Instructions.Count == 0) return;
            IdentifySequences();
            IdentifyIfElse();
            IdentifySwitch();
            //IdentifyLoops();
        }
    }

    public class BlockAnalysisInstruction
    {
        public BHAVInstruction Instruction;
        public AbstractTranslationPrimitive Translator;
        public int VisitCount;

        public byte Index => Translator.Index;
        public bool Yields => Translator.CanYield;
        public PrimitiveReturnType ReturnType => Translator.ReturnType;
        public PrimitiveReturnType SimpleReturnType
        {
            get
            {
                switch (ReturnType)
                {
                    case PrimitiveReturnType.NativeStatementTrue:
                    case PrimitiveReturnType.SimanticsStatement:
                        return PrimitiveReturnType.SimanticsTrue;
                    case PrimitiveReturnType.NativeExpressionTrueFalse:
                    case PrimitiveReturnType.NativeStatementTrueFalse:
                    case PrimitiveReturnType.SimanticsSubroutine:
                        return PrimitiveReturnType.SimanticsTrueFalse;

                    default:
                        return ReturnType;
                }
            }
        }

        //links
        //hashset that allows multiple nulls?
        public ToInstructionSet To = new ToInstructionSet();
        public HashSet<BlockAnalysisInstruction> From = new HashSet<BlockAnalysisInstruction>();

        public BlockAnalysisInstruction(TranslationContext ctx, BHAVInstruction instruction, byte index)
        {
            Translator = ctx.Primitives.GetPrimitive(instruction, index);
            Instruction = instruction;
        }

        public override string ToString()
        {
            return Translator.Primitive.ToString();
        }
    }

    public class ToInstructionSet : List<BlockAnalysisInstruction>
    {
        public void AddInst(BlockAnalysisInstruction inst)
        {
            if (inst != null && Contains(inst)) return;
            Add(inst);
        }
    }

    public class SimanticsBlock
    {
        public int VisitCount;
        public SimanticsBlockType BlockType;
        public VMExpressionOperand SwitchOperand; //only used for switch

        public List<BlockAnalysisInstruction> IBody = new List<BlockAnalysisInstruction>(); //for sequence blocks
        public List<SimanticsBlock> Body = new List<SimanticsBlock>(); //branch sides
        public SimanticsBlock Parent;

        public byte StartInstructionIndex;
        public byte LastInstructionIndex;

        public List<SimanticsBlock> FromBlocks;
        public List<SimanticsBlock> NextBlocks; //[trueBranch, falseBranch], with null for return.

        public override string ToString()
        {
            return BlockType.ToString() + " (" + StartInstructionIndex + "-" + LastInstructionIndex + ")";
        }
    }

    public enum SimanticsBlockType
    {
        BlockSequence,
        InstructionSequence,
        IfElse, //split into if/else branch at end of sequence
        Switch,
        SwitchCase,
        SwitchLast, //false goes to default
        LoopBefore, //while (condition)
        LoopAfter //do { } while (condition)
    }
}
