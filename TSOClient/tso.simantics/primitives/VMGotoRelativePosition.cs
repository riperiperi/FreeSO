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
using System.IO;

namespace FSO.SimAntics.Primitives
{
    public class VMGotoRelativePosition : VMPrimitiveHandler
    {

        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMGotoRelativePositionOperand)args;
            
            var obj = context.StackObject;
            var avatar = (VMAvatar)context.Caller;

            if (obj.Position == LotTilePos.OUT_OF_WORLD) return VMPrimitiveExitCode.GOTO_FALSE;

            var slot = new SLOTItem { Type = 3, Standing = 1 };

            if (operand.Location != VMGotoRelativeLocation.OnTopOf) { //default slot is on top of
                slot.MinProximity = 16;
                slot.MaxProximity = 24;
                if (operand.Location == VMGotoRelativeLocation.AnywhereNear) slot.Rsflags |= (SLOTFlags)255;
                else slot.Rsflags |= (SLOTFlags)(1 << (((int)operand.Location) % 8));
            }

            if (operand.Direction == VMGotoRelativeDirection.AnyDirection) slot.Facing = SLOTFacing.FaceAnywhere; //TODO: verify. not sure where this came from?
            else slot.Facing = (SLOTFacing)operand.Direction;

            var pathFinder = context.Thread.PushNewRoutingFrame(context, !operand.NoFailureTrees);
            var success = pathFinder.InitRoutes(slot, context.StackObject);

            return VMPrimitiveExitCode.CONTINUE;
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

        public bool NoFailureTrees
        {
            get
            {
                return (Flags & VMGotoRelativeFlags.NoFailureTrees) > 0;
            }
        }

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

        public void Write(byte[] bytes) {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(OldTrapCount);
                io.Write((byte)Location);
                io.Write((byte)Direction);
                io.Write(RouteCount);
                io.Write((byte)Flags);
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
        AllowDiffAlt = 0x1,
        NoFailureTrees = 0x2
    }
}
