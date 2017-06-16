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

        public void Run(VMEntity cbOwner) {
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
                    ActionFlags = TTABFlags.Leapfrog;
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

                bhav = Target.GetBHAVWithOwner(ActionID, vm.Context).bhav;
                if (bhav == null) return; //???
                if (IsTree) ActionName = bhav.ChunkLabel;

                CodeOwner = Target.Object;
                var routine = vm.Assemble(bhav);
                var args = new short[4];
                if (SetParam) args[0] = cbOwner.ObjectID;

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
            }
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
