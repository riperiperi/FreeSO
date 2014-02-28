using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.files.utils;
using tso.simantics.engine.scopes;
using tso.simantics.engine.utils;
using tso.simantics;
using tso.files.formats.iff.chunks;

namespace tso.simantics.engine.primitives
{
    public class VMRunTreeByName : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            var operand = context.GetCurrentOperand<VMRunTreeByNameOperand>();

            string name;

            if (operand.StringScope == 1)
            {//global
                name = context.Global.Resource.Get<STR>(operand.StringTable).GetString(operand.StringID-1);
            }
            else
            {//local
                name = context.CodeOwner.Get<STR>(operand.StringTable).GetString(operand.StringID-1);
            }

            if (context.Callee.TreeByName.ContainsKey(name))
            {
                var tree = context.Callee.TreeByName[name];
                //found it! now lets call the tree ;)
            }
            else
            {
                return VMPrimitiveExitCode.GOTO_FALSE;
            }

            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMRunTreeByNameOperand : VMPrimitiveOperand
    {
        public ushort StringTable;
        public byte StringScope;
        public byte Unused;
        public byte StringID;
        public byte Destination;

        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                StringTable = io.ReadUInt16();
                StringScope = io.ReadByte();
                Unused = io.ReadByte();
                StringID = io.ReadByte();
                Destination = io.ReadByte();
            }
        }
    }
}
