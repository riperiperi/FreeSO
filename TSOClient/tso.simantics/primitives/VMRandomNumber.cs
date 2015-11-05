/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Files.Utils;
using FSO.SimAntics.Engine.Scopes;
using FSO.SimAntics.Engine.Utils;
using System.IO;

namespace FSO.SimAntics.Engine.Primitives
{
    public class VMRandomNumber : VMPrimitiveHandler {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMRandomNumberOperand)args;
            var rangeValue = (ushort)VMMemory.GetVariable(context, operand.RangeScope, operand.RangeData);

            if (context.Routine.Rti.Name.Trim('\0') == "spread - m") { }
            var result = context.VM.Context.NextRandom(rangeValue);
            VMMemory.SetVariable(context, operand.DestinationScope, operand.DestinationData, (short)result);
            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMRandomNumberOperand : VMPrimitiveOperand
    {
        public ushort DestinationData;
        public VMVariableScope DestinationScope;
        public ushort RangeData;
        public VMVariableScope RangeScope;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes){
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN)){
                DestinationData = io.ReadUInt16();
                DestinationScope = (VMVariableScope)io.ReadUInt16();
                RangeData = io.ReadUInt16();
                RangeScope = (VMVariableScope)io.ReadUInt16();
            }
        }

        public void Write(byte[] bytes) {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(DestinationData);
                io.Write((ushort)DestinationScope);
                io.Write(RangeData);
                io.Write((ushort)RangeScope);
            }
        }
        #endregion
    }
}
