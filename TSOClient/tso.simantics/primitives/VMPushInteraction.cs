using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.simantics.engine;
using tso.files.utils;
using tso.files.formats.iff.chunks;
using tso.content;

namespace tso.simantics.engine.primitives
{
    public class VMPushInteraction : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            var operand = context.GetCurrentOperand<VMPushInteractionOperand>();
            VMEntity interactionSource;

            if ((operand.Flags & (1 << 1)) > 0) interactionSource = context.VM.GetObjectById((short)context.Locals[operand.ObjectLocation]);
            else interactionSource = context.VM.GetObjectById((short)context.Args[operand.ObjectLocation]);


            BHAV bhav;
            GameIffResource CodeOwner = null;
            var Action = interactionSource.TreeTable.InteractionByIndex[operand.Interaction];
            ushort ActionID = Action.ActionFunction;

            if (ActionID < 4096)
            { //global
                bhav = null;
                //unimp as it has to access the context to get this.
            }
            else if (ActionID < 8192)
            { //local
                bhav = interactionSource.Object.Resource.Get<BHAV>(ActionID);
                CodeOwner = interactionSource.Object.Resource;
            }
            else
            { //semi-global
                bhav = interactionSource.SemiGlobal.Resource.Get<BHAV>(ActionID);
                CodeOwner = interactionSource.SemiGlobal.Resource;
            } 

            var routine = context.VM.Assemble(bhav);
            context.StackObject.Thread.EnqueueAction(
                new tso.simantics.engine.VMQueuedAction
                {
                    Callee = interactionSource,
                    CodeOwner = CodeOwner,
                    Routine = routine,
                    Name = interactionSource.TreeTableStrings.GetString((int)Action.TTAIndex),
                    StackObject = interactionSource,
                    InteractionNumber = operand.Interaction
                }
            );

            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMPushInteractionOperand : VMPrimitiveOperand
    {
        public byte Interaction;
        public byte ObjectLocation;
        public byte Priority;
        public byte Flags;
        public byte IconLocation;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                Interaction = io.ReadByte();
                ObjectLocation = io.ReadByte();
                Priority = io.ReadByte();
                Flags = io.ReadByte();
                IconLocation = io.ReadByte();
            }
        }
        #endregion
    }
}
