using FSO.SimAntics.Engine;

namespace FSO.SimAntics
{
    public class VMInstruction
    {
        public VMRoutine Function;

        public ushort Opcode;
        public byte TruePointer;
        public byte FalsePointer;
        public byte Index;

        public VMPrimitiveOperand Operand;

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
