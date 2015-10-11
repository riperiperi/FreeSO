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
using FSO.SimAntics.Marshals.Threads;
using FSO.Files.Formats.IFF.Chunks;

namespace FSO.SimAntics.Engine
{
    public class VMQueuedAction
    {
        public VMQueuedAction() { }

        public VMRoutine Routine;
        public VMEntity Callee;
        public VMEntity StackObject; //set to callee for interactions

        private VMEntity _IconOwner = null; //defaults to callee
        public VMEntity IconOwner {
            get {
                return (_IconOwner == null)?Callee:_IconOwner;
            }
            set {
                _IconOwner = value;
            }
        } 

        public GameObject CodeOwner; //used to access local resources from BHAVs like strings
        public string Name;
        public short[] Args; //WARNING - if you use this, the args array MUST have the same number of elements the routine is expecting!

        public int InteractionNumber = -1; //this interaction's number... This is needed for create object callbacks 
                                           //for This Interaction but entry point functions don't have this...
                                           //suggests init and main don't use action queue.
        public bool Cancelled;
        public VMQueuePriority Priority = VMQueuePriority.Idle; //Sliding scale 0-5, where 0 is maximum priority, 5 is idle.

        public ushort UID; //a wraparound ID that is just here so that a specific interaction can be reliably "cancelled" by a client.

        public VMActionCallback Callback;

        #region VM Marshalling Functions
        public VMQueuedActionMarshal Save()
        {
            return new VMQueuedActionMarshal
            {
                RoutineID = Routine.ID,
                Callee = (Callee == null) ? (short)0 : Callee.ObjectID,
                StackObject = (StackObject == null) ? (short)0 : StackObject.ObjectID,
                IconOwner = (IconOwner == null) ? (short)0 : IconOwner.ObjectID,
                CodeOwnerGUID = CodeOwner.OBJ.GUID,
                Name = Name,
                Args = Args,
                InteractionNumber = InteractionNumber,
                Cancelled = Cancelled,
                Priority = Priority,
                UID = UID,
                Callback = (Callback == null)?null:Callback.Save()
            };
        }

        public void Load(VMQueuedActionMarshal input, VMContext context)
        {
            CodeOwner = FSO.Content.Content.Get().WorldObjects.Get(input.CodeOwnerGUID);

            BHAV bhav = null;
            if (input.RoutineID >= 8192) bhav = CodeOwner.Resource.SemiGlobal.Get<BHAV>(input.RoutineID);
            else if (input.RoutineID >= 4096) bhav = CodeOwner.Resource.Get<BHAV>(input.RoutineID);
            else bhav = context.Globals.Resource.Get<BHAV>(input.RoutineID);
            Routine = context.VM.Assemble(bhav);

            Callee = context.VM.GetObjectById(input.Callee);
            StackObject = context.VM.GetObjectById(input.StackObject);
            IconOwner = context.VM.GetObjectById(input.IconOwner);
            Name = input.Name;
            Args = input.Args;
            InteractionNumber = input.InteractionNumber;
            Cancelled = input.Cancelled;
            Priority = input.Priority;
            UID = input.UID;
            Callback = (input.Callback == null)?null:new VMActionCallback(input.Callback, context);
        }

        public VMQueuedAction(VMQueuedActionMarshal input, VMContext context)
        {
            Load(input, context);
        }
        #endregion
    }

    public enum VMQueuePriority
    {
        Maximum = 0,
        Autonomous = 1,
        UserDriven = 2,
        ParentIdle = 3,
        ParentExit = 4,
        Idle = 5
    }
}
