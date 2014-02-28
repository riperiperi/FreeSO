using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.content;

namespace tso.simantics.engine
{
    public class VMQueuedAction
    {
        public VMRoutine Routine;
        public VMEntity Callee;
        public VMEntity StackObject; //set to callee for interactions
        public GameIffResource CodeOwner; //used to access local resources from BHAVs like strings
    }
}
