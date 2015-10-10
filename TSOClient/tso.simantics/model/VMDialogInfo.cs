/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.SimAntics.Primitives;

namespace FSO.SimAntics.Model
{
    public struct VMDialogInfo
    {
        public bool Block;
        public VMEntity Caller;
        public VMEntity Icon;
        public VMDialogOperand Operand;
        public string Message;
        public string IconName;
        public string Title;

        public string Yes;
        public string No;
        public string Cancel;
    }
}
