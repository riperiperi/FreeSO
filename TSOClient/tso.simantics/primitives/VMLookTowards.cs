using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.SimAntics.Engine;
using FSO.Files.Utils;
using FSO.LotView.Model;
using FSO.Files.Formats.IFF.Chunks;
using System.IO;

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

            switch (operand.Mode)
            {
                case VMLookTowardsMode.HeadTowardsObject:
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

            }
            
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
        public VMLookTowardsMode Mode;

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
        BodyAwayFromStackObj = 3
    }
}
