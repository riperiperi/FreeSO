using FSO.SimAntics.Primitives;

namespace FSO.SimAntics.Model
{
    public class VMDialogInfo
    {
        public bool Block;
        public VMEntity Caller;
        public VMEntity Icon;
        public VMDialogOperand Operand;
        public string Message;
        public string IconName;
        public string Title;

        public string Yes;
        public string No;
        public string Cancel;

        public ulong DialogID; //what primitive this dialog belongs to. (GUID<<32) | (BHAVID<<16) | (pointer) informs ui of duplicates.
    }
}
