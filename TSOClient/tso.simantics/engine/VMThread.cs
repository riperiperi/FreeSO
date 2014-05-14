using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Content;
using TSO.Files.formats.iff.chunks;
using TSO.Simantics.primitives;

namespace TSO.Simantics.engine
{
    /// <summary>
    /// Handles instruction execution
    /// </summary>
    public class VMThread
    {
        private VMContext Context;
        private VMEntity Entity;
        private List<VMStackFrame> Stack;
        private bool ContinueExecution;
        public List<VMQueuedAction> Queue;
        public short[] TempRegisters = new short[20];
        public int[] TempXL = new int[2];
        public VMThreadState State;
        public VMPrimitiveExitCode LastStackExitCode = VMPrimitiveExitCode.GOTO_FALSE;

        public static VMPrimitiveExitCode EvaluateCheck(VMContext context, VMEntity entity, VMQueuedAction action)
        {
            var temp = new VMThread(context, entity, 5);
            temp.EnqueueAction(action);
            while (temp.Queue.Count > 0) //keep going till we're done! idling is for losers!
            {
                temp.Tick();
            }
            context.ThreadRemove(temp); //hopefully this thread should be completely dereferenced...
            return temp.LastStackExitCode;
        }

        public bool RunInMyStack(BHAV bhav, GameIffResource CodeOwner)
        {
            var prevFrame = Stack[Stack.Count - 1];
            var OldStack = Stack;
            var OldQueue = Queue;

            Stack = new List<VMStackFrame>() { prevFrame };
            Queue = new List<VMQueuedAction>() { Queue[0] };
            ExecuteSubRoutine(prevFrame, bhav, CodeOwner, new VMSubRoutineOperand(new short[] {-1, -1, -1, -1}));
            Stack.RemoveAt(0);
            if (Stack.Count == 0)
            {
                Stack = OldStack;
                Queue = OldQueue;
                return false;
                //bhav was invalid/empty
            }
            var frame = Stack[Stack.Count - 1];

            while (Stack.Count > 0)
            {
                NextInstruction();
            }

            //copy child stack things to parent stack

            prevFrame.Args = frame.Args;
            prevFrame.StackObject = frame.StackObject;
            Stack = OldStack;
            Queue = OldQueue;

            return (LastStackExitCode == VMPrimitiveExitCode.RETURN_TRUE) ? true : false;
        }

        public VMThread(VMContext context, VMEntity entity, int stackSize){
            this.Context = context;
            this.Entity = entity;

            this.Stack = new List<VMStackFrame>(stackSize);
            this.Queue = new List<VMQueuedAction>();

            Context.ThreadIdle(this);
        }

        public void Tick(){
            if (!Entity.Dead)
            {
                EvaluateQueuePriorities();
                if (Stack.Count == 0)
                {
                    if (Queue.Count == 0)
                    {
                        /** Idle **/
                        Context.ThreadIdle(this);
                        return;
                    }
                    var item = Queue[0];
                    ExecuteAction(item);
                }
                if (!Queue[0].Callee.Dead)
                {
                    ContinueExecution = true;
                    while (ContinueExecution)
                    {
                        ContinueExecution = false;
                        NextInstruction();
                    }
                }
                else //interaction owner is dead, rip
                {
                    Stack.Clear();
                    if (Queue[0].Callback != null) Queue[0].Callback.Run(Entity);
                    if (Queue.Count > 0) Queue.RemoveAt(0);
                }
            }
            else
            {
                Context.ThreadRemove(this); //thread owner is not alive, kill their thread
            }
        }

        private void EvaluateQueuePriorities() {
            if (Queue.Count == 0) return;
            int CurrentPriority = (int)Queue[0].Priority;
            for (int i = 1; i < Queue.Count; i++)
            {
                if ((int)Queue[i].Priority < CurrentPriority)
                {
                    Queue[0].Cancelled = true;
                    break;
                }
            }
        }

        private void NextInstruction()
        {
            if (Stack.Count == 0){
                return;
            }

            /** Next instruction **/
            var currentFrame = Stack.Last();

            if (currentFrame is VMPathFinder) HandleResult(currentFrame, null, ((VMPathFinder)currentFrame).Tick());
            else ExecuteInstruction(currentFrame);
        }

        public VMPathFinder PushNewPathFinder(VMStackFrame frame, List<VMFindLocationResult> locations)
        {
            var childFrame = new VMPathFinder
            {
                Routine = frame.Routine,
                Caller = frame.Caller,
                Callee = frame.Callee,
                CodeOwner = frame.CodeOwner,
                StackObject = frame.StackObject,
                Thread = this
            };

            var success = childFrame.InitRoutes(locations);

            if (!success) return null; //no route, don't push
            else
            {
                Stack.Add(childFrame);
                return childFrame;
            }
        }

        public void ExecuteSubRoutine(VMStackFrame frame, BHAV bhav, GameIffResource codeOwner, VMSubRoutineOperand args)
        {
            if (bhav == null){
                Pop(VMPrimitiveExitCode.ERROR);
                return;
            }

            var routine = frame.VM.Assemble(bhav);
            var childFrame = new VMStackFrame
            {
                Routine = routine,
                Caller = frame.Caller,
                Callee = frame.Callee,
                CodeOwner = codeOwner,
                StackObject = frame.StackObject
            };
            childFrame.Args = new short[4];
            for (var i = 0; i < childFrame.Args.Length; i++){
                var argValue = args.Arguments[i];
                if (argValue == -1)
                {
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
                NextInstruction();
                return;
            }
            

            var primitive = Context.GetPrimitive(opcode);
            if (primitive == null)
            {
                throw new Exception("Unknown primitive!");
                //HandleResult(frame, instruction, VMPrimitiveExitCode.GOTO_TRUE);
                //return;
                //Pop(VMPrimitiveExitCode.ERROR);
                
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
                    ContinueExecution = false;
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
                case VMPrimitiveExitCode.CONTINUE:
                    ContinueExecution = true;
                    break;
                case VMPrimitiveExitCode.INTERRUPT:
                    Stack.Clear();
                    if (Queue.Count > 0) Queue.RemoveAt(0);
                    LastStackExitCode = result;
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
            ContinueExecution = continueExecution;
            /*if (continueExecution)
            {
                NextInstruction();
            }*/
        }

        private void ExecuteAction(VMQueuedAction action){
            var frame = new VMStackFrame {
                Caller = Entity,
                Callee = action.Callee,
                CodeOwner = action.CodeOwner,
                Routine = action.Routine,
                StackObject = action.StackObject
            };
            if (action.Args == null) frame.Args = new short[4]; //always 4? i got crashes when i used the value provided by the routine, when for that same routine edith displayed 4 in the properties...
            else frame.Args = action.Args; //WARNING - if you use this, the args array MUST have the same number of elements the routine is expecting!
            

            Push(frame);
        }

        public void Pop(VMPrimitiveExitCode result){
            Stack.RemoveAt(Stack.Count - 1);
            LastStackExitCode = result;

            if (Stack.Count > 0)
            {
                if (result == VMPrimitiveExitCode.RETURN_TRUE)
                {
                    result = VMPrimitiveExitCode.GOTO_TRUE;
                }
                if (result == VMPrimitiveExitCode.RETURN_FALSE)
                {
                    result = VMPrimitiveExitCode.GOTO_FALSE;
                }

                var currentFrame = Stack.Last();
                HandleResult(currentFrame, currentFrame.GetCurrentInstruction(), result);
            }
            else //interaction finished!
            {
                if (Queue[0].Callback != null) Queue[0].Callback.Run(Entity);
                if (Queue.Count > 0) Queue.RemoveAt(0);
            }
        }

        public void Push(VMStackFrame frame)
        {
            if (frame.Routine.Instructions.Length == 0) return; //some bhavs are empty... do not execute these.
            Stack.Add(frame);

            /** Initialize the locals **/
            var numLocals = Math.Max(frame.Routine.Locals, frame.Routine.Arguments);
            frame.Locals = new ushort[numLocals];
            frame.Thread = this;

            /** Copy args in by default **/
            /**if (frame.Args != null)
            {
                for (var i = 0; i < frame.Args.Length; i++){
                    frame.Locals[i] = (ushort)frame.Args[i];
                }
            }**/

            frame.InstructionPointer = 0;
        }

        /// <summary>
        /// Add an item to the action queue
        /// </summary>
        /// <param name="invocation"></param>
        public void EnqueueAction(VMQueuedAction invocation)
        {
            if (Queue.Count == 0) //if empty, just queue right at the front (or end, if you're like that!)
            {
                this.Queue.Add(invocation);
                Context.ThreadActive(this);
            }
            else //we've got an even harder job! find a place for this interaction based on its priority
            {
                for (int i = Queue.Count - 1; i > 0; i--)
                {
                    if (invocation.Priority >= Queue[i].Priority) //if the next queue element we need to skip over is of the same or a higher priority we'll stay right here, otherwise skip over it!
                    {
                        this.Queue.Insert(i+1, invocation);
                        return;
                    }
                }
                this.Queue.Insert(1, invocation); //this is more important than all other queued items that are not running, so stick this to run next.
            }
            EvaluateQueuePriorities();
        }
    }

    public enum VMThreadState
    {
        Idle,
        Active,
        Removed
    }
}
