/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.SimAntics.Engine;
using FSO.Files.Utils;
using FSO.Files.Formats.IFF.Chunks;
using FSO.Content;

namespace FSO.SimAntics.Engine.Primitives
{
    public class VMPushInteraction : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMPushInteractionOperand)args;
            VMEntity interactionSource;

            if ((operand.Flags & (1 << 1)) > 0) interactionSource = context.VM.GetObjectById((short)context.Locals[operand.ObjectLocation]);
            else interactionSource = context.VM.GetObjectById((short)context.Args[operand.ObjectLocation]);

            VMQueuePriority priority;
            if (operand.Priority == 0)
            {
                priority = context.Thread.Queue[0].Priority;
            }
            else
            {
                priority = (VMQueuePriority)(operand.Priority - 1);
            }

            BHAV bhav;
            GameIffResource CodeOwner = null;
            var Action = interactionSource.TreeTable.InteractionByIndex[operand.Interaction];
            ushort ActionID = Action.ActionFunction;

            CodeOwner = interactionSource.Object.Resource;
            if (ActionID < 4096)
            { //global
                bhav = null;
                //unimp as it has to access the context to get this.
            }
            else if (ActionID < 8192)
            { //local
                bhav = interactionSource.Object.Resource.Get<BHAV>(ActionID);
            }
            else
            { //semi-global
                bhav = interactionSource.SemiGlobal.Resource.Get<BHAV>(ActionID);
            } 

            var routine = context.VM.Assemble(bhav);
            context.StackObject.Thread.EnqueueAction(
                new FSO.SimAntics.Engine.VMQueuedAction
                {
                    Callee = interactionSource,
                    CodeOwner = CodeOwner,
                    Routine = routine,
                    Name = interactionSource.TreeTableStrings.GetString((int)Action.TTAIndex),
                    StackObject = interactionSource,
                    InteractionNumber = operand.Interaction,
                    Priority = priority
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
