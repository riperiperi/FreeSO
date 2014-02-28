using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.content;
using tso.files.formats.iff.chunks;
using tso.simantics.primitives;

namespace tso.simantics.engine
{
    /// <summary>
    /// Handles instruction execution
    /// </summary>
    public class VMThread
    {
        private VMContext Context;
        private VMEntity Entity;
        private List<VMStackFrame> Stack;
        private List<VMQueuedAction> Queue;
        public short[] TempRegisters = new short[20];
        public VMThreadState State;

        public VMThread(VMContext context, VMEntity entity, int stackSize){
            this.Context = context;
            this.Entity = entity;

            this.Stack = new List<VMStackFrame>(stackSize);
            this.Queue = new List<VMQueuedAction>();

            Context.ThreadIdle(this);
        }

        public void Tick(){
            if (Stack.Count == 0){
                if (Queue.Count == 0) {
                    /** Idle **/
                    Context.ThreadIdle(this);
                    return;
                }

                var item = Queue[0];
                Queue.RemoveAt(0);
                ExecuteAction(item);
            }
            NextInstruction();
        }

        private void NextInstruction(){
            if (Stack.Count == 0){
                return;
            }

            /** Next instruction **/
            var currentFrame = Stack.Last();
            ExecuteInstruction(currentFrame);
        }

        private void ExecuteSubRoutine(VMStackFrame frame, BHAV bhav, GameIffResource codeOwner, VMSubRoutineOperand args)
        {
            if (bhav == null){
                Pop(VMPrimitiveExitCode.ERROR);
                return;
            }
            System.Diagnostics.Debug.WriteLine("Invoke: " + bhav.ChunkLabel);
            System.Diagnostics.Debug.WriteLine("");

            var routine = frame.VM.Assemble(bhav);
            var childFrame = new VMStackFrame
            {
                Routine = routine,
                Caller = frame.Caller,
                Callee = frame.Callee,
                CodeOwner = codeOwner,
                StackObject = frame.StackObject
            };
            childFrame.Args = new short[routine.Arguments];
            for (var i = 0; i < childFrame.Args.Length; i++){
                var argValue = args.Arguments[i];
                if (argValue == -1)
                {
                    /** TODO: Is this the right rule? Maybe a flag decides when to copy from temp? **/
                    argValue = TempRegisters[i];
                }
                childFrame.Args[i] = argValue;
            }
            Push(childFrame);
        }

        private void ExecuteInstruction(VMStackFrame frame){
            var instruction = frame.GetCurrentInstruction();
            var opcode = instruction.Opcode;

            if (opcode >= 256)
            {
                BHAV bhav = null;

                GameIffResource CodeOwner;
                if (opcode >= 8192)
                {
                    CodeOwner = frame.Callee.SemiGlobal.Resource;
                    bhav = frame.Callee.SemiGlobal.Resource.Get<BHAV>(opcode);
                }
                else if (opcode >= 4096)
                {
                    /** Private sub-routine call **/
                    CodeOwner = frame.CalleePrivate;
                    bhav = frame.CalleePrivate.Get<BHAV>(opcode);
                }
                else
                {
                    /** Global sub-routine call **/
                    CodeOwner = frame.Global.Resource;
                    bhav = frame.Global.Resource.Get<BHAV>(opcode);
                }

                var operand = frame.GetCurrentOperand<VMSubRoutineOperand>();
                ExecuteSubRoutine(frame, bhav, CodeOwner, operand);
                return;
            }
            

            var primitive = Context.GetPrimitive(opcode);
            if (primitive == null)
            {
                throw new Exception("Unknown primitive!");
                //Pop(VMPrimitiveExitCode.ERROR);
                //return;
            }

            VMPrimitiveHandler handler = primitive.GetHandler();
            var result = handler.Execute(frame);
            HandleResult(frame, instruction, result);
        }

        private void HandleResult(VMStackFrame frame, VMInstruction instruction, VMPrimitiveExitCode result)
        {
            switch (result)
            {
                /** Dont advance the instruction pointer, this primitive isnt finished yet **/
                case VMPrimitiveExitCode.CONTINUE_NEXT_TICK:
                    break;
                case VMPrimitiveExitCode.ERROR:
                    Pop(result);
                    break;
                case VMPrimitiveExitCode.RETURN_TRUE:
                case VMPrimitiveExitCode.RETURN_FALSE:
                    /** pop stack and return false **/
                    Pop(result);
                    break;
                case VMPrimitiveExitCode.GOTO_TRUE:
                    MoveToInstruction(frame, instruction.TruePointer, true);
                    break;
                case VMPrimitiveExitCode.GOTO_FALSE:
                    MoveToInstruction(frame, instruction.FalsePointer, true);
                    break;
                case VMPrimitiveExitCode.GOTO_TRUE_NEXT_TICK:
                    MoveToInstruction(frame, instruction.TruePointer, false);
                    break;
                case VMPrimitiveExitCode.GOTO_FALSE_NEXT_TICK:
                    MoveToInstruction(frame, instruction.FalsePointer, false);
                    break;
            }
        }

        private void MoveToInstruction(VMStackFrame frame, byte instruction, bool continueExecution){
            if (instruction == 254){
                Pop(VMPrimitiveExitCode.RETURN_TRUE);
            }
            else if (instruction == 255)
            {
                Pop(VMPrimitiveExitCode.RETURN_FALSE);
            }
            else if (instruction == 253)
            {
                Pop(VMPrimitiveExitCode.ERROR);
            }
            else
            {
                frame.InstructionPointer = instruction;
            }
            if (continueExecution)
            {
                NextInstruction();
            }
        }

        private void ExecuteAction(VMQueuedAction action){
            var frame = new VMStackFrame {
                Caller = Entity,
                Callee = action.Callee,
                CodeOwner = action.CodeOwner,
                Routine = action.Routine,
                StackObject = action.StackObject
            };
            frame.Args = new short[action.Routine.Arguments];

            Push(frame);
        }

        private void Pop(VMPrimitiveExitCode result){
            Stack.RemoveAt(Stack.Count - 1);

            if (Stack.Count > 0){
                if (result == VMPrimitiveExitCode.RETURN_TRUE){
                    result = VMPrimitiveExitCode.GOTO_TRUE;
                }
                if (result == VMPrimitiveExitCode.RETURN_FALSE){
                    result = VMPrimitiveExitCode.GOTO_FALSE;
                }

                var currentFrame = Stack.Last();
                HandleResult(currentFrame, currentFrame.GetCurrentInstruction(), result);
            }
        }

        private void Push(VMStackFrame frame)
        {
            Stack.Add(frame);

            /** Initialize the locals **/
            var numLocals = Math.Max(frame.Routine.Locals, frame.Routine.Arguments);
            frame.Locals = new ushort[numLocals];
            frame.Thread = this;

            /** Copy args in by default **/
            if (frame.Args != null)
            {
                for (var i = 0; i < frame.Args.Length; i++){
                    frame.Locals[i] = (ushort)frame.Args[i];
                }
            }

            frame.InstructionPointer = 0;
        }

        /// <summary>
        /// Add an item to the action queue
        /// </summary>
        /// <param name="invocation"></param>
        public void EnqueueAction(VMQueuedAction invocation)
        {
            this.Queue.Add(invocation);
            Context.ThreadActive(this);
        }
    }

    public enum VMThreadState
    {
        Idle,
        Active
    }
}
