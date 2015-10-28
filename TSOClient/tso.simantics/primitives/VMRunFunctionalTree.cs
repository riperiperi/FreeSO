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
    //See VMFindBestObjectForFunction for function map table.

    public class VMRunFunctionalTree : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMRunFunctionalTreeOperand)args;

            var entry = VMFindBestObjectForFunction.FunctionToEntryPoint[operand.Function];
            var ent = context.StackObject;
            if (ent.EntryPoints[entry].ActionFunction != 0)
            {
                bool Execute;
                if (ent.EntryPoints[entry].ConditionFunction != 0) //check if we can definitely execute this...
                {
                    var Behavior = ent.GetBHAVWithOwner(ent.EntryPoints[entry].ConditionFunction, context.VM.Context);
                    Execute = (VMThread.EvaluateCheck(context.VM.Context, context.Caller, new VMQueuedAction()
                    {
                        Callee = ent,
                        CodeOwner = Behavior.owner,
                        StackObject = ent,
                        Routine = context.VM.Assemble(Behavior.bhav),
                    }) == VMPrimitiveExitCode.RETURN_TRUE);

                }
                else
                {
                    Execute = true;
                }

                if (Execute)
                {
                    //push it onto our stack, except now the stack object owns our soul!
                    var Behavior = ent.GetBHAVWithOwner(ent.EntryPoints[entry].ActionFunction, context.VM.Context);
                    var routine = context.VM.Assemble(Behavior.bhav);
                    var childFrame = new VMStackFrame
                    {
                        Routine = routine,
                        Caller = context.Caller,
                        Callee = ent,
                        CodeOwner = Behavior.owner,
                        StackObject = ent
                    };
                    if (operand.Flags > 0) context.Thread.Queue[0].IconOwner = context.StackObject;
                    childFrame.Args = new short[routine.Arguments];
                    context.Thread.Push(childFrame);
                    return VMPrimitiveExitCode.CONTINUE;
                }
                else
                {
                    return VMPrimitiveExitCode.GOTO_FALSE;
                }
            }
            else
            {
                return VMPrimitiveExitCode.GOTO_FALSE;
            }
        }
    }

    public class VMRunFunctionalTreeOperand : VMPrimitiveOperand
    {
        public ushort Function;
        public byte Flags; //only flag is 1: change icon

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                Function = io.ReadUInt16();
                Flags = io.ReadByte();
            }
        }

        public void Write(byte[] bytes) {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(Function);
                io.Write(Flags);
            }
        }
        #endregion
    }

}
