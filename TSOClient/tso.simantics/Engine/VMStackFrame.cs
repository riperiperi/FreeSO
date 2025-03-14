using System;
using FSO.Content;
using FSO.SimAntics.Marshals.Threads;

namespace FSO.SimAntics.Engine
{
    public enum VMSpecialResult
    {
        Normal,
        Interaction,
        Retry
    }

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
        public byte InstructionPointer;

        /** The object who executed this behavior **/
        public VMEntity Caller;

        /** The object the code is running on **/
        public VMEntity Callee;

        /** An object selected by the code to perform operations on. **/
        public VMEntity StackObject
        {
            get { return _StackObject; }
            set {
                _StackObject = value;
                _StackObjectID = value?.ObjectID ?? 0;
            }
        }
        public short StackObjectID
        {
            get { return _StackObjectID; }
            set
            {
                _StackObjectID = value;
                _StackObject = VM.GetObjectById(value);
            }
        }

        private VMEntity _StackObject;
        public short _StackObjectID;

        /** 
         * Normally, returning true or false from a stack frame goes to the corresponding node on the parent frame.
         * This works differently for interactions, and for TS1 loaded trees. (no routing frames, needs to rerun the route primitive)
         */
        public VMSpecialResult SpecialResult;
        
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
                RoutineID = Routine?.ID ?? 0,
                InstructionPointer = InstructionPointer,
                Caller = (Caller == null) ? (short)0 : Caller.ObjectID,
                Callee = (Callee == null) ? (short)0 : Callee.ObjectID,
                StackObject = StackObjectID,
                CodeOwnerGUID = CodeOwner.OBJ.GUID,
                Locals = (short[])Locals?.Clone(),
                Args = (short[])Args?.Clone(),
                SpecialResult = SpecialResult,
                ActionTree = ActionTree,
            };
        }

        public virtual void Load(VMStackFrameMarshal input, VMContext context)
        {
            CodeOwner = FSO.Content.Content.Get().WorldObjects.Get(input.CodeOwnerGUID);

            Routine = null;
            if (input.RoutineID >= 8192) Routine = (VMRoutine)ScopeResource.SemiGlobal.GetRoutine(input.RoutineID);
            else if (input.RoutineID >= 4096) Routine = (VMRoutine)ScopeResource.GetRoutine(input.RoutineID);
            else Routine = (VMRoutine)Global.Resource.GetRoutine(input.RoutineID);

            InstructionPointer = (byte)input.InstructionPointer;
            Caller = context.VM.GetObjectById(input.Caller);
            Callee = context.VM.GetObjectById(input.Callee);
            StackObjectID = input.StackObject;
            if (Routine != null && input.Locals != null && Routine.Locals > input.Locals.Length)
            {
                Locals = new short[Routine.Locals];
                Array.Copy(input.Locals, Locals, input.Locals.Length);
            }
            else
            {
                Locals = input.Locals;
            }
            Args = input.Args;
            SpecialResult = input.SpecialResult;
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
