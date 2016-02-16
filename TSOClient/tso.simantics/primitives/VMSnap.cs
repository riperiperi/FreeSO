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
using FSO.SimAntics.Engine.Scopes;
using FSO.SimAntics.Engine.Utils;
using Microsoft.Xna.Framework;
using FSO.LotView.Model;
using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.Model;
using FSO.Common.Utils;
using System.IO;

namespace FSO.SimAntics.Primitives
{
    public class VMSnap : VMPrimitiveHandler 
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMSnapOperand)args;
            var avatar = context.Caller; //todo, can sometimes be an object?? see roaches object tile movement, snaps to its own routing slot
            var obj = context.StackObject;

            SLOTItem slot = null;

            switch (operand.Mode)
            {
                case VMSnapSlotScope.StackVariable:
                    slot = VMMemory.GetSlot(context, VMSlotScope.StackVariable, operand.Index);
                    break;
                case VMSnapSlotScope.BeContained:
                    context.StackObject.PlaceInSlot(context.Caller, 0, true, context.VM.Context);
                break;
                case VMSnapSlotScope.InFront:
                    slot = new SLOTItem { Type = 3, Standing = 1, MinProximity = 16, Rsflags = SLOTFlags.NORTH };
                    break;
                case VMSnapSlotScope.Literal:
                    slot = VMMemory.GetSlot(context, VMSlotScope.Literal, operand.Index);
                    break;
                case VMSnapSlotScope.Global:
                    slot = VMMemory.GetSlot(context, VMSlotScope.Global, operand.Index);
                    break;
            }

            if (operand.Mode != VMSnapSlotScope.BeContained)
            {
                var parser = new VMSlotParser(slot);
                var locations = parser.FindAvaliableLocations(obj, context.VM.Context, avatar);
                if (slot.SnapTargetSlot > -1)
                {
                    context.StackObject.PlaceInSlot(context.Caller, slot.SnapTargetSlot, true, context.VM.Context);
                    if (locations.Count > 0) avatar.RadianDirection = ((slot.Rsflags & SLOTFlags.SnapToDirection) > 0) ? locations[0].RadianDirection: avatar.RadianDirection;
                }
                else
                {
                    if (locations.Count > 0)
                    {
                        if (!SetPosition(avatar, locations[0].Position,
                            ((slot.Rsflags & SLOTFlags.SnapToDirection) > 0) ? locations[0].RadianDirection : avatar.RadianDirection,
                            context.VM.Context))
                            return VMPrimitiveExitCode.GOTO_FALSE;
                    }
                    else
                    {
                        avatar.SetValue(VMStackObjectVariable.PrimitiveResultID, (parser.Blocker == null) ? (short)0 : parser.Blocker.ObjectID);
                        return VMPrimitiveExitCode.GOTO_FALSE;
                    }
                }
            }

            return VMPrimitiveExitCode.GOTO_TRUE; 
        }

        private bool SetPosition(VMEntity entity, LotTilePos pos, Direction dir, VMContext context)
        {
            return SetPosition(entity, pos, (float)(Math.Round(Math.Log((double)dir, 2))*(Math.PI/4)), context);
        }

        private bool SetPosition(VMEntity entity, LotTilePos pos, float radDir, VMContext context)
        {
            var posChange = entity.SetPosition(pos, (Direction)(1 << (int)(Math.Round(DirectionUtils.PosMod(radDir, (float)Math.PI * 2) / (Math.PI/4)) % 8)), context);
            if (posChange.Status != VMPlacementError.Success)
            {
                entity.SetValue(VMStackObjectVariable.PrimitiveResultID, (posChange.Object == null) ? (short)0 : posChange.Object.ObjectID);
                return false;
            }
            if (entity is VMAvatar) entity.RadianDirection = radDir;
            return true;
        }
    }

    public class VMSnapOperand : VMPrimitiveOperand
    {
        public ushort Index { get; set; }
        public VMSnapSlotScope Mode { get; set; }
        public byte Flags { get; set; }

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                Index = io.ReadUInt16();
                Mode = (VMSnapSlotScope)io.ReadUInt16();
                Flags = io.ReadByte(); 
            }
        }

        public void Write(byte[] bytes) {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(Index);
                io.Write((ushort)Mode);
                io.Write(Flags);
            }
        }
        #endregion
    }

    public enum VMSnapSlotScope
    {
        StackVariable = 0,
        BeContained = 1,
        InFront = 2,
        Literal = 3,
        Global = 4
    }
}
