using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Files.utils;
using TSO.Simantics.engine.scopes;
using TSO.Simantics.engine.utils;
using TSO.Vitaboy;
using TSO.Simantics.model;
using TSO.Simantics.utils;

namespace TSO.Simantics.engine.primitives
{
    public class VMAnimateSim : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            var operand = context.GetCurrentOperand<VMAnimateSimOperand>();

            var avatar = (VMAvatar)context.Caller;
            if (operand.AnimationID == 0)
            { //reset
                avatar.CurrentAnimation = null;
                if (avatar.GetSlot(0) != null) //if we're carrying something, set carry animation to default carry.
                {
                    avatar.CarryAnimation = TSO.Content.Content.Get().AvatarAnimations.Get("a2o-rarm-carry-loop.anim");
                    avatar.CarryAnimationState = new VMAnimationState();
                }
                else avatar.CarryAnimation = null;
                return VMPrimitiveExitCode.GOTO_TRUE;
            }

            var animation = VMMemory.GetAnimation(context, operand.Source, operand.AnimationID);
            if(animation == null){
                return VMPrimitiveExitCode.ERROR;
            }

            if (operand.Mode == 2) //stop standard carry, then play and wait
                avatar.CarryAnimation = null;

            if (operand.Mode == 0 || operand.Mode == 2) //Play and Wait
            {
                /** Are we starting the animation or progressing it? **/
                if (avatar.CurrentAnimation == null || avatar.CurrentAnimation != animation)
                {

                    /** Start it **/
                    avatar.CurrentAnimation = animation;
                    avatar.CurrentAnimationState = new VMAnimationState();
                    avatar.Avatar.LeftHandGesture = SimHandGesture.Idle;
                    avatar.Avatar.RightHandGesture = SimHandGesture.Idle;

                    if (operand.PlayBackwards)
                    {
                        avatar.CurrentAnimationState.PlayingBackwards = true;
                        avatar.CurrentAnimationState.CurrentFrame = avatar.CurrentAnimation.NumFrames;
                    }

                    foreach (var motion in animation.Motions)
                    {
                        if (motion.TimeProperties == null) { continue; }

                        foreach (var tp in motion.TimeProperties)
                        {
                            foreach (var item in tp.Items)
                            {
                                avatar.CurrentAnimationState.TimePropertyLists.Add(item);
                            }
                        }
                    }

                    /** Sort time property lists by time **/
                    avatar.CurrentAnimationState.TimePropertyLists.Sort(new TimePropertyListItemSorter());
                    return VMPrimitiveExitCode.CONTINUE_NEXT_TICK;
                }
                else
                {
                    if (avatar.CurrentAnimationState.EndReached)
                    {
                        avatar.CurrentAnimation = null;
                        return VMPrimitiveExitCode.GOTO_TRUE;
                    }
                    else if (avatar.CurrentAnimationState.EventFired)
                    {

                        avatar.CurrentAnimationState.EventFired = false; //clear fired flag
                        if (operand.StoreFrameInLocal)
                        {
                            VMMemory.SetVariable(context, VMVariableScope.Local, operand.LocalEventNumber, avatar.CurrentAnimationState.EventCode);
                        }
                        else
                        {
                            VMMemory.SetVariable(context, VMVariableScope.Parameters, 0, avatar.CurrentAnimationState.EventCode);
                        }
                        return VMPrimitiveExitCode.GOTO_FALSE;

                    }
                    else
                    {
                        return VMPrimitiveExitCode.CONTINUE_NEXT_TICK;
                    }
                }
            }
            else if (operand.Mode == 3) //set custom carry animation
            {
                avatar.CarryAnimation = animation;
                avatar.CarryAnimationState = new VMAnimationState();
                return VMPrimitiveExitCode.GOTO_TRUE;
            }
            else throw new Exception("Unknown animation mode!");
        }
    }

    public class VMAnimateSimOperand : VMPrimitiveOperand {
        public ushort AnimationID;
        public byte LocalEventNumber;
        public byte _pad;
        public VMAnimationScope Source;
        public byte Flags;
        public byte ExpectedEventCount;

        #region VMPrimitiveOperand Members

        public void Read(byte[] bytes){
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN)){
                AnimationID = io.ReadUInt16();
                LocalEventNumber = io.ReadByte();
                _pad = io.ReadByte();
                Source = (VMAnimationScope)io.ReadByte();
                Flags = io.ReadByte();
                ExpectedEventCount = io.ReadByte();
            }
        }

        #endregion

        public bool StoreFrameInLocal
        {
            get
            {
                return (Flags & 32) == 32;
            }
        }

        public bool PlayBackwards
        {
            get
            {
                return (Flags & 2) == 2;
            }
        }

        public byte Mode
        {
            get
            {
                return (byte)((Flags >> 3) & 3);
            }
        }

        public override string ToString(){
            return "Animate Sim (id " + AnimationID + " from " + Source.ToString() + ")";
        }
    }
}
