//#define THROW_SIMANTICS
#define IDE_COMPAT

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
using FSO.SimAntics.Marshals.Threads;
using FSO.SimAntics.Model.TSOPlatform;
using FSO.SimAntics.NetPlay.EODs.Model;
using System.Threading;

namespace FSO.SimAntics.Engine
{
    /// <summary>
    /// Handles instruction execution
    /// </summary>
    public class VMThread
    {
        public static int MAX_USER_ACTIONS = 20;

        public VMContext Context;
        private VMEntity Entity;

        public VMThreadBreakMode ThreadBreak = VMThreadBreakMode.Active;
        public int BreakFrame; //frame the last breakpoint was performed on
        public bool RoutineDirty;

        //check tree only vars
        public bool IsCheck;
        public List<VMPieMenuInteraction> ActionStrings;

        public List<VMStackFrame> Stack;
        private bool ContinueExecution;

        public List<VMQueuedAction> Queue;
        /// <summary>
        /// Set when a change to the queue or an item's priority is changed. Internal functions set this, but since you can modify the queue 
        /// from other classes MAKE SURE you set this when such a change is made. (eg. priority set from VMAvatar)
        /// </summary>
        public bool QueueDirty;

        public byte ActiveQueueBlock = 0; //cannot reorder items in the queue with index <= this.
        public short[] TempRegisters = new short[20];
        public int[] TempXL = new int[2];
        public VMPrimitiveExitCode LastStackExitCode = VMPrimitiveExitCode.GOTO_FALSE;

        public VMAsyncState BlockingState;
        public VMEODPluginThreadState EODConnection;
        public bool Interrupt;

        private ushort ActionUID;

        // Exception handling variables
        // Don't need to be serialized.
        public int DialogCooldown = 0;
        // the number of ticks that have executed so far this frame. If this exceeds the allowed max,
        // the thread resets, and a SimAntics Error pops up.
        public int TicksThisFrame = 0;
        // the maximum number of primitives a thread can execute in one frame. Tweak appropriately.

        // variables for internal scheduler
        public uint ScheduleIdleStart; // keep track of tick when we started idling for an object. must be synced!
        public uint ScheduleIdleEnd;

        public static readonly int MAX_LOOP_COUNT = 500000;

        public static VMPrimitiveExitCode EvaluateCheck(VMContext context, VMEntity entity, VMStackFrame initFrame)
        {
            return EvaluateCheck(context, entity, initFrame, null, null);
        }

        public static VMPrimitiveExitCode EvaluateCheck(VMContext context, VMEntity entity, VMStackFrame initFrame, VMQueuedAction action)
        {
            return EvaluateCheck(context, entity, initFrame, action, null);
        }

        public static VMPrimitiveExitCode EvaluateCheck(VMContext context, VMEntity entity, VMStackFrame initFrame, VMQueuedAction action, List<VMPieMenuInteraction> actionStrings)
        {
            var temp = new VMThread(context, entity, 5);
            var forceClone = !context.VM.Scheduler.RunningNow;
            //temps should only persist on check trees running within the vm tick to avoid desyncs.
            if (entity.Thread != null)
            {
                temp.TempRegisters = forceClone?(short[])entity.Thread.TempRegisters.Clone() : entity.Thread.TempRegisters;
                temp.TempXL = forceClone ? (int[])entity.Thread.TempXL.Clone() : entity.Thread.TempXL;
            }
            temp.IsCheck = true;
            temp.ActionStrings = actionStrings; //generate and place action strings in here
            temp.Push(initFrame);
            if (action != null) temp.Queue.Add(action); //this check runs an action. We may need its interaction number, etc.
            while (temp.Stack.Count > 0 && temp.DialogCooldown == 0) //keep going till we're done! idling is for losers!
            {
                temp.Tick();
                temp.ThreadBreak = VMThreadBreakMode.Active; //cannot breakpoint in check trees
            }
            return (temp.DialogCooldown > 0) ? VMPrimitiveExitCode.RETURN_FALSE : temp.LastStackExitCode;
        }

        public bool RunInMyStack(BHAV bhav, GameObject CodeOwner, short[] passVars, VMEntity stackObj)
        {
            //a little bit hacky. We may not need to do as serious a context switch as this.
            var OldStack = Stack;
            var OldQueue = Queue;
            var OldCheck = IsCheck;
            var OldQueueBlock = ActiveQueueBlock;

            VMStackFrame prevFrame = new VMStackFrame() { Caller = Entity, Callee = Entity };
            if (Stack.Count > 0)
            {
                prevFrame = Stack[Stack.Count - 1];
                Stack = new List<VMStackFrame>() { prevFrame };
            }
            else
            {
                Stack = new List<VMStackFrame>();
            }

            Queue = new List<VMQueuedAction>();
            if (Queue.Count > 0) Queue.Add(Queue[0]);
            IsCheck = true;

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

            try
            {
                while (Stack.Count > 0)
                {
                    NextInstruction();
                }
            }
            catch (Exception e)
            {
                if (e is ThreadAbortException) throw e;
                //we need to catch these so that the parent can be restored.
            }

            //copy child stack things to parent stack
            Stack = OldStack;
            Queue = OldQueue;
            IsCheck = OldCheck;
            ActiveQueueBlock = OldQueueBlock;

            return (LastStackExitCode == VMPrimitiveExitCode.RETURN_TRUE) ? true : false;
        }

        public VMThread(VMContext context, VMEntity entity, int stackSize)
        {
            this.Context = context;
            this.Entity = entity;

            this.Stack = new List<VMStackFrame>(stackSize);
            this.Queue = new List<VMQueuedAction>();
        }

        /// <summary>
        /// Checks to see if it can push an interaction, and pushes it.
        /// Returns true on success, false on failure.
        /// </summary>
        public bool AttemptPush()
        {
            QueueDirty = true;
            while (Queue.Count > 0)
            {
                var item = Queue[0];
                if (item.Cancelled) Entity.SetFlag(VMEntityFlags.InteractionCanceled, true);
                if (IsCheck || ((item.Mode != VMQueueMode.ParentIdle || !Entity.GetFlag(VMEntityFlags.InteractionCanceled)) && CheckAction(item) != null))
                {
                    ExecuteAction(item);
                    return true;
                }
                else
                {
                    Queue.RemoveAt(0); //keep going.
                }
            }
            return false;
        }

        public void TryRunImmediately()
        {
            //check if we have a run immediately interaction, and inject it if we do.
            var ind = Queue.FindIndex(x => (x.Flags & TTABFlags.RunImmediately) > 0);
            if ((ind > ActiveQueueBlock || (!(Stack.Count > 0 && Stack.LastOrDefault().ActionTree) && ind > -1)))
            {
                //not already running (if no action we are still not running if we're queue[0], so go for it)
                //swap current item with ind.
                var temp = Queue[ind];
                Queue.RemoveAt(ind);
                Queue.Insert(0, temp);
                var frame = temp.ToStackFrame(Entity);
                frame.DiscardResult = true;
                Push(frame);
                ActiveQueueBlock++; //both the run immediately interaction and the active interaction must be protected.
            }
        }

        public void Tick(){
#if IDE_COMPAT
            if (ThreadBreak == VMThreadBreakMode.Pause) return;
            else if (ThreadBreak == VMThreadBreakMode.Immediate)
            {
                Breakpoint(Stack.LastOrDefault()); return;
            }
            if (RoutineDirty)
            {
                foreach (var frame in Stack)
                    if (frame.Routine.Chunk.RuntimeVer != frame.Routine.RuntimeVer) frame.Routine = Context.VM.Assemble(frame.Routine.Chunk);
                RoutineDirty = false;
            }
#endif

            if (BlockingState != null) BlockingState.WaitTime++;
            if (DialogCooldown > 0) DialogCooldown--;
#if !THROW_SIMANTICS
            try
            {
#endif
                if (!Entity.Dead)
                {
                    if (QueueDirty)
                    {
                        EvaluateQueuePriorities();
                        TryRunImmediately();
                        QueueDirty = false;
                    }
                    if (Stack.Count == 0)
                    {
                        if (IsCheck) return; //running out of execution means check trees have ended.
                        Entity.ExecuteEntryPoint(1, Context, false);
                        if (Stack.Count == 0) return;
                    }
                    if ((!Stack.LastOrDefault().ActionTree) || (!Queue[0].Callee.Dead)) //main or our target is not dead
                    {
#if IDE_COMPAT
                        if (ThreadBreak == VMThreadBreakMode.ReturnTrue)
                        {
                            var bf = Stack[BreakFrame];
                            HandleResult(bf, bf.GetCurrentInstruction(), VMPrimitiveExitCode.RETURN_TRUE);
                            Breakpoint(Stack.LastOrDefault());
                            return;
                        }
                        if (ThreadBreak == VMThreadBreakMode.ReturnFalse)
                        {
                            var bf = Stack[BreakFrame];
                            HandleResult(bf, bf.GetCurrentInstruction(), VMPrimitiveExitCode.RETURN_TRUE);
                            Breakpoint(Stack.LastOrDefault());
                            return;
                        }
#endif
                        ContinueExecution = true;
                        while (ContinueExecution)
                        {
                            if (TicksThisFrame++ > MAX_LOOP_COUNT) throw new Exception("Thread entered infinite loop! ( >" + MAX_LOOP_COUNT + " primitives)");
                            ContinueExecution = false;
                            NextInstruction();
                        }
                    }
                    else //interaction owner is dead, rip
                    {
                        Entity.Reset(Context);
                    }
                }

#if !THROW_SIMANTICS
            }
            catch (Exception e)
            {
                if (e is ThreadAbortException) throw e;
                if (Stack.Count == 0) return;
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
                        Operand = new VMDialogOperand { },
                        Message = exceptionStr,
                        Title = "SimAntics Exception!"
                    };
                    Context.VM.SignalDialog(info);
                    DialogCooldown = 30 * 20;
                }

                if (!IsCheck)
                {
                    context.Callee.Reset(context.VM.Context);
                    context.Caller.Reset(context.VM.Context);
                    if (Delete) Entity.Delete(true, context.VM.Context);
                } else
                {
                    Stack.Clear();
                }
            }
#endif
        }

        private void EvaluateQueuePriorities()
        {
            if (Queue.Count == 0) return;
            int CurrentPriority = (int)Queue[0].Priority;
            for (int i = ActiveQueueBlock + 1; i < Queue.Count; i++)
            {
                if (Queue[i].Callee == null || Queue[i].Callee.Dead)
                {
                    Queue.RemoveAt(i--); //remove interactions to dead objects (not within active queue block)
                    continue;
                }
                if ((int)Queue[i].Priority > CurrentPriority)
                {
                    Queue[0].Cancelled = true;
                    Entity.SetFlag(VMEntityFlags.InteractionCanceled, true);
                }
            }
        }

        private void NextInstruction()
        {
            /** Next instruction **/
            var currentFrame = Stack.LastOrDefault();
            if (currentFrame == null) return;

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
                CallFailureTrees = failureTrees,
                ActionTree = frame.ActionTree
            };

            Stack.Add(childFrame);
            return childFrame;
        }

        public void ExecuteSubRoutine(VMStackFrame frame, BHAV bhav, GameObject codeOwner, VMSubRoutineOperand args)
        {
            if (bhav == null)
            {
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
                StackObject = frame.StackObject,
                ActionTree = frame.ActionTree
            };
            childFrame.Args = new short[(routine.Arguments > 4) ? routine.Arguments : 4];
            for (var i = 0; i < childFrame.Args.Length; i++)
            {
                short argValue = (i > 3) ? (short)-1 : args.Arguments[i];
                if (argValue == -1 && args.UseTemp0)
                {
                    argValue = TempRegisters[i];
                }
                childFrame.Args[i] = argValue;
            }
            Push(childFrame);
        }

        private void ExecuteInstruction(VMStackFrame frame)
        {
            var instruction = frame.GetCurrentInstruction();
            var opcode = instruction.Opcode;

            if (opcode >= 256)
            {
                BHAV bhav = null;

                GameObject CodeOwner;
                if (opcode >= 8192)
                {
                    // Semi-Global sub-routine call
                    bhav = frame.ScopeResource.SemiGlobal.Get<BHAV>(opcode);
                }
                else if (opcode >= 4096)
                {
                    // Private sub-routine call
                    bhav = frame.ScopeResource.Get<BHAV>(opcode);
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
#if IDE_COMPAT
                if (Stack.LastOrDefault().GetCurrentInstruction().Breakpoint || ThreadBreak == VMThreadBreakMode.StepIn)
                {
                    Breakpoint(frame);
                    ContinueExecution = false;
                } else
#endif
                {
                    ContinueExecution = true;
                }

                return;
            }


            var primitive = Context.Primitives[opcode];
            if (primitive == null)
            {
                HandleResult(frame, instruction, VMPrimitiveExitCode.GOTO_TRUE);
                return;
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
                    ScheduleIdleStart = Context.VM.Scheduler.CurrentTickID;
                    Context.VM.Scheduler.ScheduleTickIn(Entity, 1);
                    ContinueExecution = false;
                    break;
                case VMPrimitiveExitCode.CONTINUE_FUTURE_TICK:
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
                    ScheduleIdleStart = Context.VM.Scheduler.CurrentTickID;
                    Context.VM.Scheduler.ScheduleTickIn(Entity, 1);
                    ContinueExecution = false;
                    break;
                case VMPrimitiveExitCode.GOTO_FALSE_NEXT_TICK:
                    MoveToInstruction(frame, instruction.FalsePointer, false);
                    ScheduleIdleStart = Context.VM.Scheduler.CurrentTickID;
                    Context.VM.Scheduler.ScheduleTickIn(Entity, 1);
                    ContinueExecution = false;
                    break;
                case VMPrimitiveExitCode.CONTINUE:
                    ContinueExecution = true;
                    break;
                case VMPrimitiveExitCode.INTERRUPT:
                    Stack.Clear();
                    QueueDirty = true;
                    if (Queue.Count > 0) Queue.RemoveAt(0);
                    LastStackExitCode = result;
                    break;
            }
        }

        private void MoveToInstruction(VMStackFrame frame, byte instruction, bool continueExecution)
        {
            if (frame is VMRoutingFrame)
            {
                //TODO: Handle returning false into the pathfinder (indicates failure)
                return;
            }

            switch (instruction)
            {
                case 255:
                    Pop(VMPrimitiveExitCode.RETURN_FALSE);
                    break;
                case 254:
                    Pop(VMPrimitiveExitCode.RETURN_TRUE); break;
                case 253:
                    //attempt to continue along only path available. 
                    if (frame.GetCurrentInstruction().TruePointer != 253)
                    {
                        MoveToInstruction(frame, frame.GetCurrentInstruction().TruePointer, continueExecution); return;
                    }
                    else if (frame.GetCurrentInstruction().FalsePointer != 253)
                    {
                        MoveToInstruction(frame, frame.GetCurrentInstruction().FalsePointer, continueExecution); return;
                    }
                    Pop(VMPrimitiveExitCode.ERROR); break;
                default:
                    frame.InstructionPointer = instruction;
                    if (frame.GetCurrentInstruction().Breakpoint ||
                        (ThreadBreak != VMThreadBreakMode.Active && (
                            ThreadBreak == VMThreadBreakMode.StepIn ||
                            (ThreadBreak == VMThreadBreakMode.StepOver && Stack.Count - 1 <= BreakFrame) ||
                            (ThreadBreak == VMThreadBreakMode.StepOut && Stack.Count <= BreakFrame)
                        )))
                    {
                        Breakpoint(frame);
                    }
                    break;
            }

            ContinueExecution = (ThreadBreak != VMThreadBreakMode.Pause) && continueExecution;
        }

        public void Breakpoint(VMStackFrame frame)
        {
            if (IsCheck) return; //can't breakpoint in check trees.
            ThreadBreak = VMThreadBreakMode.Pause;
            BreakFrame = Stack.IndexOf(frame);
            Context.VM.BreakpointHit(Entity);
        }

        public void Pop(VMPrimitiveExitCode result)
        {
            var discardResult = Stack[Stack.Count - 1].DiscardResult;
            var contextSwitch = (Stack.Count > 1) && Stack.LastOrDefault().ActionTree != Stack[Stack.Count - 2].ActionTree;
            if (contextSwitch && !Stack.LastOrDefault().ActionTree) { }
            Stack.RemoveAt(Stack.Count - 1);
            LastStackExitCode = result;

            if (contextSwitch) //interaction switching back to main (it cannot be the other way...)
            {
                QueueDirty = true;
                var interaction = Queue[0];
                //clear "interaction cancelled" since we are leaving the interaction
                if (interaction.Mode != VMQueueMode.ParentIdle) Entity.SetFlag(VMEntityFlags.InteractionCanceled, false);
                if (interaction.Callback != null) interaction.Callback.Run(Entity);
                if (Queue.Count > 0) Queue.RemoveAt(0);
                ContinueExecution = true; //continue where the Allow Push idle left off
                result = VMPrimitiveExitCode.CONTINUE;
            }
            if (Stack.Count > 0)
            {
                if (discardResult)
                {
                    //TODO: merge this functionality with contextSwitch. Will have to change how Allow Push works.
                    //only used by Run Immediately currently.
                    QueueDirty = true;
                    if (Queue.Count > 0) Queue.RemoveAt(0);
                    ActiveQueueBlock--;
                    result = VMPrimitiveExitCode.CONTINUE;
                }
                else if (result == VMPrimitiveExitCode.RETURN_TRUE)
                    result = VMPrimitiveExitCode.GOTO_TRUE;
                else if (result == VMPrimitiveExitCode.RETURN_FALSE)
                    result = VMPrimitiveExitCode.GOTO_FALSE;
                var currentFrame = Stack.Last();
                HandleResult(currentFrame, currentFrame.GetCurrentInstruction(), result);
            }
            else // :(
            {
                ContinueExecution = false;
            }
        }

        public void Push(VMStackFrame frame)
        {
            if (frame.Routine.Instructions.Length == 0) return; //some bhavs are empty... do not execute these.
            Stack.Add(frame);

            /** Initialize the locals **/
            var numLocals = Math.Max(frame.Routine.Locals, frame.Routine.Arguments);
            frame.Locals = new short[numLocals];
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
            QueueDirty = true;
            if (!IsCheck && (invocation.Flags & TTABFlags.RunImmediately) > 0)
            {
                // shove this action in the queue to try run it next tick.
                // interaction can be run normally if we actually hit it using allow push. (unlikely)
                // otherwise next tick will detect the "run immediately" interaction's presence. 
                // and it will be pushed to the stack immediately.
                // TODO: check if any interactions of this kind clobber the temps.
                // this doesnt ""run immediately"", but is good enough.

                invocation.Mode = VMQueueMode.Idle; //hide
                invocation.Priority = short.MinValue;
                this.Queue.Add(invocation);
                return;
            }

            if (Queue.Count == 0) //if empty, just queue right at the front 
                this.Queue.Add(invocation);
            else if ((invocation.Flags & TTABFlags.FSOPushTail) > 0 && invocation.Mode != VMQueueMode.ParentExit)
            {
                //this one's weird. start at the left til there's a lower priority. (eg. parent exit or idle)
                bool hitParentEnd = (invocation.Mode != VMQueueMode.ParentIdle);
                for (int i = ActiveQueueBlock+1; i < Queue.Count; i++)
                {
                    if (Queue[i].Priority < invocation.Priority) //we have higher priority than this item
                    {
                        this.Queue.Insert(i, invocation); //insert before this. will come before parent exit and pals.
                        EvaluateQueuePriorities();
                        return;
                    }
                }
                this.Queue.Add(invocation); //right at the end! (somehow)
            }
            else if ((invocation.Flags & TTABFlags.Leapfrog) > 0)
                //place right after active interaction, ignoring all priorities.
                this.Queue.Insert(ActiveQueueBlock + 1, invocation);
            else //we've got an even harder job! find a place for this interaction based on its priority
            {
                bool hitParentEnd = (invocation.Mode != VMQueueMode.ParentIdle);
                for (int i = Queue.Count - 1; i > ActiveQueueBlock; i--)
                {
                    if (hitParentEnd && (invocation.Priority <= Queue[i].Priority || Queue[i].Mode == VMQueueMode.ParentExit)) //skip until we find a parent exit or something with the same or higher priority.
                    {
                        this.Queue.Insert(i + 1, invocation);
                        EvaluateQueuePriorities();
                        return;
                    }
                    if (Queue[i].Mode == VMQueueMode.ParentExit) hitParentEnd = true;
                }
                this.Queue.Insert(ActiveQueueBlock + 1, invocation); //this is more important than all other queued items that are not running, so stick this to run next.
            }
            EvaluateQueuePriorities();
        }

        public void CancelAction(ushort actionUID)
        {
            var interaction = Queue.FirstOrDefault(x => x.UID == actionUID);
            if (interaction != null)
            {
                if (Entity is VMAvatar && interaction == Queue[0] && Context.VM.EODHost != null) Context.VM.EODHost.ForceDisconnect((VMAvatar)Entity);
                QueueDirty = true;
                interaction.Cancelled = true;
                //cancel any idle parents after this interaction
                var index = Queue.IndexOf(interaction);

                if (interaction.Mode == Engine.VMQueueMode.ParentIdle)
                {
                    for (int i = index + 1; i < Queue.Count; i++)
                    {
                        if (Queue[i].Mode == Engine.VMQueueMode.ParentIdle)
                        {
                            if (interaction.Mode == Engine.VMQueueMode.ParentIdle) Queue.RemoveAt(i--);
                            else
                            {
                                Queue[i].Cancelled = true;
                                Queue[i].Priority = 0;
                            }
                        }
                        else if (Queue[i].Mode == Engine.VMQueueMode.ParentExit)
                        {
                            Queue[i].Cancelled = true;
                            Queue[i].Priority = 0;
                        }
                        //parent exit needs to "appear" like it is cancelled.
                    }
                }

                if ((index > ActiveQueueBlock || Stack.LastOrDefault()?.ActionTree == false) && interaction.Mode == Engine.VMQueueMode.Normal)
                {
                    Queue.Remove(interaction);
                }
                else
                {
                    Entity.SetFlag(VMEntityFlags.InteractionCanceled, true);
                    interaction.Priority = 0;
                }
            }
        }

        private void ExecuteAction(VMQueuedAction action)
        {
            var frame = action.ToStackFrame(Entity);
            Push(frame);
        }

        public List<VMPieMenuInteraction> CheckAction(VMQueuedAction action)
        {
            // 1. check action flags for permissions (if we are avatar)
            // 2. run check tree

            // rules:
            // Dogs/Cats means people CANNOT use these interactions. (DogsFlag|CatsFlag & IsDog|IsCat)

            // When Allow Object Owner is OFF it disallows the object owner, otherwise no effect
            // Visitors, Roommates, Ghosts have this same negative effect.
            // Friends apprars to override Owner, Visitors, Roommates

            // Allow CSRs:positive effect.

            if (action == null) return null;
            var result = new List<VMPieMenuInteraction>();

            if (((action.Flags & TTABFlags.MustRun) == 0) && Entity is VMAvatar) //just let everyone use the CSR interactions
            {
                var avatar = (VMAvatar)Entity;

                if (avatar.GetSlot(0) != null && (action.Flags & TTABFlags.TSOAvailableCarrying) == 0) return null;

                if ((action.Flags & (TTABFlags.AllowCats | TTABFlags.AllowDogs)) > 0)
                {
                    //interaction can only be performed by cats or dogs
                    if (!avatar.IsPet) return null;
                    //check we're the correct type
                    if (avatar.IsCat && (action.Flags & TTABFlags.AllowCats) == 0) return null;
                    if (avatar.IsDog && (action.Flags & TTABFlags.AllowDogs) == 0) return null;
                }
                else if (avatar.IsPet) return null; //not allowed

                if ((action.Flags & TTABFlags.TSOIsRepair) > 0) return null;

                TSOFlags tsoState =
                    ((!(action.Callee is VMGameObject) || avatar.PersistID == ((VMTSOObjectState)action.Callee.TSOState).OwnerID)
                    ? TSOFlags.AllowObjectOwner : 0)
                    | ((((VMTSOAvatarState)avatar.TSOState).Permissions == VMTSOAvatarPermissions.Visitor) ? TSOFlags.AllowVisitors : 0)
                    | ((((VMTSOAvatarState)avatar.TSOState).Permissions >= VMTSOAvatarPermissions.Roommate) ? TSOFlags.AllowRoommates : 0)
                    | ((((VMTSOAvatarState)avatar.TSOState).Permissions == VMTSOAvatarPermissions.Admin) ? TSOFlags.AllowCSRs : 0)
                    | ((avatar.GetPersonData(VMPersonDataVariable.IsGhost) > 0) ? TSOFlags.AllowGhost : 0)
                    | TSOFlags.AllowFriends;
                TSOFlags tsoCompare = action.Flags2;
                //if flags are empty apart from "Non-Empty", force everything but visitor. (a kind of default state)
                if (tsoCompare == TSOFlags.NonEmpty) tsoCompare |= TSOFlags.AllowFriends | TSOFlags.AllowRoommates | TSOFlags.AllowObjectOwner;

                //DEBUG: enable debug interction for all CSRs.
                if ((action.Flags & TTABFlags.Debug) > 0)
                {
                    if ((tsoState & TSOFlags.AllowCSRs) > 0)
                        return result; //do not bother running check
                    else
                        return null; //disable debug for everyone else.
                }

                if ((action.Flags & TTABFlags.TSOAvailableWhenDead) > 0) tsoCompare |= TSOFlags.AllowGhost;
                if ((action.Flags & TTABFlags.AllowVisitors) > 0) tsoCompare |= TSOFlags.AllowVisitors; //wrong???????

                var posMask = (TSOFlags.AllowObjectOwner);
                if (((tsoState & posMask) & (tsoCompare & posMask)) == 0)
                {
                    //NEGATIVE EFFECTS:
                    var negMask = (TSOFlags.AllowVisitors | TSOFlags.AllowRoommates | TSOFlags.AllowGhost);

                    var negatedFlags = (~tsoCompare) & negMask;
                    if ((negatedFlags & tsoState) > 0) return null; //we are disallowed
                    if ((tsoCompare & TSOFlags.AllowCSRs) > 0 && (tsoState & TSOFlags.AllowCSRs) == 0) return null; // only admins can run csr.
                }
            }
            if (((action.Flags & TTABFlags.MustRun) == 0 || ((action.Flags & TTABFlags.TSORunCheckAlways) > 0))
                && action.CheckRoutine != null && EvaluateCheck(Context, Entity, new VMStackFrame()
                {
                    Caller = Entity,
                    Callee = action.Callee,
                    CodeOwner = action.CodeOwner,
                    StackObject = action.StackObject,
                    Routine = action.CheckRoutine,
                    Args = new short[4]
                }, null, result) != VMPrimitiveExitCode.RETURN_TRUE)
                return null;

            return result;
        }

        #region VM Marshalling Functions
        public virtual VMThreadMarshal Save()
        {
            var stack = new VMStackFrameMarshal[Stack.Count];
            int i = 0;
            foreach (var item in Stack) stack[i++] = item.Save();

            var queue = new VMQueuedActionMarshal[Queue.Count];
            i = 0;
            foreach (var item in Queue) queue[i++] = item.Save();

            return new VMThreadMarshal
            {
                Stack = stack,
                Queue = queue,
                ActiveQueueBlock = ActiveQueueBlock,
                TempRegisters = TempRegisters,
                TempXL = TempXL,
                LastStackExitCode = LastStackExitCode,

                BlockingState = BlockingState,
                EODConnection = EODConnection,

                Interrupt = Interrupt,

                ActionUID = ActionUID,
                DialogCooldown = DialogCooldown,
                ScheduleIdleStart = ScheduleIdleStart
            };
        }

        public virtual void Load(VMThreadMarshal input, VMContext context)
        {
            Stack = new List<VMStackFrame>();
            foreach (var item in input.Stack)
            {
                Stack.Add((item is VMRoutingFrameMarshal) ? new VMRoutingFrame(item, context, this) : new VMStackFrame(item, context, this));
            }
            Queue = new List<VMQueuedAction>();
            QueueDirty = true;
            foreach (var item in input.Queue) Queue.Add(new VMQueuedAction(item, context));
            ActiveQueueBlock = input.ActiveQueueBlock;
            TempRegisters = input.TempRegisters;
            TempXL = input.TempXL;
            LastStackExitCode = input.LastStackExitCode;

            BlockingState = input.BlockingState;
            EODConnection = input.EODConnection;
            Interrupt = input.Interrupt;
            ActionUID = input.ActionUID;
            DialogCooldown = input.DialogCooldown;
            ScheduleIdleStart = input.ScheduleIdleStart;
        }

        public VMThread(VMThreadMarshal input, VMContext context, VMEntity entity)
        {
            Context = context;
            Entity = entity;
            Load(input, context);
        }
        #endregion
    }

    public enum VMThreadBreakMode
    {
        Active = 0,
        Pause = 1,
        StepIn = 2,
        StepOut = 3,
        StepOver = 4,
        ReturnTrue = 5,
        ReturnFalse = 6,
        Immediate = 7
    }
}