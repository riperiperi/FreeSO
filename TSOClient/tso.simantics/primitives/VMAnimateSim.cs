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

            var avatar = (VMAvatar)context.Caller;
            if (operand.AnimationID == 0)
            { //reset
                avatar.CurrentAnimation = null;
                return VMPrimitiveExitCode.GOTO_TRUE;
            }

            var animation = VMMemory.GetAnimation(context, operand.Source, operand.AnimationID);
            if(animation == null){
                return VMPrimitiveExitCode.ERROR;
            }
            
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
