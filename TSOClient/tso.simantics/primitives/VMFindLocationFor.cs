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
using FSO.SimAntics.Model;
using FSO.LotView.Components;
using FSO.LotView.Model;
using System.IO;

namespace FSO.SimAntics.Primitives
{
    public class VMFindLocationFor : VMPrimitiveHandler
    {
        private static LotTilePos[] DirectionVectors = {
            new LotTilePos(0, 16, 0),
            new LotTilePos(-16, 16, 0),
            new LotTilePos(-16, 0, 0),
            new LotTilePos(-16, -16, 0),
            new LotTilePos(0, -16, 0),
            new LotTilePos(16, -16, 0),
            new LotTilePos(16, 0, 0),
            new LotTilePos(16, 16, 0),
        };

        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMFindLocationForOperand)args;
            var refObj = (operand.UseLocalAsRef) ? context.VM.GetObjectById((short)context.Locals[operand.Local]) : context.Caller;

            var obj = context.StackObject;
            var flags = VMPlaceRequestFlags.AcceptSlots;
            if (operand.UserEditableTilesOnly) flags |= VMPlaceRequestFlags.UserBuildableLimit;
            if (operand.AllowIntersection) flags |= VMPlaceRequestFlags.AllowIntersection;

            switch (operand.Mode)
            {
                case 0:
                    //default
                    if (FindLocationFor(obj, refObj, context.VM.Context, flags)) return VMPrimitiveExitCode.GOTO_TRUE;
                    else return VMPrimitiveExitCode.GOTO_FALSE;
                case 1:
                    //out of world
                    obj.SetPosition(LotTilePos.OUT_OF_WORLD, Direction.NORTH, context.VM.Context, flags);
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case 2:
                    //"smoke cloud" - halfway between callee and caller (is "caller" actually reference object?)
                    var smokePos = context.Callee.Position;
                    smokePos += context.Caller.Position;
                    smokePos /= 2;
                    smokePos -= new LotTilePos(8, 8, 0); //smoke is 2x2... offset to center it.
                    return (obj.SetPosition(smokePos, Direction.NORTH, context.VM.Context).Status == VMPlacementError.Success)?
                        VMPrimitiveExitCode.GOTO_TRUE : VMPrimitiveExitCode.GOTO_FALSE;
                case 3:
                case 4:
                    //along object vector
                    var intDir = (int)Math.Round(Math.Log((double)refObj.Direction, 2));
                    if (operand.Mode == 4) intDir = (intDir + 2) % 8; //lateral to object vector
                    if (FindLocationVector(obj, refObj, context.VM.Context, intDir, flags)) return VMPrimitiveExitCode.GOTO_TRUE;
                    else return VMPrimitiveExitCode.GOTO_FALSE;
                case 5:
                    //random
                    return VMPrimitiveExitCode.GOTO_TRUE;
            }

            return VMPrimitiveExitCode.GOTO_FALSE;
        }

        public static bool FindLocationVector(VMEntity obj, VMEntity refObj, VMContext context, int dir, VMPlaceRequestFlags flags)
        {
            LotTilePos step = DirectionVectors[dir];
            for (int i = 0; i < 32; i++)
            {
                if (obj.SetPosition(new LotTilePos(refObj.Position) + step * (i/2),
                    (Direction)(1 << (dir)), context, flags).Status == VMPlacementError.Success)
                    return true;
                if (i%2 != 0)
                {
                    if (obj.SetPosition(new LotTilePos(refObj.Position) - step * (i/2),
                        (Direction)(1 << (dir)), context, flags).Status == VMPlacementError.Success)
                        return true;
                }
            }
            return false;
        }

        public static bool FindLocationFor(VMEntity obj, VMEntity refObj, VMContext context, VMPlaceRequestFlags flags)
        {
            for (int i = 0; i < 10; i++)
            {
                if (i == 0)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        if (obj.SetPosition(new LotTilePos(refObj.Position), (Direction)(1 << (j * 2)), context, flags).Status == VMPlacementError.Success)
                            return true;
                    }
                }
                else
                {
                    LotTilePos bPos = refObj.Position;
                    for (int x = -i; x <= i; x++)
                    {
                        for (int j = 0; j < 8; j++)
                        {
                            if (obj.SetPosition(LotTilePos.FromBigTile((short)(bPos.TileX + x), (short)(bPos.TileY + ((j % 2) * 2 - 1) * i), bPos.Level),
                                (Direction)(1 << ((j / 2) * 2)), context, flags).Status == VMPlacementError.Success)
                                return true;
                        }
                    }

                    for (int y = 1 - i; y < i; y++)
                    {
                        for (int j = 0; j < 8; j++)
                        {
                            if (obj.SetPosition(LotTilePos.FromBigTile((short)(bPos.TileX + ((j % 2) * 2 - 1) * i), (short)(bPos.TileY + y), bPos.Level),
                                (Direction)(1 << ((j / 2) * 2)), context, flags).Status == VMPlacementError.Success)
                                return true;
                        }
                    }
                }
            }
            return false;
        }
    }

    public class VMFindLocationForOperand : VMPrimitiveOperand
    {
        public byte Mode { get; set; }
        public byte Local { get; set; }
        public byte Flags { get; set; }

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                Mode = io.ReadByte();
                Local = io.ReadByte();
                Flags = io.ReadByte();
            }
        }

        public void Write(byte[] bytes) {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(Mode);
                io.Write(Local);
                io.Write(Flags);
            }
        }
        #endregion

        public bool UseLocalAsRef
        {
            get
            {
                return (Flags & 1) == 1;
            }
            set
            {
                if (value) Flags |= 1;
                else Flags &= unchecked((byte)~1);
            }
        }

        public bool AllowIntersection
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

        public bool UserEditableTilesOnly
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
    }
}
