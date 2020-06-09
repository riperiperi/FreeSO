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
    public class VMPrimitiveRegistration
    {
        private VMPrimitiveHandler Handler;
        public VMPrimitiveRegistration(VMPrimitiveHandler handler)
        {
            this.Handler = handler;
        }

        public Type OperandModel;
        public string Name;
        public ushort Opcode;

        public VMPrimitiveHandler GetHandler(){
            return Handler;
        }
    }
}
