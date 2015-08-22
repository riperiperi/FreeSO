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

namespace FSO.SimAntics.Primitives
{
    public class VMFindLocationFor : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMFindLocationForOperand)args;
            var refObj = (operand.UseLocalAsRef) ? context.VM.GetObjectById((short)context.Locals[operand.Local]) : context.Caller;

            //short container = context.StackObject.GetValue(VMStackObjectVariable.ContainerId);
            //if (container != 0) context.VM.GetObjectById(container).ClearSlot(context.StackObject.GetValue(VMStackObjectVariable.SlotNumber)); //if object is in a slot, eject it

            var obj = context.StackObject;
            if (operand.Mode == 0) //todo: detect collisions and place close to intended position if AllowIntersection is false.
            { //default
                if (FindLocationFor(obj, refObj, context.VM.Context)) return VMPrimitiveExitCode.GOTO_TRUE;
                //search: expanding rectangle
                //var result = context.StackObject.SetPosition(new LotTilePos(refObj.Position), context.StackObject.Direction, context.VM.Context);

            }

            return VMPrimitiveExitCode.GOTO_FALSE;
        }

        public static bool FindLocationFor(VMEntity obj, VMEntity refObj, VMContext context)
        {
            for (int i = 0; i < 10; i++)
            {
                if (i == 0)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        if (obj.SetPosition(new LotTilePos(refObj.Position), (Direction)(1 << (j * 2)), context).Status == VMPlacementError.Success)
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
                                (Direction)(1 << ((j / 2) * 2)), context).Status == VMPlacementError.Success)
                                return true;
                        }
                    }

                    for (int y = 1 - i; y < i; y++)
                    {
                        for (int j = 0; j < 8; j++)
                        {
                            if (obj.SetPosition(LotTilePos.FromBigTile((short)(bPos.TileX + ((j % 2) * 2 - 1) * i), (short)(bPos.TileY + y), bPos.Level),
                                (Direction)(1 << ((j / 2) * 2)), context).Status == VMPlacementError.Success)
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
        public byte Mode;
        public byte Local;
        public byte Flags;

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
        #endregion

        public bool UseLocalAsRef
        {
            get
            {
                return (Flags & 1) == 1;
            }
        }

        public bool AllowIntersection
        {
            get
            {
                return (Flags & 2) == 2;
            }
        }

        public bool UserEditableTilesOnly
        {
            get
            {
                return (Flags & 4) == 4;
            }
        }
    }
}
