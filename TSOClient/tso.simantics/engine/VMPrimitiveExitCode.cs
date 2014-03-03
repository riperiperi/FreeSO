using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSO.Simantics.engine
{
    public enum VMPrimitiveExitCode
    {
        GOTO_TRUE,
        GOTO_FALSE,
        GOTO_TRUE_NEXT_TICK,
        GOTO_FALSE_NEXT_TICK,
        RETURN_TRUE,
        RETURN_FALSE,
        ERROR,
        CONTINUE_NEXT_TICK,
        CONTINUE //used for primitives which change the control flow, don't quite return, more or idle yet.
    }
}
