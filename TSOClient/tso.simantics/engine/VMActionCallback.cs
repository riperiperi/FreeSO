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

namespace FSO.SimAntics.Engine
{
    public class VMActionCallback
    {
        private int type;

        //type 1 variables
        private VMEntity Target;
        private byte Interaction;
        private bool SetParam;
        private VM vm;
        private VMEntity StackObject;
        private VMEntity Caller;

        public VMActionCallback(VM vm, byte interactionNumber, VMEntity target, VMEntity stackObj, VMEntity caller, bool paramAsObjectID) //type 1: interaction callback
        {
            this.type = 1;
            this.Target = target;
            this.Interaction = interactionNumber;
            this.SetParam = paramAsObjectID;
            this.StackObject = stackObj;
            this.vm = vm;
            this.Caller = caller;
        }

        //type 2 will be function callback.

        public void Run(VMEntity cbOwner) {
            if (type == 1) {
                BHAV bhav;
                GameIffResource CodeOwner = null;
                var Action = Target.TreeTable.InteractionByIndex[Interaction];
                ushort ActionID = Action.ActionFunction;

                if (ActionID < 4096)
                { //global
                    bhav = null;
                    //unimp as it has to access the context to get this.
                }
                else if (ActionID < 8192)
                { //local
                    bhav = Target.Object.Resource.Get<BHAV>(ActionID);
                    
                }
                else
                { //semi-global
                    bhav = Target.SemiGlobal.Resource.Get<BHAV>(ActionID);
                    //CodeOwner = Target.SemiGlobal.Resource;
                }

                CodeOwner = Target.Object.Resource;
                var routine = vm.Assemble(bhav);
                var args = new short[4];
                if (SetParam) args[0] = cbOwner.ObjectID;

                Caller.Thread.EnqueueAction(
                    new FSO.SimAntics.Engine.VMQueuedAction
                    {
                        Callee = Target,
                        CodeOwner = CodeOwner,
                        Routine = routine,
                        Name = Target.TreeTableStrings.GetString((int)Action.TTAIndex),
                        StackObject = this.StackObject,
                        Args = args,
                        InteractionNumber = Interaction,
                        Priority = VMQueuePriority.Maximum //not sure if this is meant to be the case!
                    }
                );
            }
        }
    }
}
