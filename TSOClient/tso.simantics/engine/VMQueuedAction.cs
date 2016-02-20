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

        public short Priority = (short)VMQueuePriority.Idle;
        public VMQueueMode Mode = VMQueueMode.Normal;
        public TTABFlags Flags;

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
                Mode = Mode,
                Flags = Flags,
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
            Mode = input.Mode;
            Flags = input.Flags;
            UID = input.UID;
            Callback = (input.Callback == null)?null:new VMActionCallback(input.Callback, context);
        }

        public VMQueuedAction(VMQueuedActionMarshal input, VMContext context)
        {
            Load(input, context);
        }
        #endregion
    }

    public enum VMQueuePriority : short
    {
        Maximum = 100,
        Autonomous = 2,
        UserDriven = 50,
        ParentIdle = 25,
        ParentExit = 24,
        Idle = 0
    }

    public enum VMQueueMode : byte
    {
        Normal,
        ParentIdle,
        ParentExit, //hidden until active. DO NOT CANCEL OR SKIP!
        Idle
    }
}
