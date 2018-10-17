﻿/*
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
using FSO.SimAntics.NetPlay.Model.Commands;
using FSO.SimAntics.Model.Platform;
using FSO.SimAntics.Model.TS1Platform;

namespace FSO.SimAntics
{
    public class VMGameObject : VMEntity
    {
        public VMGameObjectDisableFlags Disabled;
        public VMIObjectState ObjectState;

        public VMGameObject(GameObject def, ObjectComponent worldUI) : base(def)
        {
            this.WorldUI = worldUI;
            var state = VM.GlobTS1?(VMAbstractEntityState)new VMTS1ObjectState():new VMTSOObjectState();
            PlatformState = state;
            ObjectState = (VMIObjectState)state;
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
                if (Disabled >= VMGameObjectDisableFlags.LotCategoryWrong) WorldUI.Room = 65533; //grayscale
                else
                {
                    WorldUI.Room = ((flags & VMEntityFlags2.GeneratesLight) > 0 &&
                        GetValue(VMStackObjectVariable.LightingContribution) > 0 &&
                        (flags & (VMEntityFlags2.ArchitectualWindow | VMEntityFlags2.ArchitectualDoor)) == 0)
                        ? (ushort)65535 : (ushort)ObjectData[(int)VMStackObjectVariable.Room];
                }
            }
        }

        public void DisableIfTSOCategoryWrong(VMContext context)
        {
            if (context.VM.TS1) return;
            OBJD obj = Object.OBJ;
            if (MasterDefinition != null) obj = MasterDefinition;
            var category = context.VM.TSOState.PropertyCategory;
            var flag = (1 << category);
            if (category == 7) flag |= 2; //money objects are allowed on welcome lots too. (fso change, disabling this is todo)
            if (category != 255 && obj.LotCategories > 0 && (obj.LotCategories & flag) == 0)
                Disabled |= VMGameObjectDisableFlags.LotCategoryWrong;
            else
                Disabled &= ~VMGameObjectDisableFlags.LotCategoryWrong; 
        }

        public override void Init(FSO.SimAntics.VMContext context){
            if (UseWorld) WorldUI.ObjectID = ObjectID;
            if (Slots != null && Slots.Slots.ContainsKey(0))
            {
                Contained = new VMEntity[Slots.Slots[0].Count];
                if (UseWorld) ((ObjectComponent)WorldUI).ContainerSlots = Slots.Slots[0];
            }

            base.Init(context);
            DisableIfTSOCategoryWrong(context);
        }

        public override void Tick()
        {
            if ((Disabled & VMGameObjectDisableFlags.PendingRoommateDeletion) > 0)
            {
                //can we be deleted and moved back to inventory? maybe some stuff on us needs to be first.
                var context = Thread.Context;
                var current = DeepestObjInSlot(this, 0);
                if (current is VMGameObject && !current.IsInUse(context, true))
                {
                    if (current.PersistID > 0)
                    {
                        if (context.VM.IsServer && (Disabled & VMGameObjectDisableFlags.TransactionIncomplete) == 0)
                        {
                            context.VM.ForwardCommand(new VMNetSendToInventoryCmd()
                            {
                                InternalDispatch = true,
                                ObjectPID = current.PersistID,
                            });
                        }
                    }
                }
            }

            if ((Disabled & VMGameObjectDisableFlags.ObjectLimitExceeded) > 0) { 
                if ((Disabled & VMGameObjectDisableFlags.ObjectLimitThreadDisable) > 0) return;
                else if (!IsInUse(Thread.Context, true) && !PartOfPortal())
                {
                    Disabled |= VMGameObjectDisableFlags.ObjectLimitThreadDisable;
                }
            }
            base.Tick();
        }

        public bool PartOfPortal()
        {
            foreach (var obj in MultitileGroup.Objects)
            {
                if (obj.EntryPoints[15].ActionFunction != 0) return true;
            }
            return false;
        }

        private VMEntity DeepestObjInSlot(VMEntity pt, int depth)
        {
            //todo: make sure nobody can create cyclic slots, and limit slot depth
            if (depth > 50) throw new Exception("slot depth too high!");
            var slots = pt.TotalSlots();
            for (int i=0; i<slots; i++)
            {
                var ent = pt.GetSlot(i);
                if (ent != null) return DeepestObjInSlot(ent, depth++);
            }
            return pt;
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
                Direction = (Direction)(1 << (int)(Math.Round(DirectionUtils.PosMod(value / ((float)Math.PI * 2), 1f) * 8) % 8));
            }
        }

        private Direction _Direction;
        public override Direction Direction { 
            get { return _Direction; }
            set {
                var notches = GetValue(VMStackObjectVariable.RotationNotches);
                if (notches > 1)
                {
                    var index = Array.IndexOf(DirectionNotches, value);
                    if (index != -1)
                        _Direction = DirectionNotches[index - (index % notches)];
                }
                else _Direction = value;

                if (UseWorld) WorldUI.Direction = _Direction;
            }
        }

        public override Vector3 VisualPosition
        {
            get { return (UseWorld)?(WorldUI.Position + new Vector3(0.5f, 0.5f, 0f)):new Vector3(); }
            set { if (UseWorld) WorldUI.Position = value-new Vector3(0.5f, 0.5f, 0f); }
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
            if (GetSlot(slot) != null || WillLoopSlot(obj) || obj.Dead) return false; //would recursively loop slot..
            if (cleanOld) obj.PrePositionChange(context);

            if (Contained != null)
            {
                if (slot > -1 && slot < Contained.Length)
                {
                    if (obj.GhostImage == GhostImage)
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
            var bmp = Object.Resource.Get<BMP>((ushort)((MasterDefinition ?? Object.OBJ).CatalogStringsID + store * 2000));
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
            var exclusive = GetValue(VMStackObjectVariable.ExclusivePlacementFlags);
            if (((VMEntityFlags2)ObjectData[(int)VMStackObjectVariable.FlagField2] & (VMEntityFlags2.ArchitectualWindow | VMEntityFlags2.ArchitectualDoor)) > 0)
            { //if wall or door, attempt to place style on wall
                var placeFlags = (WallPlacementFlags)ObjectData[(int)VMStackObjectVariable.WallPlacementFlags];
                var dir = DirectionToWallOff(Direction);
                if ((placeFlags & WallPlacementFlags.WallRequiredInFront) > 0) SetWallStyle((dir) % 4, arch, 0);
                if ((placeFlags & WallPlacementFlags.WallRequiredOnRight) > 0) SetWallStyle((dir + 1) % 4, arch, 0);
                if ((placeFlags & WallPlacementFlags.WallRequiredBehind) > 0) SetWallStyle((dir + 2) % 4, arch, 0);
                if ((placeFlags & WallPlacementFlags.WallRequiredOnLeft) > 0) SetWallStyle((dir + 3) % 4, arch, 0);
            }
            SetWallUse(arch, false, ((exclusive & 2) > 0));
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
            for (int i = 0; i < Contained.Length; i++)
            {
                if (Contained[i] != null)
                {
                    context.UnregisterObjectPos(Contained[i]);
                    Contained[i].Position = Position;
                    Contained[i].PositionChange(context, noEntryPoint); //recursive
                }
            }
            if (GhostImage) return;

            var room = context.GetObjectRoom(this);
            SetRoom(room);

            context.RegisterObjectPos(this);

            if (Container != null) return;
            if (Position == LotTilePos.OUT_OF_WORLD) return;

            var arch = context.Architecture;
            if (((VMEntityFlags2)ObjectData[(int)VMStackObjectVariable.FlagField2] & (VMEntityFlags2.ArchitectualWindow | VMEntityFlags2.ArchitectualDoor)) > 0)
            { //if wall or door, attempt to place style on wall

                if (Object.OBJ.WallStyle > 21 && Object.OBJ.WallStyle < 256)
                { //first thing's first, is the style between 22-255 inclusive? If it is, then the style is stored in the object. Need to load its sprites and change the id for the objd.
                    var id = Object.OBJ.WallStyleSpriteID;
                    var sprs = new SPR[6];
                    for (int i=0; i<6; i++)
                    {
                        sprs[i] = Object.Resource.Get<SPR>((ushort)(id + i));
                        if (sprs[i] != null) sprs[i].WallStyle = true;
                    }
                    var style = new WallStyle()
                    {
                        WallsUpFar = sprs[0],
                        WallsUpMedium = sprs[1],
                        WallsUpNear = sprs[2],
                        WallsDownFar = sprs[3],
                        WallsDownMedium = sprs[4],
                        WallsDownNear = sprs[5]
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
            var exclusive = GetValue(VMStackObjectVariable.ExclusivePlacementFlags);
            SetWallUse(arch, true, ((exclusive & 2) > 0));
            if (GetValue(VMStackObjectVariable.Category) == 8) context.Architecture.SetObjectSupported(Position.TileX, Position.TileY, Position.Level, true);

            if (EntryPoints[8].ActionFunction != 0) UpdateDynamicMultitile(context);

            base.PositionChange(context, noEntryPoint);
        }

        #region FSO Particles
        public void EnableParticle(ushort id)
        {
            if (UseWorld)
            {
                var parts = ((ObjectComponent)WorldUI).Particles;
                var relevant = parts.FirstOrDefault(x => x.Resource?.ChunkID == id && float.IsPositiveInfinity(x.StopTime));
                if (relevant == null)
                {
                    var part = new ParticleComponent(WorldUI.blueprint, WorldUI.blueprint.ObjectParticles);
                    //for now there is only one particle resource. In future get from iff.
                    part.Resource = PART.BROKEN;
                    part.Mode = ParticleType.GENERIC_BOX;
                    GameThread.InUpdate(() =>
                    {
                        part.Tex = Content.Content.Get().RCMeshes.GetTex("FSO_smoke.png");
                        WorldUI.blueprint.ObjectParticles.Add(part);
                    });
                    ((ObjectComponent)WorldUI).Particles.Add(part);
                    part.Owner = WorldUI;
                    WorldUI.blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.OBJECT_GRAPHIC_CHANGE, WorldUI.TileX, WorldUI.TileY, WorldUI.Level, WorldUI));
                }
            }
        }

        public void DisableParticle(ushort id)
        {
            if (UseWorld)
            {
                var parts = ((ObjectComponent)WorldUI).Particles;
                var relevant = parts.FirstOrDefault(x => x.Resource?.ChunkID == id);
                if (relevant != null)
                {
                    relevant.Stop();
                }
            }
        }

        #endregion


        #region VM Marshalling Functions
        public VMGameObjectMarshal Save()
        {
            var gameObj = new VMGameObjectMarshal { Direction = Direction, Disabled = Disabled };
            SaveEnt(gameObj);
            return gameObj;
        }

        public void Load(VMGameObjectMarshal input)
        {
            base.Load(input);
            ObjectState = (VMIObjectState)PlatformState;
            Position = Position;
            Direction = input.Direction;
            Disabled = input.Disabled;
            if (UseWorld)
            {
                ((ObjectComponent)this.WorldUI).DynamicSpriteFlags = this.DynamicSpriteFlags;
                WorldUI.ObjectID = ObjectID;
                if (Slots != null && Slots.Slots.ContainsKey(0)) ((ObjectComponent)WorldUI).ContainerSlots = Slots.Slots[0];
                SetValue(VMStackObjectVariable.Flags, GetValue(VMStackObjectVariable.Flags));
                RefreshGraphic();
            }
        }

        public override void LoadCrossRef(VMEntityMarshal input, VMContext context)
        {
            base.LoadCrossRef(input, context);
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

    [Flags]
    public enum VMGameObjectDisableFlags
    {
        TransactionIncomplete = 1 << 0,
        ForSale = 1 << 1,
        //past this point disabled objects appear in grayscale.
        LotCategoryWrong = 1 << 2,
        ObjectLimitExceeded = 1 << 3, //when too many objects are on a lot and the object lot is lowered, the last few objects are disabled.
        PendingRoommateDeletion = 1 << 4,
        ObjectLimitThreadDisable = 1 << 5 //activated when object limit exceeded and object is no longer in use.
    }
}
