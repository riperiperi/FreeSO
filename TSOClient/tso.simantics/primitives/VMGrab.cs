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
using FSO.SimAntics.Model;

namespace FSO.SimAntics.Primitives
{
    public class VMGrab : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMGrabOperand)args;

            if (context.Caller.GetSlot(0) == null)
            {
                var prevContain = context.StackObject.Container;
                if (prevContain != null)
                {
                    prevContain.ClearSlot(context.StackObject.ContainerSlot);
                }
                context.Caller.PlaceInSlot(context.StackObject, 0);

                var avatar = (VMAvatar)context.Caller;
                avatar.CarryAnimationState = new VMAnimationState(FSO.Content.Content.Get().AvatarAnimations.Get("a2o-rarm-carry-loop.anim"), false); //set default carry animation
            }
            else
                return VMPrimitiveExitCode.GOTO_FALSE;

            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMGrabOperand : VMPrimitiveOperand //empty :(
    {
        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
            }
        }
        #endregion
    }
}
