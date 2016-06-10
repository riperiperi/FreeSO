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

            if ((operand.Flags & (1 << 1)) > 0) interactionSource = context.VM.GetObjectById((short)context.Locals[operand.ObjectLocation]);
            else interactionSource = context.VM.GetObjectById((short)context.Args[operand.ObjectLocation]);

            short priority = 0;
            VMQueueMode mode = VMQueueMode.Normal;
            switch (operand.Priority)
            {
                case VMPushPriority.Inherited:
                    short oldPrio = 1;
                    if (context.ActionTree) oldPrio = context.Thread.Queue[0].Priority;
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

            var action = interactionSource.GetAction(operand.Interaction, context.StackObject, context.VM.Context);
            if (action == null) return VMPrimitiveExitCode.GOTO_FALSE;
            if (operand.UseCustomIcon) action.IconOwner = context.VM.GetObjectById((short)context.Locals[operand.IconLocation]);
            action.Mode = mode;
            action.Priority = priority;
            action.Flags |= TTABFlags.MustRun;
            if (operand.PushHeadContinuation) action.Flags |= TTABFlags.Leapfrog;

            context.StackObject.Thread.EnqueueAction(action);

            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMPushInteractionOperand : VMPrimitiveOperand
    {
        public byte Interaction;
        public byte ObjectLocation;
        public VMPushPriority Priority;
        public byte Flags;
        public byte IconLocation;

        public bool UseCustomIcon
        {
            get { return (Flags & 1) > 0; }
        }

        public bool PushHeadContinuation
        {
            get { return (Flags & 4) > 0; }
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
