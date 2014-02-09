using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.files.formats.iff;

namespace tso.simantics
{
    public class VMRoutine
    {
        public VMRoutine(){
        }

        public VM VM;
        public byte Type;
        public VMInstruction[] Instructions;
        public ushort Locals;
        public ushort Arguments;

        /** Run time info **/
        public VMFunctionRTI Rti;
    }


    public class VMFunctionRTI
    {
        public string Name;
    }
}
