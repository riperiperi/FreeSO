/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.SimAntics.Engine;
using FSO.Files.Utils;

namespace FSO.SimAntics.Primitives
{
    public class VMSetBalloonHeadline : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            //TODO: Implement this.
            var operand = (VMSetBalloonHeadlineOperand)args;
            if (operand.Duration == 0)
            {
                /** Clear **/
            }
            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMSetBalloonHeadlineOperand : VMPrimitiveOperand
    {
        public ushort Flags2;
        public sbyte Index;
        public VMSetBalloonHeadlineOperandGroup Group;
        public ushort Duration;
        public ushort FlagsAndType;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN)){
                Flags2 = io.ReadUInt16();
                Index = (sbyte)io.ReadByte();
                Group = (VMSetBalloonHeadlineOperandGroup)(sbyte)io.ReadByte();
                Duration = io.ReadUInt16();
                FlagsAndType = io.ReadUInt16();
            }
        }
        #endregion
    }

    public enum VMSetBalloonHeadlineOperandGroup
    {
        OldStyle = 0,
        Balloon = 1,
        Conversation = 2,
        Motive = 3,
        Relationship = 4,
        Headline = 5,
        Debug = 6,
        Algorithmic = 7,
        RouteFailure = 8,
        Progress = 9,

    }
}
