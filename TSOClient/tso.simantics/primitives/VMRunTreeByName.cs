using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Files.utils;
using TSO.Simantics.engine.scopes;
using TSO.Simantics.engine.utils;
using TSO.Simantics;
using TSO.Files.formats.iff.chunks;
using TSO.Simantics.primitives;

namespace TSO.Simantics.engine.primitives
{
    public class VMRunTreeByName : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            var operand = context.GetCurrentOperand<VMRunTreeByNameOperand>();

            string name;
            STR res = null;
            if (operand.StringScope == 1)
            {//global
                res = context.Global.Resource.Get<STR>(operand.StringTable);
            }
            else
            {//local
                if (context.Routine.ID >= 8192 && context.CodeOwner.SemiGlobal != null) res = context.CodeOwner.SemiGlobal.Get<STR>(operand.StringTable);
                if (res == null) res = context.CodeOwner.Get<STR>(operand.StringTable); 
            }
            if (res == null) return VMPrimitiveExitCode.GOTO_FALSE;
            name = res.GetString(operand.StringID-1);

            if (context.StackObject.TreeByName == null) return VMPrimitiveExitCode.GOTO_FALSE;
            if (context.StackObject.TreeByName.ContainsKey(name))
            {
                var tree = context.StackObject.TreeByName[name];

                if (operand.Destination == 2)
                {
                    context.Thread.ExecuteSubRoutine(context, tree.bhav, tree.Owner, new VMSubRoutineOperand(context.Thread.TempRegisters));
                    return VMPrimitiveExitCode.CONTINUE;
                    //push onto my stack - acts like a subroutine.
                }
                else if (operand.Destination == 0)
                {
                    return context.Caller.Thread.RunInMyStack(tree.bhav, tree.Owner, context.Thread.TempRegisters, context.StackObject)
                        ? VMPrimitiveExitCode.GOTO_TRUE : VMPrimitiveExitCode.GOTO_FALSE;
                    //run in my stack
                }
                else
                {
                    return context.StackObject.Thread.RunInMyStack(tree.bhav, tree.Owner, context.Thread.TempRegisters, context.StackObject)
                        ? VMPrimitiveExitCode.GOTO_TRUE : VMPrimitiveExitCode.GOTO_FALSE;
                    //run in stack obj's stack
                }
                //found it! now lets call the tree ;)
            }
            else
            {
                return VMPrimitiveExitCode.GOTO_FALSE;
            }
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
