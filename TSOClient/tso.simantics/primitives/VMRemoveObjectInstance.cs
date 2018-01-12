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
using FSO.Files.Formats.IFF.Chunks;
using FSO.Content;
using System.IO;

namespace FSO.SimAntics.Engine.Primitives
{
    public class VMRemoveObjectInstance : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMRemoveObjectInstanceOperand)args;
            VMEntity obj;
            if (operand.Target == 0) obj = context.Caller;
            else obj = context.StackObject;

            //operand.CleanupAll;
            obj?.Delete(true, context.VM.Context);

            //if (obj == context.StackObject) context.StackObject = null;

            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMRemoveObjectInstanceOperand : VMPrimitiveOperand
    {
        public short Target { get; set; }
        public byte Flags { get; set; }

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                Target = io.ReadInt16();
                Flags = io.ReadByte();
            }
        }

        public void Write(byte[] bytes) {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(Target);
                io.Write(Flags);
            }
        }
        #endregion

        public bool ReturnImmediately
        {
            get
            {
                return ((Flags & 1) == 1);
            }
            set
            {
                if (value) Flags |= 1;
                else Flags &= unchecked((byte)~1);
            }
        }

        public bool CleanupAll
        {
            get
            {
                return ((Flags & 2) == 2);
            }
            set
            {
                if (value) Flags |= 2;
                else Flags &= unchecked((byte)~2);
            }
        }
    }
}
