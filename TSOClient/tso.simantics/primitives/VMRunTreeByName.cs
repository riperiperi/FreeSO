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
using FSO.SimAntics;
using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.Primitives;
using System.IO;

namespace FSO.SimAntics.Engine.Primitives
{
    public class VMRunTreeByName : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMRunTreeByNameOperand)args;
            if (context.StackObject == null) return VMPrimitiveExitCode.GOTO_FALSE;

            string name;
            STR res = null;
            if (operand.StringScope == 1)
            {//global
                res = context.Global.Resource.Get<STR>(operand.StringTable);
            }
            else
            {//local
                if (context.Routine.ID >= 8192 && context.ScopeResource.SemiGlobal != null) res = context.ScopeResource.SemiGlobal.Get<STR>(operand.StringTable);
                if (res == null) res = context.ScopeResource.Get<STR>(operand.StringTable); 
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

        public void Write(byte[] bytes) {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(StringTable);
                io.Write(StringScope);
                io.Write(Unused);
                io.Write(StringID);
                io.Write(Destination);
            }
        }
    }
}
