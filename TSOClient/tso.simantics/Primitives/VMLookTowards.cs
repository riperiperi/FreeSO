using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.SimAntics.Engine;
using FSO.Files.Utils;
using FSO.LotView.Model;
using FSO.Files.Formats.IFF.Chunks;
using System.IO;
using FSO.SimAntics.Model;

namespace FSO.SimAntics.Primitives
{
    // This primitive allows the sim to look at objects or other people eg. when talking to them. Not important right now
    // but crucial for tv/eating conversations to make sense

    public class VMLookTowards : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMLookTowardsOperand)args;
            //TODO: primitive fails if object calls it
            VMAvatar sim = (VMAvatar)context.Caller;

            var result = new VMFindLocationResult();
            result.Position = new LotTilePos(sim.Position);

            LotTilePos pos = new LotTilePos();
            switch (operand.Mode)
            {
                case VMLookTowardsMode.HeadTowardsObject:
                    //set default state
                    sim.SetPersonData(VMPersonDataVariable.HeadSeekObject, context.StackObjectID);
                    sim.SetPersonData(VMPersonDataVariable.HeadSeekState, 1); //in progress flag only
                    sim.SetPersonData(VMPersonDataVariable.HeadSeekLimitAction, 1); //look back on limit?
                    sim.SetPersonData(VMPersonDataVariable.HeadSeekFinishAction, 0); //unknown
                    sim.SetPersonData(VMPersonDataVariable.HeadSeekTimeout, 0); //forever
                    return VMPrimitiveExitCode.GOTO_TRUE; //TODO: turning head towards things, with head seek timeout
                case VMLookTowardsMode.BodyTowardsCamera:
                    return VMPrimitiveExitCode.GOTO_TRUE; //does not work in TSO
                case VMLookTowardsMode.BodyTowardsStackObj:
                    result.RadianDirection = (float)GetDirectionTo(sim.Position, context.StackObject.Position);
                    break;
                case VMLookTowardsMode.BodyAwayFromStackObj:
                    result.RadianDirection = (float)GetDirectionTo(sim.Position, context.StackObject.Position);
                    result.RadianDirection = (float)((result.RadianDirection + Math.PI) % (Math.PI*2));
                    break;
                case VMLookTowardsMode.BodyTowardsAverageStackObj:
                    foreach (var obj in context.StackObject.MultitileGroup.Objects)
                        pos += obj.Position;
                    pos /= context.StackObject.MultitileGroup.Objects.Count;
                    result.RadianDirection = (float)GetDirectionTo(sim.Position, pos);
                    break;
                case VMLookTowardsMode.BodyAwayFromAverageStackObj:
                    foreach (var obj in context.StackObject.MultitileGroup.Objects)
                        pos += obj.Position;
                    pos /= context.StackObject.MultitileGroup.Objects.Count;
                    result.RadianDirection = (float)GetDirectionTo(sim.Position, pos);
                    result.RadianDirection = (float)((result.RadianDirection + Math.PI) % (Math.PI * 2));
                    break;
            }

            if (context.Thread.IsCheck) return VMPrimitiveExitCode.GOTO_FALSE;
            var pathFinder = context.Thread.PushNewRoutingFrame(context, false); //use the path finder to do the turn animation.
            pathFinder.InitRoutes(new List<VMFindLocationResult>() { result });

            return VMPrimitiveExitCode.CONTINUE;
        }

        private double GetDirectionTo(LotTilePos pos1, LotTilePos pos2)
        {
            return Math.Atan2(pos2.x - pos1.x, -(pos2.y - pos1.y));
        }
        private SLOTFlags RadianToFlags(double rad)
        {
            int result = (int)(Math.Round((rad / (Math.PI * 2)) * 8) + 80) % 8; //for best results, make sure rad is >-pi and <pi
            return (SLOTFlags)(1 << result);
        }
    }

    public class VMLookTowardsOperand : VMPrimitiveOperand
    {
        public VMLookTowardsMode Mode { get; set; }

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                Mode = (VMLookTowardsMode)io.ReadByte();
            }
        }

        public void Write(byte[] bytes) {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write((byte)Mode);
            }
        }
        #endregion
    }

    public enum VMLookTowardsMode : byte
    {
        HeadTowardsObject = 0,
        BodyTowardsCamera = 1,
        BodyTowardsStackObj = 2,
        BodyAwayFromStackObj = 3,
        BodyTowardsAverageStackObj = 4,
        BodyAwayFromAverageStackObj = 5
    }
}
