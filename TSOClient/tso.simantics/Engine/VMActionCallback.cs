/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Files.Formats.IFF.Chunks;
using FSO.Content;
using FSO.SimAntics.Marshals.Threads;
using FSO.SimAntics.Primitives;

namespace FSO.SimAntics.Engine
{
    public class VMActionCallback
    {
        private int type;

        //type 1 variables
        private VMEntity Target;
        private short Interaction;
        private bool SetParam;
        private VM vm;
        private VMEntity StackObject;
        private VMEntity Caller;
        private bool IsTree; //true if we're calling a tree instead of an interaction number

        public VMActionCallback(VM vm, short interactionNumber, VMEntity target, VMEntity stackObj, VMEntity caller, bool paramAsObjectID, bool isTree) //type 1: interaction callback
        {
            this.type = 1;
            this.Target = target;
            this.Interaction = interactionNumber;
            this.SetParam = paramAsObjectID;
            this.StackObject = stackObj;
            this.vm = vm;
            this.Caller = caller;
            this.IsTree = isTree;
        }

        //type 2 will be function callback.

        public bool Run(VMEntity cbOwner) {
            if (Target == null) return false;
            if (type == 1) {
                BHAV bhav;
                GameObject CodeOwner = null;
                ushort ActionID;
                TTABFlags ActionFlags;
                var global = Interaction < 0;
                Interaction &= 0x7FFF;
                string ActionName = "";
                if (IsTree)
                {
                    ActionFlags = TTABFlags.FSOPushHead;
                    ActionID = (ushort)Interaction;
                }
                else
                {
                    var tt = global ? vm.Context.GlobalTreeTable : Target.TreeTable;
                    var ttas = global ? vm.Context.GlobalTTAs : Target.TreeTableStrings;
                    var Action = tt.InteractionByIndex[(byte)Interaction];
                    ActionID = Action.ActionFunction;
                    ActionFlags = Action.Flags;
                    ActionName = ttas.GetString((int)Action.TTAIndex);
                }

                var routine = Target.GetRoutineWithOwner(ActionID, vm.Context)?.routine;
                if (routine == null) return false; //???
                if (IsTree) ActionName = routine.Chunk.ChunkLabel;

                CodeOwner = Target.Object;
                var args = new short[4];
                if (SetParam) args[0] = cbOwner.ObjectID;


                if (true)
                {
                    //we can't rely on objects to run their callback functions, as they don't usually allow push
                    //plus objects shouldn't really be able to run interactions in ts1 anyways, that's a freeso thing

                    //we should probably find a better way to do this. here's a list of what we know:
                    // - routine MUST be ran with the same caller.
                    // - temp[0] in caller must contain the created object ID.
                    // - object shouldn't have to wait for allow push to run the callback (so probably not an interaction)
                    // (from below examples)
                    // - args are passed in as requested. arg[0] also contains the created object ID.
                    // - i can't remember, but the callback may need to run over multiple ticks. we need to list all occurances of this
                    //   and study them carefully.
                    Caller.Thread.TempRegisters[0] = cbOwner.ObjectID;

                    Caller.Thread.ExecuteSubRoutine(Caller.Thread.Stack.Last(), routine, CodeOwner, new VMSubRoutineOperand(args));

                    return true;
                    //Caller.Thread.RunInMyStack(routine, CodeOwner, args, StackObject);
                }
                else
                {

                    Caller.Thread.EnqueueAction(
                        new FSO.SimAntics.Engine.VMQueuedAction
                        {
                            Callee = Target,
                            CodeOwner = CodeOwner,
                            ActionRoutine = routine,
                            Name = ActionName,
                            StackObject = this.StackObject,
                            Args = args,
                            InteractionNumber = Interaction,
                            Priority = (short)VMQueuePriority.Maximum, //not sure if this is meant to be the case!
                        Flags = ActionFlags
                        }
                    );
                    return false;
                }
            }
            return false;
        }

        #region VM Marshalling Functions
        public VMActionCallbackMarshal Save()
        {
            return new VMActionCallbackMarshal
            {
                Type = type,
                Target = (Target == null) ? (short)0 : Target.ObjectID,
                Interaction = Interaction,
                SetParam = SetParam,
                StackObject = (StackObject == null) ? (short)0 : StackObject.ObjectID,
                Caller = (Caller == null) ? (short)0 : Caller.ObjectID,
                IsTree = IsTree
            };
        }

        public void Load(VMActionCallbackMarshal input, VMContext context)
        {
            type = input.Type;
            Target = context.VM.GetObjectById(input.Target);
            Interaction = input.Interaction;
            SetParam = input.SetParam;
            StackObject = context.VM.GetObjectById(input.StackObject);
            Caller = context.VM.GetObjectById(input.Caller);
            IsTree = input.IsTree;
        }

        public VMActionCallback(VMActionCallbackMarshal input, VMContext context)
        {
            Load(input, context);
        }
        #endregion
    }
}
