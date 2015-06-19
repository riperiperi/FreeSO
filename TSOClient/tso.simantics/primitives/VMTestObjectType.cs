using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Simantics.engine;
using TSO.Simantics.engine.scopes;
using TSO.Files.utils;
using TSO.Simantics.engine.utils;

namespace TSO.Simantics.primitives
{
    public class VMTestObjectType : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            var operand = context.GetCurrentOperand<VMTestObjectTypeOperand>();
            var objectID = VMMemory.GetVariable(context, operand.IdOwner, operand.IdData);

            var obj = context.VM.GetObjectById(objectID);
            //var obj = context.StackObject;
            if (obj == null){
                return VMPrimitiveExitCode.ERROR;
            }

            //What's the point of this statement?
            //if (operand.GUID == 0xDC6D7898) operand = operand;

            if (obj.Object.GUID == operand.GUID) return VMPrimitiveExitCode.GOTO_TRUE; //is my guid same?
            else if (obj.MasterDefinition != null && (obj.MasterDefinition.GUID == operand.GUID)) return VMPrimitiveExitCode.GOTO_TRUE; //is master guid same?
            else return VMPrimitiveExitCode.GOTO_FALSE;
        }
    }

    public class VMTestObjectTypeOperand : VMPrimitiveOperand
    {
        public uint GUID;
        public ushort IdData;
        public VMVariableScope IdOwner;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes){
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN)){
                GUID = io.ReadUInt32();
                IdData = io.ReadUInt16();
                IdOwner = (VMVariableScope)io.ReadByte();
            }
        }
        #endregion
    }
}
