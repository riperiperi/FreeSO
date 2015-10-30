/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Content;
using FSO.LotView.Components;
using FSO.LotView.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.Model;
using FSO.Common.Utils;
using FSO.Content.Model;
using FSO.SimAntics.Model.Routing;
using FSO.SimAntics.Marshals;

namespace FSO.SimAntics
{
    public class VMGameObject : VMEntity
    {

        /** Definition **/

        public VMGameObject(GameObject def, ObjectComponent worldUI) : base(def)
        {
            this.WorldUI = worldUI;
        }

        public override void SetDynamicSpriteFlag(ushort index, bool set)
        {
            base.SetDynamicSpriteFlag(index, set);
            if (this.WorldUI != null){
                ((ObjectComponent)this.WorldUI).DynamicSpriteFlags = this.DynamicSpriteFlags;
            }
        }

        public override bool SetValue(VMStackObjectVariable var, short value)
        {
            return base.SetValue(var, value);
        }

        public override short GetValue(VMStackObjectVariable var)
        {
            return base.GetValue(var);
        }

        public bool RefreshGraphic()
        {
            if (!UseWorld) return true;
            var newGraphic = Object.OBJ.BaseGraphicID + ObjectData[(int)VMStackObjectVariable.Graphic];
            var dgrp = Object.Resource.Get<DGRP>((ushort)newGraphic);
            if (dgrp != null)
            {
                ((ObjectComponent)WorldUI).DGRP = dgrp;
                return true;
            }
            return false;
        }


        public override void Init(FSO.SimAntics.VMContext context){
            if (UseWorld) ((ObjectComponent)WorldUI).ObjectID = ObjectID;
            if (Slots != null && Slots.Slots.ContainsKey(0))
            {
                Contained = new VMEntity[Slots.Slots[0].Count];
                if (UseWorld) ((ObjectComponent)WorldUI).ContainerSlots = Slots.Slots[0];
            }

            base.Init(context);
        }

        public override float RadianDirection
        {
            get
            {
                double dir = Math.Log((double)Direction, 2.0)*(Math.PI/4);
                if (dir > Math.PI) dir -= 2*Math.PI;
                return (float)dir;
            }
            set
            {
                Direction = (Direction)(1 << (int)(Math.Round(DirectionUtils.PosMod(value, (float)Math.PI * 2) / 8) % 8));
            }
        }

        private Direction _Direction;
        public override Direction Direction { 
            get { return _Direction; }
            set {
                _Direction = value;
                if (UseWorld) ((ObjectComponent)WorldUI).Direction = value;
            }
        }

        public override Vector3 VisualPosition
        {
            get { return (UseWorld)?(WorldUI.Position + new Vector3(0.5f, 0.5f, 0f)):new Vector3(); }
            set { if (UseWorld) WorldUI.Position = value-new Vector3(0.5f, 0.5f, 0f); }
        }

        public override string ToString()
        {
            var strings = Object.Resource.Get<CTSS>(Object.OBJ.CatalogStringsID);
            if (strings != null){
                return strings.GetString(0);
            }
            var label = Object.OBJ.ChunkLabel;
            if (label != null && label.Length > 0){
                return label.TrimEnd('\0');
            }
            return Object.OBJ.GUID.ToString("X");
        }

        // Begin Container SLOTs interface

        public override int TotalSlots()
        {
            if (Contained == null) return 0;
            return Contained.Length;
        }

        public override void PlaceInSlot(VMEntity obj, int slot, bool cleanOld, VMContext context)
        {
            if (cleanOld) obj.PrePositionChange(context);

            if (Contained != null)
            {
                if (slot > -1 && slot < Contained.Length)
                {
                    if (!obj.GhostImage)
                    {
                        Contained[slot] = obj;
                        obj.Container = this;
                        obj.ContainerSlot = (short)slot;
                    }

                    if (UseWorld)
                    {
                        obj.WorldUI.Container = this.WorldUI;
                        obj.WorldUI.ContainerSlot = slot;
                    }
                    obj.Position = Position; //TODO: is physical position the same as the slot offset position?
                }
            }
        }

        public override VMEntity GetSlot(int slot)
        {
            if (Contained != null)
            {
                if (slot > -1 && slot < Contained.Length)
                {
                    return Contained[slot];
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public override int GetSlotHeight(int slot)
        {
            if (slot < TotalSlots()) return Slots.Slots[0][0].Height;
            else return -1;
        }

        public override void ClearSlot(int slot)
        {
            if (Contained != null)
            {
                if (slot > -1 && slot < Contained.Length)
                {
                    if (Contained[slot] == null) return; //what..
                    Contained[slot].Container = null;
                    Contained[slot].ContainerSlot = -1;
                    if (UseWorld)
                    {
                        Contained[slot].WorldUI.Container = null;
                        Contained[slot].WorldUI.ContainerSlot = -1;
                    }
                    Contained[slot] = null;
                }
            }
        }

        // End Container SLOTs interface

        public override Texture2D GetIcon(GraphicsDevice gd)
        {
            var bmp = Object.Resource.Get<BMP>(Object.OBJ.CatalogStringsID);
            if (bmp != null) return bmp.GetTexture(gd);
            else return null;
        }

        public override void PrePositionChange(VMContext context)
        {
            Footprint = null;
            if (GhostImage && UseWorld)
            {
                if (WorldUI.Container != null)
                {
                    WorldUI.Container = null;
                    WorldUI.ContainerSlot = 0;
                }
                return; 
            }
            if (Container != null)
            {
                Container.ClearSlot(ContainerSlot);
                return;
            }
            if (Position == LotTilePos.OUT_OF_WORLD) return;

            var arch = context.Architecture;
            if (((VMEntityFlags2)ObjectData[(int)VMStackObjectVariable.FlagField2] & (VMEntityFlags2.ArchitectualWindow | VMEntityFlags2.ArchitectualDoor)) > 0)
            { //if wall or door, attempt to place style on wall
                var placeFlags = (WallPlacementFlags)ObjectData[(int)VMStackObjectVariable.WallPlacementFlags];
                var dir = DirectionToWallOff(Direction);
                if ((placeFlags & WallPlacementFlags.WallRequiredInFront) > 0) SetWallStyle((dir) % 4, arch, 0);
                if ((placeFlags & WallPlacementFlags.WallRequiredOnRight) > 0) SetWallStyle((dir + 1) % 4, arch, 0);
                if ((placeFlags & WallPlacementFlags.WallRequiredBehind) > 0) SetWallStyle((dir + 2) % 4, arch, 0);
                if ((placeFlags & WallPlacementFlags.WallRequiredOnLeft) > 0) SetWallStyle((dir + 3) % 4, arch, 0);
            }
            SetWallUse(arch, false);
            if (GetValue(VMStackObjectVariable.Category) == 8) context.Architecture.SetObjectSupported(Position.TileX, Position.TileY, Position.Level, false);

            context.UnregisterObjectPos(this);
            base.PrePositionChange(context);
        }

        public override VMObstacle GetObstacle(LotTilePos pos, Direction dir)
        {
            if (GetFlag(VMEntityFlags.HasZeroExtent)) return null;

            var idir = (DirectionToWallOff(dir)*4);

            uint rotatedFPM = (uint)(Object.OBJ.FootprintMask << idir);
            rotatedFPM = (rotatedFPM >> 16) | (rotatedFPM & 0xFFFF);

            int tileWidth = Object.OBJ.TileWidth / 2;
            if (tileWidth == 0) tileWidth = 8;

            return new VMObstacle(
                (pos.x + tileWidth) - ((int)(rotatedFPM >> 4) & 0xF),
                (pos.y + tileWidth) - ((int)(rotatedFPM >> 8) & 0xF),
                (pos.x - tileWidth) + ((int)(rotatedFPM >> 12) & 0xF),
                (pos.y - tileWidth) + ((int)rotatedFPM & 0xF));
                
        }

        public override void PositionChange(VMContext context, bool noEntryPoint)
        {
            if (GhostImage) return;
            if (Container != null) return;
            if (Position == LotTilePos.OUT_OF_WORLD) return;

            var arch = context.Architecture;
            if (((VMEntityFlags2)ObjectData[(int)VMStackObjectVariable.FlagField2] & (VMEntityFlags2.ArchitectualWindow | VMEntityFlags2.ArchitectualDoor)) > 0)
            { //if wall or door, attempt to place style on wall

                if (Object.OBJ.WallStyle > 21 && Object.OBJ.WallStyle < 256)
                { //first thing's first, is the style between 22-255 inclusive? If it is, then the style is stored in the object. Need to load its sprites and change the id for the objd.
                    var id = Object.OBJ.WallStyleSpriteID;
                    var style = new WallStyle()
                    {
                        WallsUpFar = Object.Resource.Get<SPR>(id),
                        WallsUpMedium = Object.Resource.Get<SPR>((ushort)(id + 1)),
                        WallsUpNear = Object.Resource.Get<SPR>((ushort)(id + 2)),
                        WallsDownFar = Object.Resource.Get<SPR>((ushort)(id + 3)),
                        WallsDownMedium = Object.Resource.Get<SPR>((ushort)(id + 4)),
                        WallsDownNear = Object.Resource.Get<SPR>((ushort)(id + 5))
                    };
                    Object.OBJ.WallStyle = FSO.Content.Content.Get().WorldWalls.AddDynamicWallStyle(style);
                }

                var placeFlags = (WallPlacementFlags)ObjectData[(int)VMStackObjectVariable.WallPlacementFlags];
                var dir = DirectionToWallOff(Direction);
                if ((placeFlags & WallPlacementFlags.WallRequiredInFront) > 0) SetWallStyle((dir) % 4, arch, Object.OBJ.WallStyle);
                if ((placeFlags & WallPlacementFlags.WallRequiredOnRight) > 0) SetWallStyle((dir + 1) % 4, arch, Object.OBJ.WallStyle);
                if ((placeFlags & WallPlacementFlags.WallRequiredBehind) > 0) SetWallStyle((dir + 2) % 4, arch, Object.OBJ.WallStyle);
                if ((placeFlags & WallPlacementFlags.WallRequiredOnLeft) > 0) SetWallStyle((dir + 3) % 4, arch, Object.OBJ.WallStyle);
            }
            SetWallUse(arch, true);
            if (GetValue(VMStackObjectVariable.Category) == 8) context.Architecture.SetObjectSupported(Position.TileX, Position.TileY, Position.Level, true);

            context.RegisterObjectPos(this);

            base.PositionChange(context, noEntryPoint);
        }


        #region VM Marshalling Functions
        public VMGameObjectMarshal Save()
        {
            var gameObj = new VMGameObjectMarshal { Direction = Direction };
            SaveEnt(gameObj);
            return gameObj;
        }

        public void Load(VMGameObjectMarshal input)
        {
            base.Load(input);
            Position = Position;
            Direction = input.Direction;
            if (UseWorld)
            {
                ((ObjectComponent)this.WorldUI).DynamicSpriteFlags = this.DynamicSpriteFlags;
                ((ObjectComponent)WorldUI).ObjectID = ObjectID;
                if (Slots != null && Slots.Slots.ContainsKey(0)) ((ObjectComponent)WorldUI).ContainerSlots = Slots.Slots[0];
                RefreshGraphic();
            }
        }
        #endregion
    }
}
