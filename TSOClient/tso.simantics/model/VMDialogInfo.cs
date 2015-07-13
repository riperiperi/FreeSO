using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Simantics.primitives;

namespace TSO.Simantics.model
{
    public struct VMDialogInfo
    {
        public VMEntity Caller;
        public VMEntity Icon;
        public VMDialogStringsOperand Operand;
        public string Message;
        public string IconName;
        public string Title;
        public string Yes;
    }
}
