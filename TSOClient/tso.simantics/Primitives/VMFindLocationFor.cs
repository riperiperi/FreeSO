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

            switch (operand.Mode)
            {
                case 0:
                    //default
                    if (FindLocationFor(obj, refObj, context.VM.Context, flags, operand.PreferNonEmpty)) return VMPrimitiveExitCode.GOTO_TRUE;
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
                    if (FindLocationVector(obj, refObj, context.VM.Context, intDir, flags, operand.PreferNonEmpty)) return VMPrimitiveExitCode.GOTO_TRUE;
                    else return VMPrimitiveExitCode.GOTO_FALSE;
                case 5:
                    //random
                    var ctx = context.VM.Context;
                    for (int i=0; i<100; i++)
                    {
                        var loc = LotTilePos.FromBigTile((short)(ctx.NextRandom((ulong)ctx.Architecture.Width - 2) + 1), (short)(ctx.NextRandom((ulong)ctx.Architecture.Height - 2) + 1), refObj.Position.Level);
                        if (obj.SetPosition(loc, Direction.NORTH, ctx, flags).Status == VMPlacementError.Success)
                            return VMPrimitiveExitCode.GOTO_TRUE;
                    }
                    return VMPrimitiveExitCode.GOTO_FALSE;
            }

            return VMPrimitiveExitCode.GOTO_FALSE;
        }

        private static bool TileOccupied(VMContext context, LotTilePos pos)
        {
            return (context.ObjectQueries.GetObjectsAt(pos)?.Count ?? 0) > 0;
        }

        public static bool FindLocationVector(VMEntity obj, VMEntity refObj, VMContext context, int dir, VMPlaceRequestFlags flags, bool preferNonEmpty = false)
        {
            LotTilePos step = DirectionVectors[dir];
            var dirf = (Direction)(1 << (dir));
            var deferred = new List<LotTilePos>();

            Func<LotTilePos, bool> evaluate = (LotTilePos pos) =>
            {
                if (preferNonEmpty && TileOccupied(context, pos))
                {
                    deferred.Add(pos);
                    return false;
                }
                else
                {
                    return obj.SetPosition(pos, dirf, context, flags).Status == VMPlacementError.Success;
                }
            };

            for (int i = 0; i < 32; i++)
            {
                if (evaluate(new LotTilePos(refObj.Position) + step * (i / 2))) return true;
                if (i%2 != 0)
                {
                    if (evaluate(new LotTilePos(refObj.Position) - step * (i / 2))) return true;
                }
            }

            foreach (var tile in deferred)
            {
                if (obj.SetPosition(tile, dirf, context, flags).Status == VMPlacementError.Success) return true;
            }
            return false;
        }

        public static bool FindLocationFor(VMEntity obj, VMEntity refObj, VMContext context, VMPlaceRequestFlags flags, bool preferNonEmpty = false)
        {
            var deferred = new List<Tuple<LotTilePos, Direction>>();

            Func<LotTilePos, Direction, bool> evaluate = (LotTilePos pos, Direction dir) =>
            {
                if (preferNonEmpty && TileOccupied(context, pos))
                {
                    deferred.Add(new Tuple<LotTilePos, Direction>(pos, dir));
                    return false;
                }
                else
                {
                    return obj.SetPosition(pos, dir, context, flags).Status == VMPlacementError.Success;
                }
            };

            for (int i = 0; i < 10; i++)
            {
                if (i == 0)
                {
                    var pos = new LotTilePos(refObj.Position);
                    for (int j = 0; j < 4; j++)
                    {
                        var dir = (Direction)(1 << (j * 2));
                        if (evaluate(pos, dir)) return true;
                    }
                }
                else
                {
                    LotTilePos bPos = refObj.Position;
                    for (int x = -i; x <= i; x++)
                    {
                        for (int j = 0; j < 8; j++)
                        {
                            var pos = LotTilePos.FromBigTile((short)(bPos.TileX + x), (short)(bPos.TileY + ((j % 2) * 2 - 1) * i), bPos.Level);
                            var dir = (Direction)(1 << ((j / 2) * 2));
                            if (evaluate(pos, dir)) return true;
                        }
                    }

                    for (int y = 1 - i; y < i; y++)
                    {
                        for (int j = 0; j < 8; j++)
                        {
                            var pos = LotTilePos.FromBigTile((short)(bPos.TileX + ((j % 2) * 2 - 1) * i), (short)(bPos.TileY + y), bPos.Level);
                            var dir = (Direction)(1 << ((j / 2) * 2));
                            if (evaluate(pos, dir)) return true;
                        }
                    }
                }
            }

            foreach (var tile in deferred)
            {
                if (obj.SetPosition(tile.Item1, tile.Item2, context, flags).Status == VMPlacementError.Success) return true;
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

        public bool PreferNonEmpty
        {
            get
            {
                return (Flags & 2) == 0;
            }
            set
            {
                if (!value) Flags |= 2;
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
