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
using FSO.Content;

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

            var newMode = true; // (context.VM.Tuning?.GetTuning("feature", 0, 0) ?? 0) != 0; //might need to disable this suddenly - too many things to test

            if (id == 0)
            { //reset
                if (operand.Mode == 3)
                {
                    avatar.CarryAnimationState = null;
                    return VMPrimitiveExitCode.GOTO_TRUE;
                }
                if (avatar.GetPersonData(VMPersonDataVariable.HeadSeekState) == 1) avatar.SetPersonData(VMPersonDataVariable.HeadSeekState, 4);
                avatar.Animations.Clear();
                var posture = avatar.GetPersonData(VMPersonDataVariable.Posture);

                if (posture != 1 && posture != 2) posture = 3; //sit and kneel are 1 and 2, 0 is stand but in walk animations it's 3.
                //todo: swimming??

                animation = FSO.Content.Content.Get().AvatarAnimations.Get(avatar.WalkAnimations[posture] + ".anim");
                if (animation == null) return VMPrimitiveExitCode.GOTO_TRUE;
                var state = new VMAnimationState(animation, operand.PlayBackwards);
                if (context.VM.TS1 || newMode)
                    state.Speed = 30 / 25f;
                state.Loop = true;
                avatar.Animations.Add(state);
                avatar.Avatar.LeftHandGesture = SimHandGesture.Idle;
                avatar.Avatar.RightHandGesture = SimHandGesture.Idle;

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
                var owner = (source == VMAnimationScope.Object) ? context.CodeOwner : ((source == VMAnimationScope.StackObject)? context.StackObject.Object : operand.AnimationSource);
                bool child = ((VMAvatar)context.Caller).GetPersonData(VMPersonDataVariable.PersonsAge) < 18 && context.VM.TS1;
                if (child)
                {
                    if (operand.ChildAnimationCache == null || owner != operand.ChildAnimationSource)
                    {
                        operand.ChildAnimationSource = owner;
                        operand.ChildAnimationCache = VMMemory.GetAnimation(context, source, id);
                    }
                    animation = operand.ChildAnimationCache;
                } else
                {
                    if (operand.AnimationCache == null || owner != operand.AnimationSource)
                    {
                        operand.AnimationSource = owner;
                        operand.AnimationCache = VMMemory.GetAnimation(context, source, id);
                    }
                    animation = operand.AnimationCache;
                }

            } else
            {
                animation = VMMemory.GetAnimation(context, source, id);
            }

            if (animation == null){
                return VMPrimitiveExitCode.GOTO_TRUE_NEXT_TICK;
            }

            switch (operand.Mode)
            {
                case 1:
                    avatar.Animations.Clear();
                    var state = new VMAnimationState(animation, operand.PlayBackwards);
                    if (context.VM.TS1 || newMode)
                        state.Speed = 30 / 25f;
                    if (avatar.GetValue(VMStackObjectVariable.WalkStyle) == 1 && operand.Hurryable) state.Speed *= 2;
                    state.Loop = true;
                    avatar.Animations.Add(state);

                    avatar.Avatar.LeftHandGesture = SimHandGesture.Idle;
                    avatar.Avatar.RightHandGesture = SimHandGesture.Idle;
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case 3:
                    avatar.CarryAnimationState = null;
                    goto case 0;
                case 0:
                    /** Are we starting the animation or progressing it? **/
                    if (avatar.CurrentAnimationState == null || avatar.CurrentAnimationState.Anim != animation)
                    {
                        /** Start it **/
                        avatar.Animations.Clear();
                        var astate = new VMAnimationState(animation, operand.PlayBackwards);
                        if (context.VM.TS1 || newMode)
                            astate.Speed = 30 / 25f;
                        if (avatar.GetValue(VMStackObjectVariable.WalkStyle) == 1 && operand.Hurryable) astate.Speed *= 2;
                        avatar.Animations.Add(astate);
                    
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
                case 2:
                    avatar.CarryAnimationState = new VMAnimationState(animation, false);
                    return VMPrimitiveExitCode.GOTO_TRUE;
            }
            return VMPrimitiveExitCode.GOTO_TRUE;
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
                ChildAnimationCache = null;
            }
        }
        public byte LocalEventNumber { get; set; }
        public byte _pad;
        public VMAnimationScope Source { get; set; }
        public byte Flags;
        public byte ExpectedEventCount { get; set; }

        public Animation AnimationCache;
        public GameObject AnimationSource;

        public Animation ChildAnimationCache;
        public GameObject ChildAnimationSource;

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

        public bool Hurryable
        {
            get
            {
                return (Flags & 64) == 64;
            }
            set
            {
                if (value) Flags |= 64;
                else Flags &= unchecked((byte)~64);
            }
        }

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

            set
            {
                Flags &= unchecked((byte)~(1 | (2 << 3)));
                Flags |= (byte)((value & 1) | ((value & 2) << 3));
            }
        }

        public override string ToString(){
            return "Animate Sim (id " + AnimationID + " from " + Source.ToString() + ")";
        }
    }
}
