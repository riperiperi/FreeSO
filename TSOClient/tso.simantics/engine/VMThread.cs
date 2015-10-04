/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Content;
using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.Primitives;
using FSO.SimAntics.Model;

namespace FSO.SimAntics.Engine
{
    /// <summary>
    /// Handles instruction execution
    /// </summary>
    public class VMThread
    {
        public VMContext Context;
        private VMEntity Entity;

        public int DialogCooldown = 0;

        public List<VMStackFrame> Stack;
        private bool ContinueExecution;
        public List<VMQueuedAction> Queue;
        public short[] TempRegisters = new short[20];
        public int[] TempXL = new int[2];
        public bool IsCheck;
        public VMPrimitiveExitCode LastStackExitCode = VMPrimitiveExitCode.GOTO_FALSE;

        public bool Interrupt;

        private ushort ActionUID;

        public static VMPrimitiveExitCode EvaluateCheck(VMContext context, VMEntity entity, VMQueuedAction action)
        {
            var temp = new VMThread(context, entity, 5);
            temp.IsCheck = true;
            temp.EnqueueAction(action);
            while (temp.Queue.Count > 0) //keep going till we're done! idling is for losers!
            {
                temp.Tick();
            }
            return (temp.DialogCooldown > 0) ? VMPrimitiveExitCode.ERROR:temp.LastStackExitCode;
        }

        public bool RunInMyStack(BHAV bhav, GameIffResource CodeOwner, short[] passVars, VMEntity stackObj)
        {
            var OldStack = Stack;
            var OldQueue = Queue;
            VMStackFrame prevFrame = new VMStackFrame() { Caller = Entity, Callee = Entity };
            if (Stack.Count > 0)
            {
                prevFrame = Stack[Stack.Count - 1];
                Stack = new List<VMStackFrame>() { prevFrame };
            } else
            {
                Stack = new List<VMStackFrame>();
            }

            if (Queue.Count > 0)
            {
                Queue = new List<VMQueuedAction>() { Queue[0] };
            } else
            {
                Queue = new List<VMQueuedAction>();
            }
            
            ExecuteSubRoutine(prevFrame, bhav, CodeOwner, new VMSubRoutineOperand(passVars));
            Stack.RemoveAt(0);
            if (Stack.Count == 0)
            {
                Stack = OldStack;
                Queue = OldQueue;
                return false;
                //bhav was invalid/empty
            }
            var frame = Stack[Stack.Count - 1];
            frame.StackObject = stackObj;

            while (Stack.Count > 0)
            {
                NextInstruction();
            }

            //copy child stack things to parent stack

            //prevFrame.Args = frame.Args;
            //prevFrame.StackObject = frame.StackObject;
            Stack = OldStack;
            Queue = OldQueue;

            return (LastStackExitCode == VMPrimitiveExitCode.RETURN_TRUE) ? true : false;
        }

        public VMThread(VMContext context, VMEntity entity, int stackSize){
            this.Context = context;
            this.Entity = entity;

            this.Stack = new List<VMStackFrame>(stackSize);
            this.Queue = new List<VMQueuedAction>();
        }

        public void Tick(){
            if (DialogCooldown > 0) DialogCooldown--;
#if !DEBUG
            try {
#endif
            if (!Entity.Dead)
            {
                EvaluateQueuePriorities();
                if (Stack.Count == 0)
                {
                    if (Queue.Count == 0)
                    {
                        //todo: should restart main
                        return;
                    }
                    var item = Queue[0];
                    if (!IsCheck && item.Priority != VMQueuePriority.ParentIdle) Entity.SetFlag(VMEntityFlags.InteractionCanceled, false);
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
                Queue.Clear();
            }

#if !DEBUG
            } catch (Exception e) {
                var context = Stack[Stack.Count - 1];
                bool Delete = ((Entity is VMGameObject) && (DialogCooldown > 30 * 20 - 10));
                if (DialogCooldown == 0)
                {
                    
                    var simExcept = new VMSimanticsException(e.Message, context);
                    string exceptionStr = "A SimAntics Exception has occurred, and has been suppressed: \r\n\r\n" + simExcept.ToString() + "\r\n\r\nThe object will be reset. Please report this!";
                    VMDialogInfo info = new VMDialogInfo
                    {
                        Caller = null,
                        Icon = context.Callee,
                        Operand = new VMDialogStringsOperand { },
                        Message = exceptionStr,
                        Title = "SimAntics Exception!"
                    };
                    Context.VM.SignalDialog(info);
                    DialogCooldown = 30 * 20;
                }

                context.Callee.Reset(context.VM.Context);
                context.Caller.Reset(context.VM.Context);

                if (Delete) Entity.Delete(true, context.VM.Context);
            }
#endif
            Interrupt = false;
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

            if (currentFrame is VMRoutingFrame) HandleResult(currentFrame, null, ((VMRoutingFrame)currentFrame).Tick());
            else ExecuteInstruction(currentFrame);
        }

        public VMRoutingFrame PushNewRoutingFrame(VMStackFrame frame, bool failureTrees)
        {
            var childFrame = new VMRoutingFrame
            {
                Routine = frame.Routine,
                Caller = frame.Caller,
                Callee = frame.Callee,
                CodeOwner = frame.CodeOwner,
                StackObject = frame.StackObject,
                Thread = this,
                CallFailureTrees = failureTrees
            };

            Stack.Add(childFrame);
            return childFrame;
        }

        public void ExecuteSubRoutine(VMStackFrame frame, BHAV bhav, GameIffResource codeOwner, VMSubRoutineOperand args)
        {
            if (bhav == null){
                Pop(VMPrimitiveExitCode.ERROR);
                return;
            }

            var routine = Context.VM.Assemble(bhav);
            var childFrame = new VMStackFrame
            {
                Routine = routine,
                Caller = frame.Caller,
                Callee = frame.Callee,
                CodeOwner = codeOwner,
                StackObject = frame.StackObject
            };
            childFrame.Args = new short[(routine.Arguments>4)?routine.Arguments:4];
            for (var i = 0; i < childFrame.Args.Length; i++){
                short argValue = (i>3)?(short)-1:args.Arguments[i];
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
                    // Semi-Global sub-routine call
                    bhav = frame.CodeOwner.SemiGlobal.Get<BHAV>(opcode);
                }
                else if (opcode >= 4096)
                {
                    // Private sub-routine call
                    bhav = frame.CodeOwner.Get<BHAV>(opcode);
                }
                else
                {
                    // Global sub-routine call
                    //CodeOwner = frame.Global.Resource;
                    bhav = frame.Global.Resource.Get<BHAV>(opcode);
                }

                CodeOwner = frame.CodeOwner;

                var operand = (VMSubRoutineOperand)instruction.Operand;
                ExecuteSubRoutine(frame, bhav, CodeOwner, operand);
                NextInstruction();
                return;
            }
            

            var primitive = Context.Primitives[opcode];
            if (primitive == null)
            {
                //throw new Exception("Unknown primitive!");
                HandleResult(frame, instruction, VMPrimitiveExitCode.GOTO_TRUE);
                return;
                //Pop(VMPrimitiveExitCode.ERROR);
                
            }

            VMPrimitiveHandler handler = primitive.GetHandler();
            var result = handler.Execute(frame, instruction.Operand);
            HandleResult(frame, instruction, result);
        }

        private void HandleResult(VMStackFrame frame, VMInstruction instruction, VMPrimitiveExitCode result)
        {
            switch (result)
            {
                // Don't advance the instruction pointer, this primitive isnt finished yet
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
            if (frame is VMRoutingFrame)
            {
                //TODO: Handle returning false into the pathfinder (indicates failure)
                return;
            }

            switch (instruction) {
                case 255: Pop(VMPrimitiveExitCode.RETURN_FALSE); break;
                case 254: Pop(VMPrimitiveExitCode.RETURN_TRUE); break;
                case 253: Pop(VMPrimitiveExitCode.ERROR); break;
                default:
                    frame.InstructionPointer = instruction; break;
            }
            ContinueExecution = continueExecution;
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

            frame.InstructionPointer = 0;
        }

        /// <summary>
        /// Add an item to the action queue
        /// </summary>
        /// <param name="invocation"></param>
        public void EnqueueAction(VMQueuedAction invocation)
        {
            invocation.UID = ActionUID++;
            if (Queue.Count == 0) //if empty, just queue right at the front (or end, if you're like that!)
            {
                this.Queue.Add(invocation);
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
}
