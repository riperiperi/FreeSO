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
    //See VMFindBestObjectForFunction for function map table.

    public class VMRunFunctionalTree : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            var operand = context.GetCurrentOperand<VMRunFunctionalTreeOperand>();

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
        #endregion
    }

}
