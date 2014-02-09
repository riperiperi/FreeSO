using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.files.utils;
using tso.simantics.engine.scopes;
using tso.simantics.engine.utils;
using tso.vitaboy;
using tso.simantics.model;
using tso.simantics.utils;

namespace tso.simantics.engine.primitives
{
    public class VMAnimateSim : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            var operand = context.GetCurrentOperand<VMAnimateSimOperand>();
            var animation = VMMemory.GetAnimation(context, operand.Source, operand.AnimationID);
            if(animation == null){
                return VMPrimitiveExitCode.ERROR;
            }
            
            var avatar = (VMAvatar)context.Caller;

            /** Are we starting the animation or progressing it? **/
            if (avatar.CurrentAnimation == null || avatar.CurrentAnimation != animation)
            {
                //21
                Trace("animate_sim " + operand.AnimationID + " from " + operand.Source);

                /** Start it **/
                avatar.CurrentAnimation = animation;
                avatar.CurrentAnimationState = new VMAnimationState();

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
            }
            else
            {
                avatar.CurrentAnimationState.CurrentFrame++;
            }


            var currentFrame = avatar.CurrentAnimationState.CurrentFrame;
            var currentTime = currentFrame * 33.33f;
            var timeProps = avatar.CurrentAnimationState.TimePropertyLists;

            for (var i = 0; i < timeProps.Count; i++)
            {
                var tp = timeProps[i];
                if (tp.ID > currentTime){
                    break;
                }

                timeProps.RemoveAt(0);
                i--;

                var evt = tp.Properties["xevt"];
                if (evt != null){
                    var eventValue = short.Parse(evt);
                    Trace("AnimationEvent, " + eventValue);
                    if (operand.StoreFrameInLocal)
                    {
                        VMMemory.SetVariable(context, VMVariableScope.Local, operand.LocalEventNumber, eventValue);
                    }
                    else
                    {
                        if (operand.LocalEventNumber == 21)
                        {
                            /** Store in param0? Not sure if this is the rule or if just defaults to param 0 if false **/
                            VMMemory.SetVariable(context, VMVariableScope.Parameters, 0, eventValue);
                        }
                    }
                }
            }


            //var currentFrame = (short)avatar.CurrentAnimationState.CurrentFrame;
            ////var currentFrameTimeProperties = animation.GetTimePropertiesForFrame(currentFrame);

            //if (currentFrameTimeProperties != null)
            //{
            //    var evt = currentFrameTimeProperties.Properties["xevt"];
            //    if (evt != null){
            //        var eventValue = short.Parse(evt);
            //        if (operand.StoreFrameInLocal)
            //        {
            //            VMMemory.SetVariable(context, VMVariableScope.Local, operand.LocalEventNumber, eventValue);
            //        }
            //        else
            //        {
            //            if (operand.LocalEventNumber == 21)
            //            {
            //                /** Store in param0? Not sure if this is the rule or if just defaults to param 0 if false **/
            //                VMMemory.SetVariable(context, VMVariableScope.Parameters, 0, eventValue);
            //            }
            //        }
            //    }
            //}


            var status = Animator.RenderFrame(avatar.Avatar, animation, avatar.CurrentAnimationState.CurrentFrame);
            if (status == AnimationStatus.IN_PROGRESS)
            {
                return VMPrimitiveExitCode.GOTO_FALSE_NEXT_TICK;
            }
            else
            {
                avatar.CurrentAnimation = null;
                return VMPrimitiveExitCode.GOTO_TRUE;
            }
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

        public override string ToString(){
            return "Animate Sim (id " + AnimationID + " from " + Source.ToString() + ")";
        }
    }
}
