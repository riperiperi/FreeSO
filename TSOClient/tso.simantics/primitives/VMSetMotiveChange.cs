using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Simantics.engine;
using TSO.Files.utils;
using TSO.Simantics.engine.scopes;
using TSO.Simantics.model;

namespace TSO.Simantics.primitives
{
    public class VMSetMotiveChange : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            var operand = context.GetCurrentOperand<VMSetMotiveChangeOperand>();
            //TODO: Implement this :)
            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMSetMotiveChangeOperand : VMPrimitiveOperand {

        public VMVariableScope DeltaOwner;
        public ushort DeltaData;

        public VMVariableScope MaxOwner;
        public ushort MaxData;

        public VMSetMotiveChangeFlags Flags;
        public VMMotive Motive;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes){
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN)){

                DeltaOwner = (VMVariableScope)io.ReadByte();
                MaxOwner = (VMVariableScope)io.ReadByte();
                Motive = (VMMotive)io.ReadByte();
                Flags = (VMSetMotiveChangeFlags)io.ReadByte();

                DeltaData = io.ReadUInt16();
                MaxData = io.ReadUInt16();
            }
        }
        #endregion
    }

    [Flags]
    public enum VMSetMotiveChangeFlags {
        ClearAll = 1
    }
}
