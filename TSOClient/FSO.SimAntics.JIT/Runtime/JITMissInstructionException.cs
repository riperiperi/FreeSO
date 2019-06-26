using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.JIT.Runtime
{
    public class JITMissInstructionException : Exception
    {
        public JITMissInstructionException(byte instruction) : base($"JIT assembly missing instruction {instruction} (bad true/false return detection or did not expect to enter).")
        {

        }
    }
}
