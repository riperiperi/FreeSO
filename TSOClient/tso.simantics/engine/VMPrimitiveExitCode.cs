/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.SimAntics.Engine
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
        CONTINUE, //used for primitives which change the control flow, don't quite return, more or idle yet.
        INTERRUPT //instantly ends this queue item. Used by Idle for Input with allow push: when any interactions are queued it exits out like this.
    }
}
