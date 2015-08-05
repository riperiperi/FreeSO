/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.SimAntics.Engine;
using FSO.Files.Utils;
using Microsoft.Xna.Framework;
using FSO.LotView;
using FSO.Files.Formats.IFF.Chunks;
using FSO.LotView.Model;

namespace FSO.SimAntics.Primitives
{
    public class VMGotoRelativePosition : VMPrimitiveHandler
    {

        private static LotTilePos[] Positions = { 
            new LotTilePos(0, -16, 0),
            new LotTilePos(16, -16, 0),
            new LotTilePos(16, 0, 0),
            new LotTilePos(16, 16, 0),
            new LotTilePos(0, 16, 0),
            new LotTilePos(-16, 16, 0),
            new LotTilePos(-16, 0, 0),
            new LotTilePos(-16, -16, 0)
        };

        public override VMPrimitiveExitCode Execute(VMStackFrame context){
            var operand = context.GetCurrentOperand<VMGotoRelativePositionOperand>();
            
            var obj = context.StackObject;
            var avatar = (VMAvatar)context.Caller;

            if (obj.Position == LotTilePos.OUT_OF_WORLD) return VMPrimitiveExitCode.GOTO_FALSE;

            var result = new VMFindLocationResult();
            LotTilePos relative;
            int intDir = (int)Math.Round(Math.Log((double)obj.Direction, 2));

            /** 
             * Examples for reference
             * Fridge - Have Snack - In front of, facing
             */
            if (operand.Location == VMGotoRelativeLocation.OnTopOf)
            {
                relative = new LotTilePos(0, 0, obj.Position.Level);
                result.Position = new LotTilePos(obj.Position);
                //result.Flags = (SLOTFlags)obj.Direction;
            }
            else
            {
                int dir;
                if (operand.Location == VMGotoRelativeLocation.AnywhereNear) dir = (int)context.VM.Context.NextRandom(8);
                else dir = ((int)operand.Location + intDir) % 8;
                
                relative = Positions[dir];

                var location = obj.Position;
                location += relative;
                result.Position = location;
            }
            //throw new Exception("Unknown goto relative");

            if (operand.Direction == VMGotoRelativeDirection.Facing)
            {
                result.RadianDirection = (float)GetDirectionTo(relative, new LotTilePos(0, 0, relative.Level));
                result.Flags = RadianToFlags(result.RadianDirection);
            }
            else if (operand.Direction == VMGotoRelativeDirection.AnyDirection)
            {
                result.RadianDirection = 0;
                result.Flags = SLOTFlags.NORTH;
            }
            else
            {
                var dir = ((int)operand.Direction + intDir) % 8;
                result.RadianDirection = (float)dir*(float)(Math.PI/4.0);
                if (result.RadianDirection > Math.PI) result.RadianDirection -= (float)(Math.PI * 2.0);
                result.Flags = (SLOTFlags)(1<<(int)dir);
            }

            var pathFinder = context.Thread.PushNewPathFinder(context, new List<VMFindLocationResult>() { result });
            if (pathFinder != null) return VMPrimitiveExitCode.CONTINUE;
            else return VMPrimitiveExitCode.GOTO_FALSE;
        }

        private SLOTFlags RadianToFlags(double rad)
        {
            int result = (int)(Math.Round((rad / (Math.PI * 2)) * 8) + 80) % 8; //for best results, make sure rad is >-pi and <pi
            return (SLOTFlags)(1 << result);
        }

        private double GetDirectionTo(LotTilePos pos1, LotTilePos pos2)
        {
            return Math.Atan2(pos2.x - pos1.x, -(pos2.y - pos1.y));
        }
    }

    public class VMGotoRelativePositionOperand : VMPrimitiveOperand
    {
        /** How long to meander around objects **/
        public ushort OldTrapCount;
        public VMGotoRelativeLocation Location;
        public VMGotoRelativeDirection Direction;
        public ushort RouteCount;
        public VMGotoRelativeFlags Flags;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes){
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN)){
                OldTrapCount = io.ReadUInt16();
                Location = (VMGotoRelativeLocation)((sbyte)io.ReadByte());
                Direction = (VMGotoRelativeDirection)((sbyte)io.ReadByte());
                RouteCount = io.ReadUInt16();
                Flags = (VMGotoRelativeFlags)io.ReadByte();
            }
        }
        #endregion
    }

    public enum VMGotoRelativeDirection
    {
        Facing = -2,
        AnyDirection = -1,
        SameDirection = 0,
        FortyFiveDegreesRightOfSameDirection = 1,
        NinetyDegreesRightOfSameDirection = 2,
        FortyFiveDegreesLeftOfOpposingDirection = 3,
        OpposingDirection = 4,
        FortyFiveDegreesRightOfOpposingDirection = 5,
        NinetyDegreesRightOfOpposingDirection = 6,
        FortyFiveDegreesLeftOfSameDirection = 7
    }

    public enum VMGotoRelativeLocation {
        OnTopOf = -2,
        AnywhereNear = -1,
        InFrontOf = 0,
        FrontAndToRightOf = 1,
        ToTheRightOf = 2,
        BehindAndToRightOf = 3,
        Behind = 4,
        BehindAndToTheLeftOf = 5,
        ToTheLeftOf = 6,
        InFrontAndToTheLeftOf = 7
    }

    [Flags]
    public enum VMGotoRelativeFlags
    {
        RequireSameAltitude = 0x2
    }
}
