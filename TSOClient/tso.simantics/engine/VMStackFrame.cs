using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.files.formats.iff;
using tso.content;

namespace tso.simantics.engine
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

        /** The avatar who executed this behavior **/
        public VMEntity Caller;

        /** The this object **/
        public VMEntity Callee;

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
            get{
                return Callee.Object.Resource;
            }
        }

        public Iff Global
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
                return Routine.VM;
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
