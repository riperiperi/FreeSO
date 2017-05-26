using FSO.Files.Utils;
using FSO.SimAntics.Engine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.Primitives
{
    public class VMTS1InventoryOperations : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            //todo: check condition
            var operand = (VMTS1InventoryOperationsOperand)args;

            return VMPrimitiveExitCode.GOTO_FALSE;
        }
    }

    public class VMTS1InventoryOperationsOperand : VMPrimitiveOperand
    {
        public byte Unknown;
        public byte Unknown2;
        public byte Unknown3;
        public byte Unknown4;
        public uint GUID;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                Unknown = io.ReadByte();
                Unknown2 = io.ReadByte();
                Unknown3 = io.ReadByte();
                Unknown4 = io.ReadByte();
                GUID = io.ReadUInt32();
            }
        }

        public void Write(byte[] bytes)
        {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(Unknown);
                io.Write(Unknown2);
                io.Write(Unknown3);
                io.Write(Unknown4);
                io.Write(GUID);
            }
        }
        #endregion
    }
}
