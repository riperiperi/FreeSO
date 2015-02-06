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
using TSO.Simantics.engine;

namespace TSO.Simantics.primitives
{
    public class VMReach : VMPrimitiveHandler
    {
        public bool failed = false;

        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            var operand = context.GetCurrentOperand<VMReachOperand>();

            int height;

            if (operand.Mode == 0)
            { //reach to stack object
                height = (int)Math.Round(context.StackObject.WorldUI.Position.Z*4); //todo, factor in second floor by making height differential to sim height
            }
            else if (operand.Mode == 1)
            {
                var slotNum = context.Args[operand.SlotParam];
                var slot = context.StackObject.Slots.Slots[0][slotNum];
                if (slot != null)
                {
                    height = (int)Math.Round((slot.Offset.Z == 0) ? slot.Height : slot.Offset.Z);
                }
                else return VMPrimitiveExitCode.GOTO_FALSE;
            }
            else
            {
                //reach to mouth is unimplemented so no, also none others exist after
                throw new VMSimanticsException("Reach to mouth not implemented!", context);
            }

            string animationName;
            if (height < 2) animationName = "a2o-reach-floorht.anim";
            else if (height < 4) animationName = "a2o-reach-seatht.anim";
            else animationName = "a2o-reach-tableht.anim";

            var animation = TSO.Content.Content.Get().AvatarAnimations.Get(animationName);
            if(animation == null){
                return VMPrimitiveExitCode.ERROR;
            }
            var avatar = (VMAvatar)context.Caller;
            
            /** Are we starting the animation or progressing it? **/
            if (avatar.CurrentAnimation == null || avatar.CurrentAnimation != animation)
            { //start the grab!

                /** Start it **/
                avatar.CurrentAnimation = animation;
                avatar.CurrentAnimationState = new VMAnimationState();
                avatar.Avatar.LeftHandGesture = SimHandGesture.Idle;
                avatar.Avatar.RightHandGesture = SimHandGesture.Idle;
                failed = false;

                foreach (var motion in animation.Motions){
                    if (motion.TimeProperties == null) { continue; }

                    foreach(var tp in motion.TimeProperties){
                        foreach (var item in tp.Items){
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
                    if (failed) return VMPrimitiveExitCode.GOTO_FALSE;
                    return VMPrimitiveExitCode.GOTO_TRUE;
                } 
                else if (avatar.CurrentAnimationState.EventFired)
                {

                    if (avatar.CurrentAnimationState.EventCode == 0)
                    {
                        //do the grab/drop
                        if (operand.Mode == 0)
                        { //pick up stack object. no drop condition
                            if (context.Caller.GetSlot(0) == null)
                            {
                                var prevContain = context.VM.GetObjectById(context.StackObject.GetValue(VMStackObjectVariable.ContainerId));
                                if (prevContain != null)
                                {
                                    prevContain.ClearSlot(context.StackObject.GetValue(VMStackObjectVariable.SlotNumber));
                                }
                                context.Caller.PlaceInSlot(context.StackObject, 0);

                                avatar.CarryAnimation = TSO.Content.Content.Get().AvatarAnimations.Get("a2o-rarm-carry-loop.anim");
                                avatar.CarryAnimationState = new VMAnimationState(); //set default carry animation
                            }
                            else
                            {
                                failed = true;
                            }
                        }
                        else if (operand.Mode == 1)
                        { //grab or drop, depending on if we're holding something
                            var holding = context.Caller.GetSlot(0);
                            var slotNum = context.Args[operand.SlotParam];

                            if (holding == null)
                            { //grab
                                var item = context.StackObject.GetSlot(slotNum);
                                if (item != null)
                                {
                                    context.StackObject.ClearSlot(slotNum);
                                    context.Caller.PlaceInSlot(item, 0);

                                    avatar.CarryAnimation = TSO.Content.Content.Get().AvatarAnimations.Get("a2o-rarm-carry-loop.anim");
                                    avatar.CarryAnimationState = new VMAnimationState(); //set default carry animation
                                }
                                else failed = true; //can't grab from an empty space
                            }
                            else //drop
                            {
                                var itemTest = context.StackObject.GetSlot(slotNum);
                                if (itemTest == null)
                                {
                                    context.Caller.ClearSlot(0);
                                    context.StackObject.PlaceInSlot(holding, slotNum);

                                    avatar.CarryAnimation = null;
                                }
                                else failed = true; //can't drop in an occupied space
                            }
                        }
                    }

                    avatar.CurrentAnimationState.EventFired = false; //clear fired flag
                    return VMPrimitiveExitCode.CONTINUE_NEXT_TICK;
                }
                else
                {
                    return VMPrimitiveExitCode.CONTINUE_NEXT_TICK;
                }
            }
        }
    }

    public class VMReachOperand : VMPrimitiveOperand
    {
        public ushort Mode;
        public ushort GrabOrDrop;
        public ushort SlotParam;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                Mode = io.ReadUInt16();
                GrabOrDrop = io.ReadUInt16();
                SlotParam = io.ReadUInt16();
            }
        }
        #endregion
    }
}
