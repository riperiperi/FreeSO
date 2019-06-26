using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.JIT.Runtime
{
    public class JITLoopException : Exception
    {
        public JITLoopException() : base("JIT/AOT compiled SimAntics routine exceeded maximum loop count. (1000000)")
        {

        }
    }
}
