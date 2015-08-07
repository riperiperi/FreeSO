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
    public class VMStopAllSounds : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMStopAllSoundsOperand)args;

            var owner = (operand.Flags == 1)?context.StackObject:context.Caller;
            var threads = owner.SoundThreads;

            for (int i = 0; i < threads.Count; i++)
            {
                threads[i].Thread.RemoveOwner(owner.ObjectID);
            }
            threads.Clear();

            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMStopAllSoundsOperand : VMPrimitiveOperand
    {
        public byte Flags;
        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                Flags = io.ReadByte();
            }
        }
        #endregion
    }
}
