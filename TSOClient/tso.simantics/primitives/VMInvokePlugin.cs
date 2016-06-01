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
using FSO.SimAntics.Model;

namespace FSO.SimAntics.Primitives
{
    public class VMInvokePlugin : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMInvokePluginOperand)args;
            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMInvokePluginOperand : VMPrimitiveOperand 
    {
        public byte PersonLocal;
        public byte ObjectLocal;
        public byte EventLocal; //target of event id. values go in temp0
        public bool Joinable;

        public uint PluginID;
        //sign: 0x2a6356a0
        //pizzamakerplugin: 57 174 71 234

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                PersonLocal = io.ReadByte();
                ObjectLocal = io.ReadByte();
                EventLocal = io.ReadByte();
                Joinable = io.ReadByte()>0;

                PluginID = io.ReadUInt32();
            }
        }

        public void Write(byte[] bytes) { }
        #endregion
    }
}
