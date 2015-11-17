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
using FSO.SimAntics.Engine.Scopes;
using System.IO;

namespace FSO.SimAntics.Primitives
{
    // Generally something bad has happened when this is called.

    public class VMBreakPoint : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            //todo: check condition
            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMBreakPointOperand : VMPrimitiveOperand
    {
        public short Data { get; set; }
        public VMVariableScope Scope { get; set; }

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                Data = io.ReadInt16();
                Scope = (VMVariableScope)io.ReadUInt16();
            }
        }

        public void Write(byte[] bytes)
        {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(Data);
                io.Write((ushort)Scope);
            }
        }
        #endregion
    }
}
