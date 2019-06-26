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
using System.IO;

namespace FSO.SimAntics.Engine.Primitives
{
    public class VMPushInteraction : VMPrimitiveHandler
    {


        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMPushInteractionOperand)args;
            VMEntity interactionSource;

            if (operand.ObjectInLocal) interactionSource = context.VM.GetObjectById((short)context.Locals[operand.ObjectLocation]);
            else interactionSource = context.VM.GetObjectById((short)context.Args[operand.ObjectLocation]);

            short priority = 0;
            VMQueueMode mode = VMQueueMode.Normal;
            switch (operand.Priority)
            {
                case VMPushPriority.Inherited:
                    short oldPrio = 1;
                    if (context.ActionTree) oldPrio = context.Thread.ActiveAction.Priority;
                    priority = Math.Max((short)1, oldPrio); break;
                case VMPushPriority.Maximum:
                    priority = (short)VMQueuePriority.Maximum; break;
                case VMPushPriority.Autonomous:
                    priority = (short)VMQueuePriority.Autonomous; break;
                case VMPushPriority.UserDriven:
                    priority = (short)VMQueuePriority.UserDriven; break;
                case VMPushPriority.ParentIdle:
                    priority = (short)VMQueuePriority.ParentIdle; mode = VMQueueMode.ParentIdle; break;
                case VMPushPriority.ParentExit:
                    priority = (short)VMQueuePriority.ParentExit; mode = VMQueueMode.ParentExit; break;
                case VMPushPriority.Idle:
                    priority = (short)VMQueuePriority.Idle; mode = VMQueueMode.Idle; break;
            }

            var action = interactionSource.GetAction(operand.Interaction, context.StackObject, context.VM.Context, false);
            if (action == null) return VMPrimitiveExitCode.GOTO_FALSE;
            if (operand.UseCustomIcon) action.IconOwner = context.VM.GetObjectById((short)context.Locals[operand.IconLocation]);
            action.Mode = mode;
            action.Priority = priority;
            action.Flags |= TTABFlags.FSOSkipPermissions;
            if (operand.PushTailContinuation) action.Flags |= TTABFlags.FSOPushTail;
            if (operand.PushHeadContinuation) action.Flags |= TTABFlags.FSOPushHead;

            context.StackObject.Thread.EnqueueAction(action);
            if (context.StackObject is VMAvatar && context.Caller is VMAvatar && context.StackObject != context.Caller)
            {
                //if this is an interaction between two sims, and this interaction is being pushed onto someone else,
                //show the interaction result chooser for that sim immediately, rather than force them to wait.
                //(erroneously shows up for "talk to" and "whisper to". there may be a better way to do this.
                action.InteractionResult = 0;
            }

            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMPushInteractionOperand : VMPrimitiveOperand
    {
        public byte Interaction { get; set; }
        public byte ObjectLocation { get; set; }
        public VMPushPriority Priority { get; set; }
        public byte Flags;
        public byte IconLocation { get; set; }

        public bool UseCustomIcon
        {
            get { return (Flags & 1) > 0; }
            set
            {
                if (value) Flags |= 1;
                else Flags &= unchecked((byte)~1);
            }
        }

        public bool ObjectInLocal
        {
            get { return (Flags & 2) > 0; }
            set
            {
                if (value) Flags |= 2;
                else Flags &= unchecked((byte)~2);
            }
        }

        public bool PushHeadContinuation
        {
            get { return (Flags & 4) > 0; }
            set
            {
                if (value) Flags |= 4;
                else Flags &= unchecked((byte)~4);
            }
        }

        public bool PushTailContinuation
        {
            get { return (Flags & 128) > 0; }
            set
            {
                if (value) Flags |= 128;
                else Flags &= unchecked((byte)~128);
            }
        }

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                Interaction = io.ReadByte();
                ObjectLocation = io.ReadByte();
                Priority = (VMPushPriority)io.ReadByte();
                Flags = io.ReadByte();
                IconLocation = io.ReadByte();
            }
        }

        public void Write(byte[] bytes) {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(Interaction);
                io.Write(ObjectLocation);
                io.Write((byte)Priority);
                io.Write(Flags);
                io.Write(IconLocation);
            }
        }
        #endregion
    }

    public enum VMPushPriority : byte
    {
        Inherited = 0,
        Maximum = 1,
        Autonomous = 2,
        UserDriven = 3,
        ParentIdle = 4,
        ParentExit = 5,
        Idle = 6
    }
}
