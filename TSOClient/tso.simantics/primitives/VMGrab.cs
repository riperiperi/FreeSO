using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Simantics.engine;
using TSO.Files.utils;
using TSO.Simantics.model;

namespace TSO.Simantics.primitives
{
    public class VMGrab : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            var operand = context.GetCurrentOperand<VMGrabOperand>();

            if (context.Caller.GetSlot(0) == null)
            {
                var prevContain = context.VM.GetObjectById(context.StackObject.GetValue(VMStackObjectVariable.ContainerId));
                if (prevContain != null)
                {
                    prevContain.ClearSlot(context.StackObject.GetValue(VMStackObjectVariable.SlotNumber));
                }
                context.Caller.PlaceInSlot(context.StackObject, 0);

                var avatar = (VMAvatar)context.Caller;
                avatar.CarryAnimation = TSO.Content.Content.Get().AvatarAnimations.Get("a2o-rarm-carry-loop.anim");
                avatar.CarryAnimationState = new VMAnimationState(); //set default carry animation
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
