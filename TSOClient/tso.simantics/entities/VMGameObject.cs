using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Content;
using tso.world.components;
using tso.world.model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TSO.Files.formats.iff.chunks;
using TSO.Simantics.model;
using TSO.Common.utils;
using TSO.Content.model;

namespace TSO.Simantics
{
    public class VMGameObject : VMEntity
    {

        private VMEntity[] SlotContainees;

        /** Definition **/

        public VMGameObject(GameObject def, ObjectComponent worldUI) : base(def)
        {
            this.WorldUI = worldUI;

            /*var mainFunction = def.Master.MainFuncID;
            if (mainFunction > 0)
            {
                var bhav = def.Iff.BHAVs.First(x => x.ID == mainFunction);
                int y = 22;
            }*/
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
            var newGraphic = Object.OBJ.BaseGraphicID + ObjectData[(int)VMStackObjectVariable.Graphic];
            var dgrp = Object.Resource.Get<DGRP>((ushort)newGraphic);
            if (dgrp != null)
            {
                ((ObjectComponent)WorldUI).DGRP = dgrp;
                return true;
            }
            return false;
        }


        public override void Init(TSO.Simantics.VMContext context){
            ((ObjectComponent)WorldUI).ObjectID = ObjectID;
            if (Slots != null && Slots.Slots.ContainsKey(0))
            {
                SlotContainees = new VMEntity[Slots.Slots[0].Count];
                ((ObjectComponent)WorldUI).ContainerSlots = Slots.Slots[0];
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

        public override Direction Direction { 
            get { return ((ObjectComponent)WorldUI).Direction; }
            set { ((ObjectComponent)WorldUI).Direction = value; }
        }

        public override Vector3 VisualPosition
        {
            get { return WorldUI.Position + new Vector3(0.5f, 0.5f, 0f); }
            set { WorldUI.Position = value-new Vector3(0.5f, 0.5f, 0f); }
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
            if (SlotContainees == null) return 0;
            return SlotContainees.Length;
        }

        public override void PlaceInSlot(VMEntity obj, int slot)
        {
            if (SlotContainees != null)
            {
                if (slot > -1 && slot < SlotContainees.Length)
                {
                    SlotContainees[slot] = obj;
                    //if (obj is VMAvatar) obj.Direction = this.Direction;
                    obj.Container = this;
                    obj.ContainerSlot = (short)slot;
                    obj.WorldUI.Container = this.WorldUI;
                    obj.WorldUI.ContainerSlot = slot;
                    obj.Position = Position; //TODO: is physical position the same as the slot offset position?
                }
            }
        }

        public override VMEntity GetSlot(int slot)
        {
            if (SlotContainees != null)
            {
                if (slot > -1 && slot < SlotContainees.Length)
                {
                    return SlotContainees[slot];
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
            if (SlotContainees != null)
            {
                if (slot > -1 && slot < SlotContainees.Length)
                {
                    SlotContainees[slot].Container = null;
                    SlotContainees[slot].ContainerSlot = -1;
                    SlotContainees[slot].WorldUI.Container = null;
                    SlotContainees[slot].WorldUI.ContainerSlot = -1;
                    SlotContainees[slot] = null;
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

            if (EntryPoints[15].ActionFunction != 0)
            { //portal
                context.RemoveRoomPortal(this);
            }
            context.UnregisterObjectPos(this);
            base.PrePositionChange(context);
        }

        public override void PositionChange(VMContext context)
        {
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
                    Object.OBJ.WallStyle = TSO.Content.Content.Get().WorldWalls.AddDynamicWallStyle(style);
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

            if (EntryPoints[15].ActionFunction != 0)
            { //portal
                context.AddRoomPortal(this);
            }

            context.RegisterObjectPos(this);

            base.PositionChange(context);
        }

    }
}
