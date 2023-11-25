using System;
using FSO.Files.Utils;
using FSO.Vitaboy;
using FSO.SimAntics.Model;
using FSO.SimAntics.Engine;
using FSO.Files.Formats.IFF.Chunks;
using System.IO;

namespace FSO.SimAntics.Primitives
{
    public class VMReach : VMPrimitiveHandler
    {

        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMReachOperand)args;
            var completedPickup = false;
            int height;

            if (operand.Mode == 0)
            { //reach to stack object
                var container = context.StackObject.Container;
                if (container == context.Caller) completedPickup = true;
                if (container == null || container is VMAvatar)
                {
                    height = 0;
                }
                else
                {
                    var slot = container.Slots.Slots[0][context.StackObject.ContainerSlot];
                    if (slot != null)
                    {
                        height = (int)Math.Round((slot.Height != 5) ? SLOT.HeightOffsets[slot.Height - 1] : slot.Offset.Z);
                    }
                    else height = 0;
                }
            }
            else if (operand.Mode == 1)
            {
                var slotNum = context.Args[operand.SlotParam];
                var slot = context.StackObject.Slots.Slots[0][slotNum];
                if (slot != null)
                {
                    height = (int)Math.Round((slot.Height != 5) ? SLOT.HeightOffsets[slot.Height-1] : slot.Offset.Z);
                }
                else return VMPrimitiveExitCode.GOTO_FALSE;
            }
            else
            {
                //reach to mouth is unimplemented so no, also none others exist after
                throw new VMSimanticsException("Reach to mouth not implemented!", context);
            }

            string animationName;
            if (context.Caller.Container != null) animationName = "a2o-sit-reach-table.anim";
            else if (height < 2) animationName = "a2o-reach-floorht.anim";
            else if (height < 4) animationName = "a2o-reach-seatht.anim";
            else animationName = "a2o-reach-tableht.anim";


            var animation = FSO.Content.Content.Get().AvatarAnimations.Get(animationName);
            if (animation == null) {
                return VMPrimitiveExitCode.GOTO_FALSE;
            }
            var avatar = (VMAvatar)context.Caller;
            
            /** Are we starting the animation or progressing it? **/
            if (avatar.CurrentAnimationState == null || (avatar.CurrentAnimationState.Anim != animation && !completedPickup))
            { //start the grab!
                /** Start it **/
                avatar.Animations.Clear();
                avatar.Animations.Add(new VMAnimationState(animation, false));

                avatar.Avatar.LeftHandGesture = SimHandGesture.Idle;
                avatar.Avatar.RightHandGesture = SimHandGesture.Idle;
                return VMPrimitiveExitCode.CONTINUE_NEXT_TICK;
            }
            else
            {
                if (avatar.CurrentAnimationState.EndReached)
                {
                    avatar.Animations.Clear();
                    return VMPrimitiveExitCode.GOTO_TRUE;
                } 
                else if (avatar.CurrentAnimationState.EventQueue.Count > 0)
                {

                    if (avatar.CurrentAnimationState.EventQueue[0] == 0)
                    {
                        //do the grab/drop
                        if (operand.Mode == 0)
                        { //pick up stack object. no drop condition
                            if (context.Caller.GetSlot(0) == null)
                            {
                                context.Caller.PlaceInSlot(context.StackObject, 0, true, context.VM.Context);
                            }
                            else
                            {
                                //failed = true;
                            }
                        }
                        else if (operand.Mode == 1)
                        { //grab or drop, depending on if we're holding something
                            var holding = context.Caller.GetSlot(0);
                            var slotNum = context.Args[operand.SlotParam];

                            // TODO: do anything special on failure?

                            if (holding == null)
                            { //grab
                                var item = context.StackObject.GetSlot(slotNum);
                                if (item != null)
                                {
                                    var failed = !context.Caller.PlaceInSlot(item, 0, true, context.VM.Context);
                                }
                                //else failed = true; //can't grab from an empty space
                            }
                            else //drop
                            {
                                var itemTest = context.StackObject.GetSlot(slotNum);
                                if (itemTest == null)
                                {
                                    var failed = !context.StackObject.PlaceInSlot(holding, slotNum, true, context.VM.Context);
                                }
                            }
                        }
                    }
                    avatar.CurrentAnimationState.EventQueue.RemoveAt(0);
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

        public void Write(byte[] bytes) {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(Mode);
                io.Write(GrabOrDrop);
                io.Write(SlotParam);
            }
        }
        #endregion
    }
}
