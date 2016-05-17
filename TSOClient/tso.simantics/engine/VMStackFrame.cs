/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Files.Formats.IFF;
using FSO.Content;
using FSO.SimAntics.Marshals.Threads;
using FSO.Files.Formats.IFF.Chunks;

namespace FSO.SimAntics.Engine
{
    /// <summary>
    /// Holds information about the execution of a routine
    /// </summary>
    public class VMStackFrame
    {
        public VMStackFrame() { }

        /** Thread executing this routine **/
        public VMThread Thread;

        /** Routine that this context relates to **/
        public VMRoutine Routine;
        
        /** Current instruction **/
        public ushort InstructionPointer;

        /** The object who executed this behavior **/
        public VMEntity Caller;

        /** The object the code is running on **/
        public VMEntity Callee;

        /** An object selected by the code to perform operations on. **/
        public VMEntity StackObject;
        
        /** Indicates that the current stack frame is part of an action tree.
         ** Set by "idle for input, allow push", when an interaction is selected.
         ** Used to stop recursive interactions, is only false when within "main".
         **/
        public bool ActionTree;

        /** Used to get strings and other resources (for primitives) from the code owner, as it may not be the callee but instead a semiglobal or global. **/
        public GameIffResource ScopeResource {
            get
            {
                return CodeOwner.Resource;
            }
        }

        public GameObject CodeOwner;
        /**
         * Routine locals
         */
        public short[] Locals;

        /**
         * Arguments
         */
        public short[] Args;

        public GameObjectResource CallerPrivate
        {
            get
            {
                return Caller.Object.Resource;
            }
        }

        public GameObjectResource CalleePrivate
        {
            get
            {
                return Callee.Object.Resource;
            }
        }

        public GameObjectResource StackObjPrivate
        {
            get
            {
                return StackObject.Object.Resource;
            }
        }

        public GameGlobal Global
        {
            get
            {
                return Thread.Context.Globals;
            }
        }

        public VM VM
        {
            get
            {
                return Thread.Context.VM;
            }
        }

        /** Utilities **/
        public VMInstruction GetCurrentInstruction(){
            return Routine.Instructions[InstructionPointer];
        }
        public T GetCurrentOperand<T>(){
            return (T)GetCurrentInstruction().Operand;
        }

        #region VM Marshalling Functions
        public virtual VMStackFrameMarshal Save()
        {
            return new VMStackFrameMarshal
            {
                RoutineID = Routine.ID,
                InstructionPointer = InstructionPointer,
                Caller = (Caller == null) ? (short)0 : Caller.ObjectID,
                Callee = (Callee == null) ? (short)0 : Callee.ObjectID,
                StackObject = (StackObject == null) ? (short)0 : StackObject.ObjectID,
                CodeOwnerGUID = CodeOwner.OBJ.GUID,
                Locals = Locals,
                Args = Args,
                ActionTree = ActionTree
            };
        }

        public virtual void Load(VMStackFrameMarshal input, VMContext context)
        {
            CodeOwner = FSO.Content.Content.Get().WorldObjects.Get(input.CodeOwnerGUID);

            BHAV bhav = null;
            if (input.RoutineID >= 8192) bhav = ScopeResource.SemiGlobal.Get<BHAV>(input.RoutineID);
            else if (input.RoutineID >= 4096) bhav = ScopeResource.Get<BHAV>(input.RoutineID);
            else bhav = Global.Resource.Get<BHAV>(input.RoutineID);
            Routine = VM.Assemble(bhav);

            InstructionPointer = input.InstructionPointer;
            Caller = context.VM.GetObjectById(input.Caller);
            Callee = context.VM.GetObjectById(input.Callee);
            StackObject = context.VM.GetObjectById(input.StackObject);
            Locals = input.Locals;
            Args = input.Args;
            ActionTree = input.ActionTree;
        }

        public VMStackFrame(VMStackFrameMarshal input, VMContext context, VMThread thread)
        {
            Thread = thread;
            Load(input, context);  
        }
        #endregion
    }
}
