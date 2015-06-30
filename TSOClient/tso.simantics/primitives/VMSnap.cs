using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Simantics.engine;
using TSO.Files.utils;
using TSO.Simantics.engine.scopes;
using TSO.Simantics.engine.utils;
using Microsoft.Xna.Framework;
using tso.world.model;
using TSO.Files.formats.iff.chunks;
using TSO.Simantics.model;

namespace TSO.Simantics.primitives
{
    public class VMSnap : VMPrimitiveHandler 
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            var operand = context.GetCurrentOperand<VMSnapOperand>();
            var avatar = (VMAvatar)context.Caller; //todo, can sometimes be an object?? see roaches object tile movement, snaps to its own routing slot
            var obj = context.StackObject;

            var prevContain = context.VM.GetObjectById(avatar.GetValue(VMStackObjectVariable.ContainerId));
            if (prevContain != null) //if we are contained in an object, drop out of it.
            {
                prevContain.ClearSlot(avatar.GetValue(VMStackObjectVariable.SlotNumber));
            }

            SLOTItem slot;
            VMFindLocationResult location;
            switch (operand.Mode)
            {
                case 0:
                    slot = VMMemory.GetSlot(context, VMSlotScope.StackVariable, operand.Index);
                    location = VMSlotParser.FindAvaliableLocations(obj, slot, context.VM.Context)[0];
                    avatar.Position = location.Position;
                    avatar.RadianDirection = location.RadianDirection;
                break;
                case 1: //be contained on stack object
                    context.StackObject.PlaceInSlot(context.Caller, 0);
                break;
                case 2:
                    var pos = obj.Position;
                    switch (obj.Direction)
                    {
                        case tso.world.model.Direction.SOUTH:
                            pos.y += 16;
                            break;
                        case tso.world.model.Direction.WEST:
                            pos.x -= 16;
                            break;
                        case tso.world.model.Direction.EAST:
                            pos.x += 16;
                            break;
                        case tso.world.model.Direction.NORTH:
                            pos.y -= 16;
                            break;
                    }
                    avatar.Direction = (Direction)(((int)obj.Direction << 4) | ((int)obj.Direction >> 4) & 255);
                    avatar.Position = pos;
                break;
                case 3:
                    slot = VMMemory.GetSlot(context, VMSlotScope.Literal, operand.Index);
                    var locations = VMSlotParser.FindAvaliableLocations(obj, slot, context.VM.Context); //chair seems to snap to position?
                    if (locations.Count > 0)
                    {
                        avatar.Position = locations[0].Position;
                        if ((slot.Rsflags & SLOTFlags.SnapToDirection) > 0) avatar.RadianDirection = locations[0].RadianDirection;
                    }
                    if (slot.SnapTargetSlot != -1) context.StackObject.PlaceInSlot(context.Caller, slot.SnapTargetSlot);
                break;
                case 4:
                    slot = VMMemory.GetSlot(context, VMSlotScope.Global, operand.Index);
                    location = VMSlotParser.FindAvaliableLocations(obj, slot, context.VM.Context)[0];
                    avatar.Position = location.Position;
                    avatar.RadianDirection = location.RadianDirection;
                break;
            }

            return VMPrimitiveExitCode.GOTO_TRUE; 
        }
    }

    public class VMSnapOperand : VMPrimitiveOperand
    {
        public ushort Index;
        public ushort Mode;
        public byte Flags;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                Index = io.ReadUInt16();
                Mode = io.ReadUInt16();
                Flags = io.ReadByte(); 
            }
        }
        #endregion
    }
}
