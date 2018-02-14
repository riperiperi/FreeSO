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
using FSO.LotView.Model;
using Microsoft.Xna.Framework;
using System.IO;
using FSO.SimAntics.NetPlay.Model.Commands;
using FSO.Files.Formats.IFF.Chunks;

namespace FSO.SimAntics.Engine.Primitives
{
    public class VMCreateObjectInstance : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMCreateObjectInstanceOperand)args;
            LotTilePos tpos = new LotTilePos(LotTilePos.OUT_OF_WORLD);
            Direction dir = Direction.NORTH; //FaceStackObjDir? not used afaik

            switch (operand.Position)
            {
                case VMCreateObjectPosition.UnderneathMe:
                case VMCreateObjectPosition.OnTopOfMe:
                    tpos = new LotTilePos(context.Caller.Position);
                    dir = Direction.NORTH;
                    break;
                case VMCreateObjectPosition.BelowObjectInLocal:
                    tpos = new LotTilePos(context.VM.GetObjectById((short)context.Locals[operand.LocalToUse]).Position);
                    dir = Direction.NORTH;
                    break;
                case VMCreateObjectPosition.BelowObjectInStackParam0:
                    tpos = new LotTilePos(context.VM.GetObjectById((short)context.Args[0]).Position);
                    dir = Direction.NORTH;
                    break;
                case VMCreateObjectPosition.OutOfWorld:
                    dir = Direction.NORTH;
                    break;
                case VMCreateObjectPosition.InSlot0OfStackObject:
                case VMCreateObjectPosition.InMyHand:
                    dir = (operand.Position == VMCreateObjectPosition.InMyHand)?context.Caller.Direction:context.StackObject.Direction;
                    //this object should start in slot 0 of the stack object!
                    //we have to create it first tho so hold your horses
                    break;
                case VMCreateObjectPosition.InFrontOfStackObject:
                case VMCreateObjectPosition.InFrontOfMe:
                    var objp = (operand.Position == VMCreateObjectPosition.InFrontOfStackObject)?context.StackObject:context.Caller;
                    tpos = new LotTilePos(objp.Position);
                    switch (objp.Direction)
                    {
                        case FSO.LotView.Model.Direction.SOUTH:
                            tpos.y += 16;
                            break;
                        case FSO.LotView.Model.Direction.WEST:
                            tpos.x -= 16;
                            break;
                        case FSO.LotView.Model.Direction.EAST:
                            tpos.x += 16;
                            break;
                        case FSO.LotView.Model.Direction.NORTH:
                            tpos.y -= 16;
                            break;
                    }
                    dir = objp.Direction;
                    break;
                case VMCreateObjectPosition.NextToMeInDirectionOfLocal:
                    tpos = new LotTilePos(context.Caller.Position);
                    var udir = context.Locals[operand.LocalToUse];
                    dir = Direction.NORTH;
                    switch (udir)
                    {
                        case 0:
                            dir = Direction.NORTH;
                            tpos.y -= 16;
                            break;
                        case 2:
                            dir = Direction.EAST;
                            tpos.x += 16;
                            break;
                        case 4:
                            dir = Direction.SOUTH;
                            tpos.y += 16;
                            break;
                        case 6:
                            dir = Direction.WEST;
                            tpos.x -= 16;
                            break;
                    }
                    break;
                default:
                    throw new VMSimanticsException("Where do I put this??", context);
            }
            var guid = operand.GUID;
            Neighbour neigh = null;
            if (operand.UseNeighbor && context.VM.TS1) {
                neigh = Content.Content.Get().Neighborhood.GetNeighborByID(context.StackObjectID);
                if (neigh == null) return VMPrimitiveExitCode.GOTO_FALSE;
                guid = neigh.GUID;
            }

            var mobj = context.VM.Context.CreateObjectInstance(guid, tpos, dir,
                (operand.PassObjectIds && context.StackObject != null) ? (context.StackObject.ObjectID) : (short)0,
                (operand.PassTemp0) ? (context.Thread.TempRegisters[0]) : (operand.PassObjectIds ? context.Caller.ObjectID : (short)0) , false);

            if (mobj == null) return VMPrimitiveExitCode.GOTO_FALSE;
            var obj = mobj.BaseObject;

            if (operand.Position == VMCreateObjectPosition.InSlot0OfStackObject) context.StackObject.PlaceInSlot(obj, 0, true, context.VM.Context);
            else if (operand.Position == VMCreateObjectPosition.InMyHand) context.Caller.PlaceInSlot(obj, 0, true, context.VM.Context);
            else if (operand.Position == VMCreateObjectPosition.UnderneathMe && obj.Position == LotTilePos.OUT_OF_WORLD)
            {
                foreach (var iobj in mobj.Objects) iobj.IgnoreIntersection = context.Caller.MultitileGroup;
                mobj.ChangePosition(context.Caller.Position, dir, context.VM.Context, Model.VMPlaceRequestFlags.Default);
                foreach (var iobj in mobj.Objects) iobj.IgnoreIntersection = null;
            }

            if (operand.Position != VMCreateObjectPosition.OutOfWorld && obj.Position == LotTilePos.OUT_OF_WORLD && obj.Container == null)
            {
                obj.Delete(true, context.VM.Context);
                return VMPrimitiveExitCode.GOTO_FALSE;
            }
            if (operand.ReturnImmediately)
            {
                short interaction = operand.InteractionCallback;
                if (interaction == 254 && context.ActionTree)
                {
                    var temp = context.Thread.Queue[0].InteractionNumber;
                    if (temp == -1) throw new VMSimanticsException("Set callback as 'this interaction' when queue item has no interaction number!", context);
                    interaction = (short)temp;
                    if (temp < 0) interaction |= unchecked((short)0x8000); //cascade global flag
                } else if (interaction == 252 || interaction == 253) {
                    interaction = context.Thread.TempRegisters[0];
                }
                //target is existing stack object. (where we get the interaction/tree from)
                var callback = new VMActionCallback(context.VM, interaction, context.StackObject, context.StackObject, 
                    context.Caller, true, (operand.InteractionCallback == 252));
                callback.Run(obj);
            }
            else context.StackObject = obj;

            if (operand.PersistInDB)
            {
                var vm = context.VM;
                if (vm.GlobalLink != null)
                {
                    vm.GlobalLink.RegisterNewObject(vm, obj, (short objID, uint pid) =>
                    {
                        vm.SendCommand(new VMNetUpdatePersistStateCmd()
                        {
                            ObjectID = objID,
                            PersistID = pid
                        });
                    });
                }
            }
            if (operand.UseNeighbor) {
                ((VMAvatar)(obj)).InheritNeighbor(neigh, context.VM.CurrentFamily);
            }

            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMCreateObjectInstanceOperand : VMPrimitiveOperand
    {
        public uint GUID { get; set; }
        public VMCreateObjectPosition Position { get; set; }
        public byte Flags;
        public byte LocalToUse { get; set; }
        public byte InteractionCallback { get; set; }


        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                GUID = io.ReadUInt32();
                Position = (VMCreateObjectPosition)io.ReadByte();
                Flags = io.ReadByte();
                LocalToUse = io.ReadByte();
                InteractionCallback = io.ReadByte();
            }
        }

        public void Write(byte[] bytes) {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(GUID);
                io.Write((byte)Position);
                io.Write(Flags);
                io.Write(LocalToUse);
                io.Write(InteractionCallback);
            }
        }

        public bool PassObjectIds
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

        public bool UseNeighbor
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

        public bool FailIfNonEmpty
        {
            get
            {
                return (Flags & 8) == 8;
            }
            set
            {
                if (value) Flags |= 8;
                else Flags &= unchecked((byte)~8);
            }
        }

        public bool PassTemp0
        {
            get
            {
                return (Flags & 16) == 16;
            }
            set
            {
                if (value) Flags |= 16;
                else Flags &= unchecked((byte)~16);
            }
        }

        public bool FaceStackObjDir
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

        public bool ReturnImmediately
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

        public bool PersistInDB
        {
            get
            {
                return (Flags & 128) == 128;
            }
            set
            {
                if (value) Flags |= 128;
                else Flags &= unchecked((byte)~128);
            }
        }
    }
    public enum VMCreateObjectPosition
    {
        InFrontOfMe = 0,
        OnTopOfMe = 1,
        InMyHand = 2,
        InFrontOfStackObject = 3,
        InSlot0OfStackObject = 4,
        UnderneathMe = 5,
        OutOfWorld = 6,
        BelowObjectInStackParam0 = 7,
        BelowObjectInLocal = 8,
        NextToMeInDirectionOfLocal = 9
    }
}
