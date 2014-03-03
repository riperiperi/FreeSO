using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSO.Simantics
{
    public class VMInstruction
    {
        public VMRoutine Function;

        public ushort Opcode;
        public byte TruePointer;
        public byte FalsePointer;
        public byte Index;

        public object Operand;

        public bool Breakpoint;
        /** Runtime info **/
        public VMInstructionRTI Rti;
    }

    public class VMInstructionRTI
    {
        public string Description;
        public string Comment;
    }
}
