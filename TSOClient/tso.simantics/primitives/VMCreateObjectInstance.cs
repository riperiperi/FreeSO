using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Files.utils;
using TSO.Simantics.engine.scopes;
using TSO.Simantics.engine.utils;
using tso.world.model;
using Microsoft.Xna.Framework;

namespace TSO.Simantics.engine.primitives
{
    public class VMCreateObjectInstance : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            var operand = context.GetCurrentOperand<VMCreateObjectInstanceOperand>();
            short x = 0;
            short y = 0;
            sbyte level = 0;
            Direction dir;

            switch (operand.Position)
            {
                case VMCreateObjectPosition.UnderneathMe:
                case VMCreateObjectPosition.OnTopOfMe:
                    var pos = context.Caller.Position;
                    x = pos.x;
                    y = pos.y;
                    level = 0; //for now..
                    dir = Direction.NORTH;
                    break;
                case VMCreateObjectPosition.BelowObjectInLocal:
                    var pos2 = context.VM.GetObjectById((short)context.Locals[operand.LocalToUse]).Position;
                    x = pos2.x;
                    y = pos2.y;
                    level = 0; //for now..
                    dir = Direction.NORTH;
                    break;
                case VMCreateObjectPosition.OutOfWorld:
                    x = 0; //need a system for out of world objects.
                    y = 0;
                    level = 0; //for now..
                    dir = Direction.NORTH;
                    break;
                case VMCreateObjectPosition.InSlot0OfStackObject:
                case VMCreateObjectPosition.InMyHand:
                    x = 0; //need a system for out of world objects.
                    y = 0;
                    level = 0; //for no..
                    dir = Direction.NORTH;
                    //this object should start in slot 0 of the stack object!
                    //we have to create it first tho so hold your horses
                    break;
                case VMCreateObjectPosition.InFrontOfStackObject:
                case VMCreateObjectPosition.InFrontOfMe:
                    var objp = (operand.Position == VMCreateObjectPosition.InFrontOfStackObject)?context.StackObject:context.Caller;
                    var location = objp.Position;
                    x = location.x;
                    y = location.y;
                    switch (objp.Direction)
                    {
                        case tso.world.model.Direction.SOUTH:
                            y += 16;
                            break;
                        case tso.world.model.Direction.WEST:
                            x -= 16;
                            break;
                        case tso.world.model.Direction.EAST:
                            x += 16;
                            break;
                        case tso.world.model.Direction.NORTH:
                            y -= 16;
                            break;
                    }

                    level = 0;
                    dir = objp.Direction;
                    break;
                default:
                    throw new VMSimanticsException("Where do I put this??", context);
            }

            var obj = context.VM.Context.CreateObjectInstance(operand.GUID, new LotTilePos(x, y, level), dir).Objects[0];

            if (operand.PassObjectIds)
            {
                obj.MainStackOBJ = context.StackObject.ObjectID;
                obj.MainParam = context.Caller.ObjectID;
            }
            if (operand.PassTemp0) obj.MainParam = context.Thread.TempRegisters[0];

            obj.Init(context.VM.Context);

            if (operand.Position == VMCreateObjectPosition.InSlot0OfStackObject) context.StackObject.PlaceInSlot(obj, 0);
            else if (operand.Position == VMCreateObjectPosition.InMyHand) context.Caller.PlaceInSlot(obj, 0);

            if ((operand.Flags & (1 << 6)) > 0)
            {
                var interaction = operand.InteractionCallback;
                if (interaction == 254)
                {
                    var temp = context.Caller.Thread.Queue[0].InteractionNumber;
                    if (temp == -1) throw new VMSimanticsException("Set callback as 'this interaction' when queue item has no interaction number!", context);
                    interaction = (byte)temp;
                }
                var callback = new VMActionCallback(context.VM, interaction, context.Callee, context.StackObject, context.Caller, true);
                callback.Run(obj);
            }
            else context.StackObject = obj;

            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMCreateObjectInstanceOperand : VMPrimitiveOperand
    {
        public uint GUID;
        public VMCreateObjectPosition Position;
        public byte Flags;
        public byte LocalToUse;
        public byte InteractionCallback;

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

        public bool PassObjectIds
        {
            get
            {
                return (Flags & 2) == 2;
            }
        }

        public bool PassTemp0
        {
            get
            {
                return (Flags & 16) == 16;
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
