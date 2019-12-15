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
    public enum VMPrimitiveExitCode : byte
    {
        GOTO_TRUE = 0,
        GOTO_FALSE = 1,
        GOTO_TRUE_NEXT_TICK = 2,
        GOTO_FALSE_NEXT_TICK = 3,
        RETURN_TRUE = 4,
        RETURN_FALSE = 5,
        ERROR = 6,
        CONTINUE_NEXT_TICK = 7,
        CONTINUE = 8, //used for primitives which change the control flow, don't quite return, more or idle yet.
        INTERRUPT = 9, //instantly ends this queue item. Used by Idle for Input with allow push: when any interactions are queued it exits out like this.
        CONTINUE_FUTURE_TICK = 10, //special schedule mode used by idle and idle for input. removes processing for this object for multiple frames.
    }
}
