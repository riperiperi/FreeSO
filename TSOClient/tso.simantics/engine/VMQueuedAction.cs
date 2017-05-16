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

        public VMRoutine ActionRoutine;
        public VMRoutine CheckRoutine;
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
        public TSOFlags Flags2 = (TSOFlags)0x1f;

        public ushort UID; //a wraparound ID that is just here so that a specific interaction can be reliably "cancelled" by a client.

        public VMActionCallback Callback;

        public sbyte InteractionResult = -1; //when this becomes 0, the interaction is expecting an interaction result.
        public ushort ResultCheckCounter = 0; //how many times the interaction result has been checked. used for timeout.

        public VMStackFrame ToStackFrame(VMEntity caller)
        {
            var frame = new VMStackFrame
            {
                Caller = caller,
                Callee = Callee,
                CodeOwner = CodeOwner,
                Routine = ActionRoutine,
                StackObject = StackObject,
                ActionTree = true
            };
            if (Args == null) frame.Args = new short[4]; //always 4? i got crashes when i used the value provided by the routine, when for that same routine edith displayed 4 in the properties...
            else frame.Args = Args; //WARNING - if you use this, the args array MUST have the same number of elements the routine is expecting!
            return frame;
        }

        #region VM Marshalling Functions
        public VMQueuedActionMarshal Save()
        {
            return new VMQueuedActionMarshal
            {
                RoutineID = ActionRoutine.ID,
                CheckRoutineID = (CheckRoutine == null) ? (ushort)0 : CheckRoutine.ID,
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
                Flags2 = Flags2,
                UID = UID,
                Callback = (Callback == null)?null:Callback.Save(),
                InteractionResult = InteractionResult,
                ResultCheckCounter = ResultCheckCounter
            };
        }

        public void Load(VMQueuedActionMarshal input, VMContext context)
        {
            CodeOwner = FSO.Content.Content.Get().WorldObjects.Get(input.CodeOwnerGUID);

            BHAV bhav = null;
            if (input.RoutineID >= 8192) bhav = CodeOwner.Resource.SemiGlobal.Get<BHAV>(input.RoutineID);
            else if (input.RoutineID >= 4096) bhav = CodeOwner.Resource.Get<BHAV>(input.RoutineID);
            else bhav = context.Globals.Resource.Get<BHAV>(input.RoutineID);
            ActionRoutine = context.VM.Assemble(bhav);

            if (input.CheckRoutineID != 0)
            {
                if (input.CheckRoutineID >= 8192) bhav = CodeOwner.Resource.SemiGlobal.Get<BHAV>(input.CheckRoutineID);
                else if (input.CheckRoutineID >= 4096) bhav = CodeOwner.Resource.Get<BHAV>(input.CheckRoutineID);
                else bhav = context.Globals.Resource.Get<BHAV>(input.CheckRoutineID);
                CheckRoutine = context.VM.Assemble(bhav);
            }

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
            Flags2 = input.Flags2;
            UID = input.UID;
            Callback = (input.Callback == null)?null:new VMActionCallback(input.Callback, context);

            InteractionResult = input.InteractionResult;
            ResultCheckCounter = input.ResultCheckCounter;
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
        ParentIdle = 40,
        ParentExit = 30,
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
