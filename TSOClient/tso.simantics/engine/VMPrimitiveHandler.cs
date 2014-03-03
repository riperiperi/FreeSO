using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSO.Simantics.engine
{
    public abstract class VMPrimitiveHandler
    {
        protected void Trace(string message){
            System.Diagnostics.Debug.WriteLine(message);
        }

        public abstract VMPrimitiveExitCode Execute(VMStackFrame context);
    }
}
