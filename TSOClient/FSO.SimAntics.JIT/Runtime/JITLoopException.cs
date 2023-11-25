using System;

namespace FSO.SimAntics.JIT.Runtime
{
    public class JITLoopException : Exception
    {
        public JITLoopException() : base("JIT/AOT compiled SimAntics routine exceeded maximum loop count. (1000000)")
        {

        }
    }
}
