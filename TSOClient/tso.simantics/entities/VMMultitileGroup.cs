/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.LotView.Model;
using Microsoft.Xna.Framework;
using FSO.LotView.Components;
using FSO.SimAntics.Model;
using FSO.SimAntics.Marshals;
using FSO.SimAntics.Model.TSOPlatform;

namespace FSO.SimAntics.Entities
{
    /// <summary>
    /// Ties multiple entities together with a common name and set of repositioning functions.
    /// </summary>
    public class VMMultitileGroup
    {
        public bool MultiTile;
        public string Name = "";
        public int Price
        {
            get
            {
                var wear = (BaseObject?.TSOState as VMTSOObjectState)?.Wear ?? (20*4);
                var value = Math.Max(0, Math.Min(InitialPrice, (InitialPrice * (400 - wear)) / 400));
                return value;
            }
        }
        public int InitialPrice;
        public int SalePrice = -1;
        public List<VMEntity> Objects = new List<VMEntity>();
        public List<LotTilePos> Offsets = new List<LotTilePos>();

        public VMEntity BaseObject
        {
            get
            {
                for (int i = 0; i < Objects.Count(); i++)
                {
                    var sub = Objects[i];
                    if (sub.Object.OBJ.MyLeadObject > 0) return sub;
                }

                for (int i = 0; i < Objects.Count(); i++)
                {
                    var sub = Objects[i];
                    if (Offsets[i] == new LotTilePos()) return sub;
                }
                return Objects.FirstOrDefault();
            }
        }

        public VMMultitileGroup() { }

        public void AddObject(VMEntity obj)
        {
            AddDynamicObject(obj, 
                new LotTilePos((short)((sbyte)(((ushort)obj.Object.OBJ.SubIndex) >> 8) * 16), 
                (short)((sbyte)(((ushort)obj.Object.OBJ.SubIndex) & 0xFF) * 16), 
                (sbyte)obj.Object.OBJ.LevelOffset));
        }

        public void AddDynamicObject(VMEntity obj, LotTilePos offset)
        {
            Objects.Add(obj);
            Offsets.Add(offset);
        }

        public void RemoveObject(VMEntity obj)
        {
            int index = Objects.IndexOf(obj);
            if (index != -1)
            {
                Objects.RemoveAt(index);
                Offsets.RemoveAt(index);
            }
        }

        public Vector3[] GetBasePositions()
        {
            Vector3[] positions = new Vector3[Objects.Count];
            for (int i = 0; i < Objects.Count(); i++)
            {
                ushort sub = (ushort)Objects[i].Object.OBJ.SubIndex;
                positions[i] = new Vector3(Offsets[i].x/16, Offsets[i].y/16, 0);
            }
            return positions;
        }
        
        public Rectangle? LightBounds()
        {
            var bObj = Objects[0];
            if (bObj.Container != null || bObj is VMAvatar) return null;

            Rectangle? result = null;
            foreach (var obj in Objects)
            {
                var flags = (VMEntityFlags)obj.GetValue(VMStackObjectVariable.Flags);
                if ((flags & VMEntityFlags.DisallowPersonIntersection) > 0 || (flags & VMEntityFlags.AllowPersonIntersection) == 0)
                {
                    var footprint = obj.Footprint;
                    if (footprint != null)
                    {
                        var combR = new Rectangle(footprint.x1, footprint.y1, footprint.x2 - footprint.x1, footprint.y2 - footprint.y1);
                        if (result == null) result = combR;
                        else result = Rectangle.Union(result.Value, combR);
                    } 
                }
            }
            return result;
        }

        public VMEntity GetInteractionGroupLeader(VMEntity obj)
        {
            var group = obj.Object.OBJ.InteractionGroupID;
            if (group < 1) return obj;
            else
            {
                //find master for this group
                var master = Objects.FirstOrDefault(x => x.Object.OBJ.InteractionGroupID == -group);
                return (master == null) ? obj : master;
            }
        }

        public VMPlacementResult ChangePosition(LotTilePos pos, Direction direction, VMContext context, VMPlaceRequestFlags flags)
        {
            if (pos.Level > context.Architecture.Stories) return new VMPlacementResult(VMPlacementError.NotAllowedOnFloor);
            if (Objects.Count == 0) return new VMPlacementResult(VMPlacementError.Success);

            VMEntity[] OldContainers = new VMEntity[Objects.Count];
            short[] OldSlotNum = new short[Objects.Count];
            for (int i = 0; i < Objects.Count(); i++)
            {
                OldContainers[i] = Objects[i].Container;
                OldSlotNum[i] = Objects[i].ContainerSlot;
                Objects[i].PrePositionChange(context);
            }

            int Dir = 0;
            switch (direction)
            {
                case Direction.NORTH:
                    Dir = 0; break;
                case Direction.EAST:
                    Dir = 2; break;
                case Direction.SOUTH:
                    Dir = 4; break;
                case Direction.WEST:
                    Dir = 6; break;
            }

            Matrix rotMat = Matrix.CreateRotationZ((float)(Dir * Math.PI / 4.0));
            VMPlacementResult[] places = new VMPlacementResult[Objects.Count];

            var bObj = BaseObject;
            var bOff = Offsets[Objects.IndexOf(BaseObject)];
            var leadOff = new Vector3(bOff.x, bOff.y, 0);
            var offTotal = new Vector3();

            //TODO: optimize so we don't have to recalculate all of this
            if (pos != LotTilePos.OUT_OF_WORLD)
            {
                for (int i = 0; i < Objects.Count(); i++)
                {
                    var sub = Objects[i];
                    var off = new Vector3(Offsets[i].x, Offsets[i].y, 0);
                    off = Vector3.Transform(off-leadOff, rotMat);
                    offTotal += off;

                    var offPos = new LotTilePos((short)Math.Round(pos.x + off.X), (short)Math.Round(pos.y + off.Y), (sbyte)(pos.Level + Offsets[i].Level));
                    places[i] = sub.PositionValid(offPos, direction, context, flags);
                    if (places[i].Status != VMPlacementError.Success)
                    {
                        //go back to where we started: we're no longer out of world.
                        for (int j = 0; j < Objects.Count(); j++)
                        {
                            //need to restore slot we were in
                            if (OldContainers[j] != null) {
                                OldContainers[j].PlaceInSlot(Objects[j], OldSlotNum[j], false, context);
                            }
                            Objects[j].PositionChange(context, false);
                        }
                        return places[i];
                    }
                }
            } else
            {
                for (int i = 0; i < Objects.Count(); i++)
                {
                    var off = new Vector3(Offsets[i].x, Offsets[i].y, 0);
                    off = Vector3.Transform(off - leadOff, rotMat);
                    offTotal += off;
                }
            }

            offTotal /= Objects.Count(); //this is now the average offset
            //verification success

            for (int i = 0; i < Objects.Count(); i++)
            {
                var sub = Objects[i];
                var off = new Vector3(Offsets[i].x, Offsets[i].y, 0);
                off = Vector3.Transform(off-leadOff, rotMat);

                var offPos = (pos==LotTilePos.OUT_OF_WORLD)?
                    LotTilePos.OUT_OF_WORLD :
                    new LotTilePos((short)Math.Round(pos.x + off.X), (short)Math.Round(pos.y + off.Y), (sbyte)(pos.Level + Offsets[i].Level));

                if (VM.UseWorld) sub.WorldUI.MTOffset = off - offTotal;
                sub.SetIndivPosition(offPos, direction, context, places[i]);
            }
            for (int i = 0; i < Objects.Count(); i++) Objects[i].PositionChange(context, false);
            return new VMPlacementResult(VMPlacementError.Success);
        }

        public void SetVisualPosition(Vector3 pos, Direction direction, VMContext context)
        {
            if (Objects.Count == 0) return;
            int Dir = 0;
            switch (direction)
            {
                case Direction.NORTH:
                    Dir = 0; break;
                case Direction.EAST:
                    Dir = 2; break;
                case Direction.SOUTH:
                    Dir = 4; break;
                case Direction.WEST:
                    Dir = 6; break;
            }

            Matrix rotMat = Matrix.CreateRotationZ((float)(Dir * Math.PI / 4.0));
            var bObj = BaseObject;
            var bOff = Offsets[Objects.IndexOf(BaseObject)];
            var leadOff = new Vector3(bOff.x/16f, bOff.y/16f, 0);

            for (int i = 0; i < Objects.Count(); i++)
            {
                var sub = Objects[i];
                var off = new Vector3(Offsets[i].x/16f, Offsets[i].y/16f, sub.Object.OBJ.LevelOffset*2.95f);
                off = Vector3.Transform(off-leadOff, rotMat);

                if (VM.UseWorld) sub.WorldUI.Level = (sbyte)(Math.Round(pos.Z / 2.95f)+1);
                sub.Direction = direction;
                sub.VisualPosition = pos + off;
            }
        }

        public void Combine(VMMultitileGroup other)
        {
            var bObj = BaseObject;

            int Dir = 0;
            switch (bObj.Direction)
            {
                case Direction.NORTH:
                    Dir = 0; break;
                case Direction.EAST:
                    Dir = 2; break;
                case Direction.SOUTH:
                    Dir = 4; break;
                case Direction.WEST:
                    Dir = 6; break;
            }
            Matrix rotMat = Matrix.CreateRotationZ((float)(-Dir * Math.PI / 4.0));
            foreach (var obj in other.Objects)
            {
                var diff = obj.Position - bObj.Position;
                var vec = new Vector3(diff.x, diff.y, 0);
                Vector3.Transform(vec, rotMat);
                AddDynamicObject(obj, new LotTilePos((short)Math.Round(vec.X), (short)Math.Round(vec.Y), diff.Level));
                obj.MultitileGroup = this;
                obj.Direction = bObj.Direction;
            }
        }

        public void ExecuteEntryPoint(int num, VMContext context)
        {
            for (int i = 0; i < Objects.Count; i++) Objects[i].ExecuteEntryPoint(num, context, true);
        }

        public void Delete(VMContext context)
        {
            var clone = new List<VMEntity>(Objects);
            foreach (var obj in clone)
            {
                obj.Delete(false, context);
            }
        }

        public void Init(VMContext context)
        {
            for (int i = 0; i < Objects.Count(); i++)
            {
                Objects[i].Init(context);
            }
        }

        #region VM Marshalling Functions
        public virtual VMMultitileGroupMarshal Save()
        {
            var objs = new short[Objects.Count];
            int i = 0;
            foreach (var obj in Objects) objs[i++] = obj.ObjectID;

            return new VMMultitileGroupMarshal
            {
                MultiTile = MultiTile,
                Name = Name,
                Price = InitialPrice,
                SalePrice = SalePrice,
                Objects = objs,
                Offsets = Offsets.ToArray()
            };
        }

        public virtual void Load(VMMultitileGroupMarshal input, VMContext context)
        {
            MultiTile = input.MultiTile;
            Name = input.Name;
            InitialPrice = input.Price;
            SalePrice = input.SalePrice;
            if (SalePrice == 0) SalePrice = -1;
            Objects = new List<VMEntity>();
            for (int i= 0; i<input.Objects.Length; i++)
            {
                var id = input.Objects[i];
                var obj = context.VM.GetObjectById(id);
                if (obj == null) continue;
                Objects.Add(obj);
                Offsets.Add(input.Offsets[i]);
                obj.MultitileGroup = this;
            }
        }

        public VMMultitileGroup(VMMultitileGroupMarshal input, VMContext context)
        {
            Load(input, context);
        }
        #endregion
    }
}
