/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using FSO.SimAntics.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.SimAntics
{
    public class VMInstruction
    {
        public VMRoutine Function;

        public ushort Opcode;
        public byte TruePointer;
        public byte FalsePointer;
        public byte Index;

        public VMPrimitiveOperand Operand;

        public bool Breakpoint;
        /** Runtime info **/
        public VMInstructionRTI Rti;
    }

    public class VMInstructionRTI
    {
        public string Description;
        public string Comment;
    }
}
