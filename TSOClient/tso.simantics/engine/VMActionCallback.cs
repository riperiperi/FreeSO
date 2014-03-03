using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Files.formats.iff.chunks;
using TSO.Content;

namespace TSO.Simantics.engine
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

        public VMActionCallback(VM vm, byte interactionNumber, VMEntity target, VMEntity stackObj, bool paramAsObjectID) //type 1: interaction callback
        {
            this.type = 1;
            this.Target = target;
            this.Interaction = interactionNumber;
            this.SetParam = paramAsObjectID;
            this.StackObject = stackObj;
            this.vm = vm;
        }

        //type 2 will be function callback.

        public void Run(VMEntity caller) {
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
                    CodeOwner = Target.Object.Resource;
                }
                else
                { //semi-global
                    bhav = Target.SemiGlobal.Resource.Get<BHAV>(ActionID);
                    CodeOwner = Target.SemiGlobal.Resource;
                }

                var routine = vm.Assemble(bhav);
                var args = new short[routine.Arguments];
                if (SetParam) args[0] = caller.ObjectID;

                Target.Thread.EnqueueAction(
                    new TSO.Simantics.engine.VMQueuedAction
                    {
                        Callee = Target,
                        CodeOwner = CodeOwner,
                        Routine = routine,
                        Name = Target.TreeTableStrings.GetString((int)Action.TTAIndex),
                        StackObject = this.StackObject,
                        Args = args,
                        InteractionNumber = Interaction
                    }
                );
            }
        }
    }
}
