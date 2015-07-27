using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Files.formats.iff;
using TSO.Content;

namespace TSO.Simantics.engine
{
    /// <summary>
    /// Holds information about the execution of a routine
    /// </summary>
    public class VMStackFrame
    {
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

        /** Used to get strings and other resources (for primitives) from the code owner, as it may not be the callee but instead a semiglobal or global. **/
        public GameIffResource CodeOwner;

        /**
         * Routine locals
         */
        public ushort[] Locals;

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
                return Routine.VM.Context.Globals;
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
    }
}
