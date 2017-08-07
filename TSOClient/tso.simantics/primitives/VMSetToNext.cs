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
using System.IO;
using FSO.LotView.Model;

namespace FSO.SimAntics.Primitives
{

    public class VMSetToNext : VMPrimitiveHandler
    {
        //position steps for object adjacent to object in local
        private static Point[] AdjStep =
        {
            new Point(0, -1),
            new Point(1, 0),
            new Point(0, 1),
            new Point(-1, 0),
        };
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMSetToNextOperand)args;
            var targetValue = VMMemory.GetVariable(context, operand.TargetOwner, operand.TargetData);
            var entities = context.VM.Entities;

            VMEntity Pointer = context.VM.GetObjectById(targetValue);

            //re-evaluation of what this actually does:
            //tries to find the next object id (from the previous) that meets a specific condition.
            //the previous object id is supplied via the target variable
            //
            //we should take the first result with object id > targetValue.

            if (operand.SearchType == VMSetToNextSearchType.PartOfAMultipartTile) {
                var target = context.VM.GetObjectById(targetValue);
                if (target == null || (!target.MultitileGroup.MultiTile)) return VMPrimitiveExitCode.GOTO_FALSE; //single part
                else
                {
                    var group = target.MultitileGroup.Objects;
                    bool found = false;
                    short bestID = 0;
                    short smallestID = 0;
                    for (int i = 0; i < group.Count; i++)
                    {
                        var temp = group[i];
                        if (temp.ObjectID < smallestID || smallestID == 0) smallestID = temp.ObjectID;
                        if (temp.ObjectID > targetValue)
                        {
                            if ((!found) || (temp.ObjectID < bestID))
                            {
                                found = true;
                                bestID = temp.ObjectID;
                            }
                        }
                    }
                    if (found)
                    {
                        VMMemory.SetVariable(context, operand.TargetOwner, operand.TargetData, bestID);
                        return VMPrimitiveExitCode.GOTO_TRUE;
                    }
                    else
                    {
                        VMMemory.SetVariable(context, operand.TargetOwner, operand.TargetData, smallestID);
                        return VMPrimitiveExitCode.GOTO_TRUE;
                    }
                }
            }
            else if (operand.SearchType == VMSetToNextSearchType.ObjectAdjacentToObjectInLocal)
            {
                VMEntity anchor = context.VM.GetObjectById((short)context.Locals[operand.Local]);
                int ptrDir = -1;

                targetValue = 0;
                if (Pointer != null)
                {
                    ptrDir = getAdjDir(anchor, Pointer);
                    if (ptrDir == 3) return VMPrimitiveExitCode.GOTO_FALSE; //reached end
                }

                //iterate through all following dirs til we find an object
                for (int i = ptrDir + 1; i < 4; i++)
                {
                    var off = AdjStep[i];
                    var adj = context.VM.Context.ObjectQueries.GetObjectsAt(LotTilePos.FromBigTile(
                        (short)(anchor.Position.TileX + off.X),
                        (short)(anchor.Position.TileY + off.Y),
                        anchor.Position.Level));

                    if (adj != null && adj.Count > 0)
                    {
                        //lists are ordered by object id. first is the smallest.
                        VMMemory.SetVariable(context, operand.TargetOwner, operand.TargetData, adj[0].ObjectID);
                        return VMPrimitiveExitCode.GOTO_TRUE;
                    }
                }
                return VMPrimitiveExitCode.GOTO_FALSE;
            } else if (operand.SearchType == VMSetToNextSearchType.Career)
            {
                var next = Content.Content.Get().Jobs.SetToNext(targetValue);
                if (next < 0) return VMPrimitiveExitCode.GOTO_FALSE;
                VMMemory.SetVariable(context, operand.TargetOwner, operand.TargetData, next);
                return VMPrimitiveExitCode.GOTO_TRUE;
            } else if (operand.SearchType == VMSetToNextSearchType.NeighborId)
            {
                var next = Content.Content.Get().Neighborhood.SetToNext(targetValue);
                if (next < 0) return VMPrimitiveExitCode.GOTO_FALSE;
                VMMemory.SetVariable(context, operand.TargetOwner, operand.TargetData, next);
                return VMPrimitiveExitCode.GOTO_TRUE;
            }
            else if (operand.SearchType == VMSetToNextSearchType.NeighborOfType)
            {
                var next = Content.Content.Get().Neighborhood.SetToNext(targetValue, operand.GUID);
                if (next < 0) return VMPrimitiveExitCode.GOTO_FALSE;
                VMMemory.SetVariable(context, operand.TargetOwner, operand.TargetData, next);
                return VMPrimitiveExitCode.GOTO_TRUE;
            } else {

                //if we've cached the search type, use that instead of all objects
                switch (operand.SearchType)
                {
                    case VMSetToNextSearchType.ObjectOnSameTile:
                        entities = context.VM.Context.ObjectQueries.GetObjectsAt(Pointer.Position); break;
                    case VMSetToNextSearchType.Person:
                    case VMSetToNextSearchType.FamilyMember:
                        entities = context.VM.Context.ObjectQueries.Avatars; break;
                    case VMSetToNextSearchType.ObjectOfType:
                        entities = context.VM.Context.ObjectQueries.GetObjectsByGUID(operand.GUID); break;
                    case VMSetToNextSearchType.ObjectWithCategoryEqualToSP0:
                        entities = context.VM.Context.ObjectQueries.GetObjectsByCategory(context.Args[0]); break;
                    default:
                        break;
                }
                if (entities == null) return VMPrimitiveExitCode.GOTO_FALSE;

                bool loop = (operand.SearchType == VMSetToNextSearchType.ObjectOnSameTile);
                VMEntity first = null;

                for (int i=0; i<entities.Count; i++) //generic search through all objects
                {
                    var temp = entities[i];
                    bool found = false;
                    if (temp.ObjectID > targetValue || loop)
                    {
                        switch (operand.SearchType)
                        { //manual search types
                            case VMSetToNextSearchType.NonPerson:
                                found = (temp is VMGameObject);
                                break;
                            case VMSetToNextSearchType.ClosestHouse:
                                return VMPrimitiveExitCode.GOTO_FALSE;
                                throw new VMSimanticsException("Not implemented!", context);
                            case VMSetToNextSearchType.FamilyMember:
                                found = context.VM.CurrentFamily?.FamilyGUIDs?.Contains(((VMAvatar)temp).Object.OBJ.GUID) ?? false;
                                break;
                            default:
                                //set to next object, or cached search.
                                found = true; break;
                        }
                        if (temp.ObjectID <= targetValue && found)
                        {
                            //remember the first element in case we need to loop back to it (set to next tile on same location)
                            if (first == null) first = temp; 
                            found = false;
                        }
                    }
                    if (found)
                    {
                        VMMemory.SetVariable(context, operand.TargetOwner, operand.TargetData, temp.ObjectID);
                        return VMPrimitiveExitCode.GOTO_TRUE;
                    }
                }

                if (loop)
                {
                    if (first == null) return VMPrimitiveExitCode.GOTO_FALSE; //no elements of this kind at all.
                    else
                    {
                        VMMemory.SetVariable(context, operand.TargetOwner, operand.TargetData, first.ObjectID); //set to loop, so go back to lowest obj id.
                        return VMPrimitiveExitCode.GOTO_TRUE;
                    }
                    //loop around
                }

            }
            return VMPrimitiveExitCode.GOTO_FALSE; //ran out of objects to test
        }

        private int getAdjDir(VMEntity src, VMEntity dest)
        {
            int diffX = dest.Position.TileX - src.Position.TileX;
            int diffY = dest.Position.TileY - src.Position.TileY;

            return getAdjDir(diffX, diffY);
        }

        private int getAdjDir(int diffX, int diffY)
        {

            //negative y is anchor
            //positive x is 90 degrees

            return (diffX == 0) ?
                ((diffY < 0) ? 0 : 2) :
                ((diffX < 0) ? 3 : 1);
        }

    }

    public class VMSetToNextOperand : VMPrimitiveOperand
    {
        public uint GUID { get; set; }
        public byte Flags { get; set; }
        public VMVariableScope TargetOwner { get; set; }
        public byte Local { get; set; }
        public byte TargetData { get; set; }
        public VMSetToNextSearchType SearchType {
            get { return (VMSetToNextSearchType)(Flags & 0x7F); } set { Flags = (byte)(0x80 | ((byte)value & 0x7F)); } }

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes){
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN)){

                this.GUID = io.ReadUInt32();
                //132 was object of type
                this.Flags = io.ReadByte();
                this.TargetOwner = (VMVariableScope)io.ReadByte();
                this.Local = io.ReadByte();
                this.TargetData = io.ReadByte();

                if ((Flags & 0x80) == 0)
                {
                    //clobber this, we should always set flag for saving.
                    Flags |= 0x80;
                    TargetOwner = VMVariableScope.StackObjectID;
                    TargetData = 0;
                }
            }
        }

        public void Write(byte[] bytes) {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(GUID);
                io.Write(Flags);
                io.Write((byte)TargetOwner);
                io.Write(Local);
                io.Write(TargetData);
            }
        }
        #endregion
    }


    public enum VMSetToNextSearchType
    {
        Object = 0,
        Person = 1,
        NonPerson = 2,
        PartOfAMultipartTile = 3,
        ObjectOfType = 4,
        NeighborId = 5,
        ObjectWithCategoryEqualToSP0 = 6,
        NeighborOfType = 7,
        ObjectOnSameTile = 8,
        ObjectAdjacentToObjectInLocal = 9,
        Career = 10,
        ClosestHouse = 11,
        FamilyMember = 12, //TS1.5 or higher
    }
}
