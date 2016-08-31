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
using FSO.SimAntics.Model.TSOPlatform;
using FSO.SimAntics.Marshals.Hollow;

namespace FSO.SimAntics
{
    public class VMGameObject : VMEntity
    {

        /** Definition **/

        public VMGameObject(GameObject def, ObjectComponent worldUI) : base(def)
        {
            this.WorldUI = worldUI;
            PlatformState = new VMTSOObjectState(); //todo: ts1 switch
        }

        public override void SetDynamicSpriteFlag(ushort index, bool set)
        {
            base.SetDynamicSpriteFlag(index, set);
            if (this.WorldUI != null){
                ((ObjectComponent)this.WorldUI).DynamicSpriteFlags = this.DynamicSpriteFlags;
                ((ObjectComponent)this.WorldUI).DynamicSpriteFlags2 = this.DynamicSpriteFlags2;
            }
        }

        public override bool SetValue(VMStackObjectVariable var, short value)
        {
            switch (var)
            {
                case VMStackObjectVariable.Flags:
                    var flags = (VMEntityFlags)value;
                    if (UseWorld)
                        ((ObjectComponent)WorldUI).HideForCutaway = (flags & VMEntityFlags.HideForCutaway) > 0;
                //            || ((VMEntityFlags2)GetValue(VMStackObjectVariable.FlagField2) & VMEntityFlags2.ArchitectualDoor) > 0;
                    break;
                case VMStackObjectVariable.FlagField2:
                    /*
                    var flags2 = (VMEntityFlags2)value;
                    if (UseWorld)
                        ((ObjectComponent)WorldUI).HideForCutaway = (flags2 & VMEntityFlags2.ArchitectualDoor) > 0
                            || ((VMEntityFlags)GetValue(VMStackObjectVariable.Flags) & VMEntityFlags.HideForCutaway) > 0; */
                    break;

            }
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


        public override void SetRoom(ushort room)
        {
            base.SetRoom(room);
            
            // NOTE: if something computationally intensive happens here, take into account
            // that the previous value of room may equal the new value if the client resyncs
            // so checking for a difference and only acting when there is is likely a bad idea.
            RefreshLight();
        }

        public void RefreshLight()
        {
            if (UseWorld)
            {
                var flags = (VMEntityFlags2)GetValue(VMStackObjectVariable.FlagField2);
                WorldUI.Room = ((flags & VMEntityFlags2.GeneratesLight) > 0 && 
                    GetValue(VMStackObjectVariable.LightingContribution)>0 && 
                    (flags & (VMEntityFlags2.ArchitectualWindow | VMEntityFlags2.ArchitectualDoor)) == 0) 
                    ? (ushort)65535 : (ushort)GetValue(VMStackObjectVariable.Room);
            }
        }

        public override void Init(FSO.SimAntics.VMContext context){
            if (UseWorld) WorldUI.ObjectID = ObjectID;
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
                if (UseWorld) WorldUI.Direction = value;
            }
        }

        public override Vector3 VisualPosition
        {
            get { return (UseWorld)?(WorldUI.Position + new Vector3(0.5f, 0.5f, 0f)):new Vector3(); }
            set { if (UseWorld) WorldUI.Position = value-new Vector3(0.5f, 0.5f, 0f); }
        }

        public override string ToString()
        {
            if (MultitileGroup.Name != "") return MultitileGroup.Name;
            var strings = Object.Resource.Get<CTSS>(Object.OBJ.CatalogStringsID);
            if (strings != null){
                return strings.GetString(0);
            }
            var label = Object.OBJ.ChunkLabel;
            if (label != null && label.Length > 0){
                return label;
            }
            return Object.OBJ.GUID.ToString("X");
        }

        // Begin Container SLOTs interface

        public override int TotalSlots()
        {
            if (Contained == null) return 0;
            return Contained.Length;
        }

        public override bool PlaceInSlot(VMEntity obj, int slot, bool cleanOld, VMContext context)
        {
            if (GetSlot(slot) == obj) return true; //already in slot
            if (GetSlot(slot) != null) return false;
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
                    if (cleanOld) obj.PositionChange(context, false);
                    return true;
                }
            }
            return false;
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

        public override Texture2D GetIcon(GraphicsDevice gd, int store)
        {
            var bmp = Object.Resource.Get<BMP>((ushort)(Object.OBJ.CatalogStringsID + store * 2000));
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
            context.UnregisterObjectPos(this);
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

            var room = context.GetObjectRoom(this);
            SetRoom(room);
            for (int i=0; i<Contained.Length; i++)
            {
                if (Contained[i] != null)
                {
                    context.UnregisterObjectPos(Contained[i]);
                    Contained[i].Position = Position;
                    Contained[i].PositionChange(context, noEntryPoint); //recursive
                }
            }

            context.RegisterObjectPos(this);

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

            if (EntryPoints[8].ActionFunction != 0) UpdateDynamicMultitile(context);

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
                WorldUI.ObjectID = ObjectID;
                if (Slots != null && Slots.Slots.ContainsKey(0)) ((ObjectComponent)WorldUI).ContainerSlots = Slots.Slots[0];
                SetValue(VMStackObjectVariable.Flags, GetValue(VMStackObjectVariable.Flags));
                RefreshGraphic();
            }
        }

        public VMHollowGameObjectMarshal HollowSave()
        {
            var cont = new short[Contained.Length];
            for (int i = 0; i < Contained.Length; i++)
            {
                cont[i] = (Contained[i] == null) ? (short)0 : Contained[i].ObjectID;
            }

            var gameObj = new VMHollowGameObjectMarshal
            {
                ObjectID = ObjectID,
                GUID = Object.OBJ.GUID,
                MasterGUID = (MasterDefinition == null) ? 0 : MasterDefinition.GUID,
                Position = Position,
                Direction = Direction,
                Graphic = GetValue(VMStackObjectVariable.Graphic),
                DynamicSpriteFlags = DynamicSpriteFlags,
                DynamicSpriteFlags2 = DynamicSpriteFlags2,

                Contained = cont,
                Container = (Container == null) ? (short)0 : Container.ObjectID,
                ContainerSlot = ContainerSlot,

                Flags = GetValue(VMStackObjectVariable.Flags),
                Flags2 = GetValue(VMStackObjectVariable.FlagField2),
                PlacementFlags = GetValue(VMStackObjectVariable.PlacementFlags),
                WallPlacementFlags = GetValue(VMStackObjectVariable.WallPlacementFlags),
                AllowedHeightFlags = GetValue(VMStackObjectVariable.AllowedHeightFlags)
            };
            return gameObj;
        }

        public void HollowLoad(VMHollowGameObjectMarshal input)
        {
            ObjectID = input.ObjectID;

            if (input.MasterGUID != 0)
            {
                var masterDef = FSO.Content.Content.Get().WorldObjects.Get(input.MasterGUID);
                MasterDefinition = masterDef.OBJ;
                UseTreeTableOf(masterDef);
            }

            else MasterDefinition = null;

            ContainerSlot = input.ContainerSlot;

            DynamicSpriteFlags = input.DynamicSpriteFlags;
            DynamicSpriteFlags2 = input.DynamicSpriteFlags2;
            SetValue(VMStackObjectVariable.Graphic, input.Graphic);
            SetValue(VMStackObjectVariable.Flags, input.Flags);
            SetValue(VMStackObjectVariable.FlagField2, input.Flags2);
            SetValue(VMStackObjectVariable.PlacementFlags, input.PlacementFlags);
            SetValue(VMStackObjectVariable.WallPlacementFlags, input.WallPlacementFlags);
            SetValue(VMStackObjectVariable.AllowedHeightFlags, input.AllowedHeightFlags);
            Position = input.Position;
            Direction = input.Direction;

            if (UseWorld)
            {
                ((ObjectComponent)this.WorldUI).DynamicSpriteFlags = this.DynamicSpriteFlags;
                ((ObjectComponent)this.WorldUI).DynamicSpriteFlags2 = this.DynamicSpriteFlags2;
                WorldUI.ObjectID = ObjectID;
                if (Slots != null && Slots.Slots.ContainsKey(0)) ((ObjectComponent)WorldUI).ContainerSlots = Slots.Slots[0];
                RefreshGraphic();
            }
        }

        public void LoadHollowCrossRef(VMHollowGameObjectMarshal input, VMContext context)
        {
            Contained = new VMEntity[input.Contained.Length];
            int i = 0;
            foreach (var item in input.Contained) Contained[i++] = context.VM.GetObjectById(item);

            Container = context.VM.GetObjectById(input.Container);
            if (UseWorld && Container != null)
            {
                WorldUI.Container = Container.WorldUI;
                WorldUI.ContainerSlot = ContainerSlot;
            }
        }
        #endregion
    }
}
