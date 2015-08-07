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
using FSO.Vitaboy;
using FSO.SimAntics.Model;
using FSO.SimAntics.Utils;

namespace FSO.SimAntics.Engine.Primitives
{
    public class VMAnimateSim : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMAnimateSimOperand)args;

            var avatar = (VMAvatar)context.Caller;

            if (operand.AnimationID == 0)
            { //reset
                avatar.CurrentAnimation = null;
                if (avatar.GetSlot(0) != null) //if we're carrying something, set carry animation to default carry.
                {
                    avatar.CarryAnimation = FSO.Content.Content.Get().AvatarAnimations.Get("a2o-rarm-carry-loop.anim");
                    avatar.CarryAnimationState = new VMAnimationState();
                }
                else avatar.CarryAnimation = null;
                return VMPrimitiveExitCode.GOTO_TRUE;
            }

            var animation = VMMemory.GetAnimation(context, operand.Source, operand.AnimationID);
            if (animation == null){
                return VMPrimitiveExitCode.GOTO_TRUE;
            }

            if (operand.Mode == 3) //stop standard carry, then play and wait
                avatar.CarryAnimation = null;

            if (operand.Mode == 0 || operand.Mode == 3) //Play and Wait
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
            else if (operand.Mode == 2) //set custom carry animation
            {
                avatar.CarryAnimation = animation;
                avatar.CarryAnimationState = new VMAnimationState();
                return VMPrimitiveExitCode.GOTO_TRUE;
            }
            else return VMPrimitiveExitCode.GOTO_TRUE;
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
            //Mode 0: Play and Wait
            //Mode 1: ??
            //Mode 2: Stop standard carry, play and wait
            //Mode 3: ??

            get
            {
                return (byte)((Flags&1) | ((Flags >> 3) & 2));
            }
        }

        public override string ToString(){
            return "Animate Sim (id " + AnimationID + " from " + Source.ToString() + ")";
        }
    }
}
