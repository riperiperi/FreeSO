using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace tso.simantics.engine
{
    public class VMQueuedAction
    {
        public VMRoutine Routine;
        public VMEntity Callee;
        public VMEntity StackObject; //set to callee for interactions
    }
}
