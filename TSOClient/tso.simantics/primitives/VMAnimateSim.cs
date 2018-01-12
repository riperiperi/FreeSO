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
using System.IO;

namespace FSO.SimAntics.Engine.Primitives
{
    public class VMAnimateSim : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMAnimateSimOperand)args;
            var avatar = (VMAvatar)context.Caller;

            Animation animation = null;
            var id = (operand.IDFromParam) ? (ushort)(context.Args[operand.AnimationID]) : operand.AnimationID;

            if (id == 0)
            { //reset
                avatar.Animations.Clear();
                var posture = avatar.GetPersonData(VMPersonDataVariable.Posture);

                if (posture != 1 && posture != 2) posture = 3; //sit and kneel are 1 and 2, 0 is stand but in walk animations it's 3.
                //todo: swimming??

                animation = FSO.Content.Content.Get().AvatarAnimations.Get(avatar.WalkAnimations[posture] + ".anim");
                if (animation == null) return VMPrimitiveExitCode.GOTO_TRUE;
                var state = new VMAnimationState(animation, operand.PlayBackwards);
                state.Loop = true;
                avatar.Animations.Add(state);

                if (avatar.GetSlot(0) != null) //if we're carrying something, set carry animation to default carry.
                {
                    if (avatar.CarryAnimationState == null)
                        avatar.CarryAnimationState = new VMAnimationState(FSO.Content.Content.Get().AvatarAnimations.Get("a2o-rarm-carry-loop.anim"), false);
                }
                else avatar.CarryAnimationState = null;
                return VMPrimitiveExitCode.GOTO_TRUE;
            }
            var source = operand.Source;
            if (operand.IDFromParam && source == VMAnimationScope.Object) source = VMAnimationScope.StackObject; //fixes MM rollercoaster
            if (!operand.IDFromParam) {
                if (operand.AnimationCache == null)
                    operand.AnimationCache = VMMemory.GetAnimation(context, source, id);
                animation = operand.AnimationCache;
            } else
            {
                animation = VMMemory.GetAnimation(context, source, id);
            }

            if (animation == null){
                return VMPrimitiveExitCode.GOTO_TRUE_NEXT_TICK;
            }

            if (operand.Mode == 3) //stop standard carry, then play and wait
                avatar.CarryAnimationState = null;

            if (operand.Mode == 0 || operand.Mode == 3) //Play and Wait
            {
                /** Are we starting the animation or progressing it? **/
                if (avatar.CurrentAnimationState == null || avatar.CurrentAnimationState.Anim != animation)
                {
                    /** Start it **/
                    avatar.Animations.Clear();
                    avatar.Animations.Add(new VMAnimationState(animation, operand.PlayBackwards));

                    avatar.Avatar.LeftHandGesture = SimHandGesture.Idle;
                    avatar.Avatar.RightHandGesture = SimHandGesture.Idle;
                    return VMPrimitiveExitCode.CONTINUE_NEXT_TICK;
                }
                else
                {
                    var cAnim = avatar.CurrentAnimationState;

                    //SPECIAL CASE: if we are ending the animation, and the number of events run < expected events
                    //forcefully run those events, with id as their event number. (required for bath drain)
                    if (cAnim.EndReached)
                    {
                        while (cAnim.EventsRun < operand.ExpectedEventCount)
                        {
                            cAnim.EventQueue.Add(cAnim.EventsRun++);
                        }
                    }

                    if (cAnim.EventQueue.Count > 0) //favor events over end. do not want to miss any.
                    {
                        var code = cAnim.EventQueue[0];
                        cAnim.EventQueue.RemoveAt(0);
                        if (operand.StoreFrameInLocal)
                            VMMemory.SetVariable(context, VMVariableScope.Local, operand.LocalEventNumber, code);
                        else
                            VMMemory.SetVariable(context, VMVariableScope.Parameters, 0, code);
                        return VMPrimitiveExitCode.GOTO_FALSE;
                    }
                    else if (cAnim.EndReached)
                    {
                        avatar.Animations.Clear();
                        return VMPrimitiveExitCode.GOTO_TRUE;
                    }
                    else
                    {
                        return VMPrimitiveExitCode.CONTINUE_NEXT_TICK;
                    }
                }
            }
            else if (operand.Mode == 2) //set custom carry animation
            {
                avatar.CarryAnimationState = new VMAnimationState(animation, false);
                return VMPrimitiveExitCode.GOTO_TRUE;
            }
            else return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMAnimateSimOperand : VMPrimitiveOperand {
        private ushort _AnimationID;
        public ushort AnimationID
        {
            get
            {
                return _AnimationID;
            }
            set
            {
                _AnimationID = value;
                AnimationCache = null;
            }
        }
        public byte LocalEventNumber { get; set; }
        public byte _pad;
        public VMAnimationScope Source { get; set; }
        public byte Flags;
        public byte ExpectedEventCount { get; set; }

        public Animation AnimationCache;

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

        public void Write(byte[] bytes) {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(AnimationID);
                io.Write(LocalEventNumber);
                io.Write((byte)0);
                io.Write((byte)Source);
                io.Write(Flags);
                io.Write(ExpectedEventCount);
            }
        }

        #endregion

        public bool StoreFrameInLocal
        {
            get
            {
                return (Flags & 32) == 32;
            }
            set
            {
                if (value) Flags |= 32;
                else Flags &= unchecked((byte)~32);
            }
        }

        public bool PlayBackwards
        {
            get
            {
                return (Flags & 2) == 2;
            }
            set
            {
                if (value) Flags |= 2;
                else Flags &= unchecked((byte)~2);
            }
        }

        public bool IDFromParam
        {
            get
            {
                return (Flags & 4) == 4;
            }
            set
            {
                if (value) Flags |= 4;
                else Flags &= unchecked((byte)~4);
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
