//#define THROW_SIMANTICS
#if !Server
    #define IDE_COMPAT
#endif

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
        public string ThreadBreakString;
        public int BreakFrame; //frame the last breakpoint was performed on
        public bool RoutineDirty;

        //check tree only vars
        public bool IsCheck;
        public List<VMPieMenuInteraction> ActionStrings;
        public Dictionary<int, short> MotiveAdChanges;

        public List<VMStackFrame> Stack;
        private bool ContinueExecution;

        public List<VMQueuedAction> Queue;
        public VMQueuedAction ActiveAction
        {
            get
            {
                return (ActiveQueueBlock>-1)?Queue[ActiveQueueBlock]:null;
            }
        }
        /// <summary>
        /// Set when a change to the queue or an item's priority is changed. Internal functions set this, but since you can modify the queue 
        /// from other classes MAKE SURE you set this when such a change is made. (eg. priority set from VMAvatar)
        /// </summary>
        public bool QueueDirty;

        public sbyte ActiveQueueBlock = -1; //cannot reorder items in the queue with index <= this.
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
            if (action != null)
            {
                temp.Queue.Add(action); //this check runs an action. We may need its interaction number, etc.
                temp.ActiveQueueBlock = 0;
            }
            while (temp.Stack.Count > 0 && temp.DialogCooldown == 0 && !temp.Entity.Dead) //keep going till we're done! idling is for losers!
            {
                temp.Tick();
                temp.ThreadBreak = VMThreadBreakMode.Active; //cannot breakpoint in check trees
            }
            if (actionStrings != null && actionStrings.Count == 0)
            {
                //add an action string containing any modified ads
                actionStrings.Add(new VMPieMenuInteraction()
                {
                    MotiveAdChanges = temp.MotiveAdChanges
                });
            }
            if (context.VM.Aborting) return VMPrimitiveExitCode.ERROR;
            return (temp.DialogCooldown > 0) ? VMPrimitiveExitCode.RETURN_FALSE : temp.LastStackExitCode;
        }

        public bool RunInMyStack(VMRoutine routine, GameObject CodeOwner, short[] passVars, VMEntity stackObj)
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

            ExecuteSubRoutine(prevFrame, routine, CodeOwner, new VMSubRoutineOperand(passVars));
            Stack.RemoveAt(0);
            if (Stack.Count == 0)
            {
                Stack = OldStack;
                Queue = OldQueue;
                return false;
                //bhav was invalid/empty
            }
            var frame = Stack[Stack.Count - 1];
            frame.Callee = stackObj;
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
            int priorityCompare = ((VMAvatar)Entity).GetPersonData(VMPersonDataVariable.Priority);
            if (priorityCompare <= 2) priorityCompare -= 1; //autonomous interactions fall through to other ones
            QueueDirty = true;
            while (Queue.Count > ActiveQueueBlock+1)
            {
                var item = Queue[ActiveQueueBlock+1];
                if (item.Priority <= priorityCompare) return false;
                if (item.NotifyIdle) Entity.SetFlag(VMEntityFlags.InteractionCanceled, item.NotifyIdle);
                if (IsCheck || ((item.Mode != VMQueueMode.ParentIdle || !Entity.GetFlag(VMEntityFlags.InteractionCanceled)) && CheckAction(item) != null))
                {
                    Entity.SetFlag(VMEntityFlags.InteractionCanceled, false);
                    if (!ExecuteAction(item)) return false;
                    ActiveQueueBlock++;
                    return true;
                }
                else
                {
                    Queue.RemoveAt(ActiveQueueBlock + 1); //keep going.
                }
            }
            return false;
        }

        public void TryRunImmediately()
        {
            //check if we have a run immediately interaction, and inject it if we do.#
            while (true)
            {
                var ind = Queue.FindIndex(x => (x.Flags & TTABFlags.RunImmediately) > 0);
                if ((ind > ActiveQueueBlock || (!(Stack.Count > 0 && Stack.LastOrDefault().ActionTree) && ind > -1)))
                {
                    //not already running (if no action we are still not running if we're queue[0], so go for it)
                    //swap current item with ind.
                    var temp = Queue[ind];
                    Queue.RemoveAt(ind);
                    if (CheckAction(temp) != null)
                    {
                        Queue.Insert(ActiveQueueBlock+1, temp);
                        var frame = temp.ToStackFrame(Entity);
                        frame.DiscardResult = true;
                        Push(frame);
                        ActiveQueueBlock++; //both the run immediately interaction and the active interaction must be protected.
                        break;
                    }
                } else
                {
                    break;
                }
            }
        }

        private void EndCurrentInteraction()
        {
            QueueDirty = true;
            var interaction = Queue[ActiveQueueBlock];
            //clear "interaction cancelled" since we are leaving the interaction
            if (interaction.Mode != VMQueueMode.ParentIdle) Entity.SetFlag(VMEntityFlags.InteractionCanceled, false);
            if (interaction.Callback != null) interaction.Callback.Run(Entity);
            if (Queue.Count > 0) Queue.RemoveAt(ActiveQueueBlock);
            if (Entity is VMAvatar && !IsCheck && ActiveQueueBlock == 0)
            {
                //some things are reset when an interaction ends
                //motive deltas reset between interactions
                ((VMAvatar)Entity).SetPersonData(VMPersonDataVariable.NonInterruptable, 0); //verified in ts1
                ((VMAvatar)Entity).ClearMotiveChanges();
            }
            ContinueExecution = true; //continue where the Allow Push idle left off
            ActiveQueueBlock--;
            //update priority with the priority of the interaction we are going back to (or 0)
            ((VMAvatar)Entity).SetPersonData(VMPersonDataVariable.Priority, (ActiveQueueBlock > -1) ? Queue[ActiveQueueBlock].Priority : (short)0);
            EvaluateQueuePriorities();
        }

        public void AbortCurrentInteraction()
        {
            //go all the way back to the stack frame that Allow Push'd us.
            var returnTo = Stack.FindLast(x => x.DiscardResult);
            if (returnTo != null)
            {
                var ind = Stack.IndexOf(returnTo);
                while (Stack.Count > ind)
                {
                    Stack.RemoveAt(Stack.Count-1);
                }
                EndCurrentInteraction();
            }
        }

        public void Tick(){
#if IDE_COMPAT
            if (ThreadBreak == VMThreadBreakMode.Pause) return;
            else if (ThreadBreak == VMThreadBreakMode.Reset)
            {
                Entity.Reset(Context);
                ThreadBreak = VMThreadBreakMode.Active;
            }
            else if (ThreadBreak == VMThreadBreakMode.Immediate)
            {
                Breakpoint(Stack.LastOrDefault(), "Paused."); return;
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
                            Breakpoint(Stack.LastOrDefault(), "Returned True.");
                            return;
                        }
                        if (ThreadBreak == VMThreadBreakMode.ReturnFalse)
                        {
                            var bf = Stack[BreakFrame];
                            HandleResult(bf, bf.GetCurrentInstruction(), VMPrimitiveExitCode.RETURN_TRUE);
                            Breakpoint(Stack.LastOrDefault(), "Returned False.");
                            return;
                        }
#endif
                        ContinueExecution = true;
                        while (ContinueExecution)
                        {
                            if (TicksThisFrame++ > MAX_LOOP_COUNT)
                            {
                                TicksThisFrame = 0;
                                throw new Exception("Thread entered infinite loop! ( >" + MAX_LOOP_COUNT + " primitives)");
                            }
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
#if IDE_COMPAT
                if (!IsCheck && VM.SignalBreaks)
                {
                    Breakpoint(Stack.LastOrDefault(), "!"+e.Message+" "+StackTraceSimplify(e.StackTrace.Split('\n').FirstOrDefault(x => x.Contains(".cs")) ?? ""));
                    ContinueExecution = false;
                    return;
                }
#endif

                if (e is ThreadAbortException) throw e;
                if (Stack.Count == 0) return;
                var context = Stack[Stack.Count - 1];
                bool Delete = ((Entity is VMGameObject) && (DialogCooldown > 30 * 20 - 10));
                if (DialogCooldown == 0)
                {

                    var simExcept = new VMSimanticsException(e.Message + StackTraceSimplify(e.StackTrace.Split('\n').FirstOrDefault(x => x.Contains(".cs")) ?? ""), context);
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

        private string StackTraceSimplify(string st)
        {
            var lastSlash = st.LastIndexOf('\\');
            if (lastSlash == -1) lastSlash = st.LastIndexOf('/');
            return (lastSlash == -1) ? st : st.Substring(lastSlash+1);
        }

        private void EvaluateQueuePriorities()
        {
            if (ActiveQueueBlock == -1 || ActiveQueueBlock >= Queue.Count) return;
            var active = Queue[ActiveQueueBlock];
            int CurrentPriority = (int)((VMAvatar)Entity).GetPersonData(VMPersonDataVariable.Priority); 
            if (CurrentPriority == (int)VMQueuePriority.Autonomous) CurrentPriority -= 1; // allow other auto actions to interrupt us
            var mode = active.Mode;
            // HACK: TS1 pushes a "Cancel Interaction" action onto the tree to interrupt itself, as well as setting current interaction to prio 0.
            // We're simulating that by notifiying idle if priority hits 0. (implied cancel interaction queued)
            // Make sure this interaction is not *meant* to be priority 0, like tso's idle.
            active.NotifyIdle = active.Priority != 0 && CurrentPriority == 0; 
            for (int i = ActiveQueueBlock + 1; i < Queue.Count; i++)
            {
                if (Queue[i].Callee == null || Queue[i].Callee.Dead)
                {
                    Queue.RemoveAt(i--); //remove interactions to dead objects (not within active queue block)
                    continue;
                }
                if ((int)Queue[i].Priority > CurrentPriority)// && mode != VMQueueMode.ParentIdle)
                {
                    active.NotifyIdle = true;
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
            else
            {
                VMInstruction instruction;
                VMPrimitiveExitCode result = currentFrame.Routine.Execute(currentFrame, out instruction);
                HandleResult(currentFrame, instruction, result);
            }
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

        public void ExecuteSubRoutine(VMStackFrame frame, VMRoutine routine, GameObject codeOwner, VMSubRoutineOperand args)
        {
            if (routine == null)
            {
                Pop(VMPrimitiveExitCode.ERROR);
                return;
            }

            var childFrame = new VMStackFrame
            {
                Routine = routine,
                Caller = frame.Caller,
                Callee = frame.Callee,
                CodeOwner = codeOwner,
                StackObject = frame.StackObject,
                _StackObjectID = frame.StackObjectID, //pass this without doing a lookup
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

        public VMPrimitiveExitCode ExecuteSubRoutine(VMStackFrame frame, ushort opcode, VMSubRoutineOperand operand)
        {
            VMRoutine bhav = null;

            GameObject CodeOwner;
            if (opcode >= 8192)
            {
                // Semi-Global sub-routine call
                bhav = (VMRoutine)frame.ScopeResource.SemiGlobal.GetRoutine(opcode);
            }
            else if (opcode >= 4096)
            {
                // Private sub-routine call
                bhav = (VMRoutine)frame.ScopeResource.GetRoutine(opcode);
            }
            else
            {
                // Global sub-routine call
                //CodeOwner = frame.Global.Resource;
                bhav = (VMRoutine)frame.Global.Resource.GetRoutine(opcode);
            }

            CodeOwner = frame.CodeOwner;
            
            ExecuteSubRoutine(frame, bhav, CodeOwner, operand);
#if IDE_COMPAT
            if (Stack.LastOrDefault().GetCurrentInstruction().Breakpoint || ThreadBreak == VMThreadBreakMode.StepIn)
            {
                Breakpoint(frame, "Stepped in.");
                ContinueExecution = false;
            }
            else
#endif
            {
                ContinueExecution = true;
            }

            return VMPrimitiveExitCode.CONTINUE;
        }

        private void ExecuteInstruction(VMStackFrame frame)
        {
            var instruction = frame.GetCurrentInstruction();
            var opcode = instruction.Opcode;

            if (opcode >= 256)
            {
                ExecuteSubRoutine(frame, opcode, (VMSubRoutineOperand)instruction.Operand);
                return;
            }


            var primitive = VMContext.Primitives[opcode];
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
                    ContinueExecution = false;
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
                    MoveToInstruction(frame, instruction.TruePointer, true);
                    if (ContinueExecution)
                    {
                        ScheduleIdleStart = Context.VM.Scheduler.CurrentTickID;
                        Context.VM.Scheduler.ScheduleTickIn(Entity, 1);
                        ContinueExecution = false;
                    }
                    break;
                case VMPrimitiveExitCode.GOTO_FALSE_NEXT_TICK:
                    MoveToInstruction(frame, instruction.FalsePointer, true);
                    if (ContinueExecution)
                    {
                        ScheduleIdleStart = Context.VM.Scheduler.CurrentTickID;
                        Context.VM.Scheduler.ScheduleTickIn(Entity, 1);
                        ContinueExecution = false;
                    }
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

            ContinueExecution = continueExecution;
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
                        string result = "Unknown Break";
                        switch (ThreadBreak) {
                            case VMThreadBreakMode.StepIn:
                                result = "Stepped In."; break;
                            case VMThreadBreakMode.StepOut:
                                result = "Stepped Out."; break;
                            case VMThreadBreakMode.StepOver:
                                result = "Stepped Over."; break;
                        }
                        Breakpoint(frame, result);
                    }
                    break;
            }

            ContinueExecution = (ThreadBreak != VMThreadBreakMode.Pause) && ContinueExecution;
        }

        public void Breakpoint(VMStackFrame frame, string description)
        {
            if (IsCheck) return; //can't breakpoint in check trees.
            ThreadBreak = VMThreadBreakMode.Pause;
            ThreadBreakString = description;
            BreakFrame = Stack.IndexOf(frame);
            Context.VM.BreakpointHit(Entity);
        }

        public void Pop(VMPrimitiveExitCode result)
        {
            var discardResult = Stack[Stack.Count - 1].DiscardResult;
            var contextSwitch = (Stack.Count > 1) && Stack.LastOrDefault().ActionTree != Stack[Stack.Count - 2].ActionTree;
            Stack.RemoveAt(Stack.Count - 1);
            LastStackExitCode = result;

            if (discardResult) //interaction switching back to main (it cannot be the other way...)
            {
                var interaction = Queue[ActiveQueueBlock];
                EndCurrentInteraction();
                result = (!interaction.Flags.HasFlag(TTABFlags.RunImmediately)) ? VMPrimitiveExitCode.CONTINUE_NEXT_TICK : VMPrimitiveExitCode.CONTINUE;
            }
            if (Stack.Count > 0)
            {
                if (result == VMPrimitiveExitCode.RETURN_TRUE)
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

        public bool Push(VMStackFrame frame)
        {
            if (frame.Routine.Instructions.Length == 0) return false; //some bhavs are empty... do not execute these.
            Stack.Add(frame);

            /** Initialize the locals **/
            var numLocals = Math.Max(frame.Routine.Locals, frame.Routine.Arguments);
            frame.Locals = new short[numLocals];
            frame.Thread = this;

            frame.InstructionPointer = 0;
            return true;
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
                //invocation.Priority = short.MinValue;
                this.Queue.Add(invocation);
                return;
            }
            var leapfrog = Context.VM.TS1 ? (TTABFlags)0: TTABFlags.Leapfrog;

            if (Queue.Count == 0) //if empty, just queue right at the front 
                this.Queue.Add(invocation);
            else if ((invocation.Flags & TTABFlags.FSOPushHead) > 0)
                //place right after active interaction, ignoring all priorities.
                this.Queue.Insert(ActiveQueueBlock + 1, invocation);
            else if (((invocation.Flags & TTABFlags.FSOPushTail) | leapfrog) > 0 && invocation.Mode != VMQueueMode.ParentExit)
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
            else //we've got an even harder job! find a place for this interaction based on its priority
            {
                bool hitParentEnd = (invocation.Mode != VMQueueMode.ParentIdle);
                for (int i = Queue.Count - 1; i > ActiveQueueBlock; i--)
                {
                    if (hitParentEnd && (invocation.Priority <= Queue[i].Priority || Queue[i].Mode == VMQueueMode.ParentExit)) //skip until we find a parent exit or something with the same or higher priority.
                    {
                        this.Queue.Insert(i + 1, invocation);
                        if (Context.VM.TS1 && invocation.Priority <= Queue[i].Priority)
                        {
                            // queue skip all items with a lower priority (after this interaction)
                            // i've verified this happens in ts1, but I don't know if it does in TSO so i've locked it for now.
                            i += 2;
                            while (i < this.Queue.Count)
                            {
                                CancelAction(this.Queue[i].UID);
                            }
                        }
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
                interaction.NotifyIdle = true;
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
                                Queue[i].NotifyIdle = true;
                                Queue[i].Priority = 0;
                            }
                        }
                        else if (Queue[i].Mode == Engine.VMQueueMode.ParentExit)
                        {
                            Queue[i].NotifyIdle = true;
                            Queue[i].Priority = 0;
                        }
                        //parent exit needs to "appear" like it is cancelled.
                    }
                }

                var canQueueSkip = !interaction.Flags.HasFlag(TTABFlags.MustRun);

                if (canQueueSkip && (index > ActiveQueueBlock || Stack.LastOrDefault()?.ActionTree == false) && interaction.Mode == Engine.VMQueueMode.Normal)
                {
                    Queue.Remove(interaction);
                    if (Context.VM.TS1) interaction.Callee.ExecuteEntryPoint(4, Context, true, Entity); //queue skipped
                }
                else
                {
                    Entity.SetFlag(VMEntityFlags.InteractionCanceled, true);
                    ((VMAvatar)Entity).SetPersonData(VMPersonDataVariable.Priority, 0);
                }
            }
        }

        private bool ExecuteAction(VMQueuedAction action)
        {
            //set the new interaction's priority
            ((VMAvatar)Entity).SetPersonData(VMPersonDataVariable.Priority, action.Priority);
            var frame = action.ToStackFrame(Entity);
            frame.DiscardResult = true;
            return Push(frame);
        }

        public List<VMPieMenuInteraction> CheckTS1Action(VMQueuedAction action, bool auto)
        {
            var result = new List<VMPieMenuInteraction>();

            if (Entity is VMAvatar && !action.Flags.HasFlag(TTABFlags.FSOSkipPermissions)) //just let everyone use the CSR interactions
            {
                var avatar = (VMAvatar)Entity;

                if ((action.Flags & (TTABFlags.TS1AllowCats | TTABFlags.TS1AllowDogs)) > 0)
                {
                    //interaction can only be performed by cats or dogs
                    //if (!avatar.IsPet) return null;
                    //check we're the correct type
                    if (avatar.IsCat && (action.Flags & TTABFlags.TS1AllowCats) == 0) return null;
                    if (avatar.IsDog && (action.Flags & TTABFlags.TS1AllowDogs) == 0) return null;
                }
                else if (avatar.IsPet) return null; //not allowed

                var isVisitor = avatar.GetPersonData(VMPersonDataVariable.PersonType) == 1 && avatar.GetPersonData(VMPersonDataVariable.GreetStatus) < 2;
                //avatar.ObjectID != Context.VM.GetGlobalValue(3);
                var debugTrees = false;

                TTABFlags ts1State =
                      ((isVisitor) ? TTABFlags.AllowVisitors : 0)
                    | ((avatar.GetPersonData(VMPersonDataVariable.PersonsAge) < 18) ? TTABFlags.TS1NoChild : 0)
                    | ((avatar.GetPersonData(VMPersonDataVariable.PersonsAge) >= 18 && !avatar.IsPet) ? TTABFlags.TS1NoAdult : 0);

                //DEBUG: enable debug interction for all CSRs.
                if ((action.Flags & TTABFlags.Debug) > 0)
                {
                    if (!isVisitor && debugTrees)
                        return result; //do not bother running check
                    else
                        return null; //disable debug for everyone else.
                }

                //NEGATIVE EFFECTS:
                var pos = ts1State & (TTABFlags.TS1NoChild | TTABFlags.TS1NoAdult);
                var ts1Compare = action.Flags;
                if ((pos & ts1Compare) > 0) return null;

                var negMask = (TTABFlags.AllowVisitors);

                var negatedFlags = (~ts1Compare) & negMask;
                if ((negatedFlags & ts1State) > 0) return null; //we are disallowed
            }
            if (action.CheckRoutine != null)
            {
                var args = new short[4];
                if (auto) args[0] = 1;
                if (EvaluateCheck(Context, Entity, new VMStackFrame()
                {
                    Caller = Entity,
                    Callee = action.Callee,
                    CodeOwner = action.CodeOwner,
                    StackObject = action.StackObject,
                    Routine = action.CheckRoutine,
                    Args = args
                }, null, result) != VMPrimitiveExitCode.RETURN_TRUE)
                {
                    return null;
                }
            }
            return result;
        }

        public List<VMPieMenuInteraction> CheckAction(VMQueuedAction action, bool auto = false)
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
            if (Context.VM.TS1) return CheckTS1Action(action, auto);
            var result = new List<VMPieMenuInteraction>();
            
            if (!action.Flags.HasFlag(TTABFlags.FSOSkipPermissions) && Entity is VMAvatar) //just let everyone use the CSR interactions
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
                else if (avatar.IsPet && avatar.AvatarState.Permissions < VMTSOAvatarPermissions.Admin) return null; //not allowed

                if ((action.Flags & TTABFlags.TSOIsRepair) > 0 != ((action.Callee.MultitileGroup.BaseObject?.TSOState as VMTSOObjectState)?.Broken ?? false)) return null;

                uint ownerID = 0;
                if (action.Callee is VMGameObject) {
                    var state = ((VMTSOObjectState)action.Callee.TSOState);
                    ownerID = state?.OwnerID ?? 0;
                    if (ownerID != 0 && state.ObjectFlags.HasFlag(VMTSOObjectFlags.FSODonated))
                    {
                        ownerID = Context.VM.TSOState.OwnerID; //owner rewrite to mayor
                    }
                }

                TSOFlags tsoState =
                    ((!(action.Callee is VMGameObject) || avatar.PersistID == ownerID)
                    ? TSOFlags.AllowObjectOwner : 0)
                    | ((avatar.AvatarState.Permissions == VMTSOAvatarPermissions.Visitor) ? TSOFlags.AllowVisitors : 0)
                    | ((avatar.AvatarState.Permissions >= VMTSOAvatarPermissions.Roommate) ? TSOFlags.AllowRoommates : 0)
                    | ((avatar.AvatarState.Permissions == VMTSOAvatarPermissions.Admin) ? TSOFlags.AllowCSRs : 0)
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
            if ((!action.Flags.HasFlag(TTABFlags.FSOSkipPermissions) || ((action.Flags & TTABFlags.TSORunCheckAlways) > 0))
                && action.CheckRoutine != null)
            {
                var args = new short[4];
                if (auto) args[0] = 1;
                if (EvaluateCheck(Context, Entity, new VMStackFrame()
                {
                    Caller = Entity,
                    Callee = action.Callee,
                    CodeOwner = action.CodeOwner,
                    StackObject = action.StackObject,
                    Routine = action.CheckRoutine,
                    Args = args
                }, null, result) != VMPrimitiveExitCode.RETURN_TRUE)
                {
                    return null;
                }
            }
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
                TempRegisters = (short[])TempRegisters.Clone(),
                TempXL = (int[])TempXL.Clone(),
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
        Immediate = 7,
        Reset = 8
    }
}