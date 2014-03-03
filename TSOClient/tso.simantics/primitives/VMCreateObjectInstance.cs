using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Files.utils;
using TSO.Simantics.engine.scopes;
using TSO.Simantics.engine.utils;
using tso.world.model;

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
                    x = (short)context.Caller.Position.X;
                    y = (short)context.Caller.Position.Y;
                    level = 0; //for now..
                    dir = Direction.RightBack;
                    break;
                case VMCreateObjectPosition.OutOfWorld:
                    x = 0; //need a system for out of world objects.
                    y = 0;
                    level = 0; //for now..
                    dir = Direction.RightBack;
                    break;
                default:
                    throw new Exception("Where do I put this??");
            }

            var obj = context.VM.Context.CreateObjectInstance(operand.GUID, x, y, level, dir);
            obj.Init(context.VM.Context);
            if ((operand.Flags & (1 << 6)) > 0)
            {
                var interaction = operand.InteractionCallback;
                if (interaction == 254)
                {
                    var temp = context.Caller.Thread.Queue[0].InteractionNumber;
                    if (temp == -1) throw new Exception("Set callback as 'this interaction' when queue item has no interaction number!");
                    interaction = (byte)temp;
                }
                obj.Thread.Queue[0].Callback = new VMActionCallback(context.VM, interaction, context.Callee, context.StackObject, true);
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
