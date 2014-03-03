using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Files.formats.iff;

namespace TSO.Simantics
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
