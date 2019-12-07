﻿/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.LotView;
using FSO.LotView.Model;
using FSO.LotView.Components;
using FSO.Content;
using Microsoft.Xna.Framework;
using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.Model;

namespace FSO.SimAntics.Utils
{
    /// <summary>
    /// Handles object creation and destruction
    /// </summary>
    public class VMWorldActivator
    {
        private VM VM;
        private LotView.World World;
        private Blueprint Blueprint;

        public Rectangle FloorClip;
        public Point Offset;
        public int TargetSize;

        public Point[] DirOffsets = new Point[]
        {
            new Point(7, 14),
            new Point(-2, 7),
            new Point(7, -1),
            new Point(14, 7)
        };

        public VMWorldActivator(VM vm, LotView.World world){
            this.VM = vm;
            this.World = world;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        public Blueprint LoadFromXML(XmlHouseData model){
            var size = TargetSize;
            if (size == 0) size = model.Size;
            model.Size = size;
            if (VM.UseWorld) this.Blueprint = new Blueprint(size, size);
            VM.Entities = new List<VMEntity>();
            VM.Scheduler = new Engine.VMScheduler(VM);
            VM.Context = new VMContext(VM.Context.World);
            VM.Context.VM = VM;
            VM.Context.Blueprint = Blueprint;
            VM.Context.Architecture = new VMArchitecture(size, size, Blueprint, VM.Context);

            var phone = model.Objects.FirstOrDefault(x => x.GUIDInt == 0x313D2F9A);
            if (phone != null && TargetSize != 0)
            {
                //determine lot alignment
                var dirDiff = new Point(phone.X, phone.Y) - new Point(model.Size / 2);
                // (atan result) 1 is default, 2 is 90 clockwise
                var dir = (int)(Math.Round(Math.Atan2(dirDiff.Y, dirDiff.X) / (Math.PI / 2)) + 3) % 4;

                Offset = DirOffsets[dir];
                VM.TSOState.Size = ((10) | (3 << 8)) | (dir << 16);
            }

            var arch = VM.Context.Architecture;

            foreach (var floor in model.World.Floors){
                if (FloorClip != Rectangle.Empty && !FloorClip.Contains(floor.X, floor.Y)) continue;
                arch.SetFloor((short)(floor.X + Offset.X), (short)(floor.Y + Offset.Y), (sbyte)(floor.Level+1), new FloorTile { Pattern = (ushort)floor.Value }, true);
            }

            foreach (var pool in model.World.Pools)
            {
                arch.SetFloor((short)(pool.X + Offset.X), (short)(pool.Y + Offset.Y), 1, new FloorTile { Pattern = 65535 }, true);
            }

            {
            foreach (var wall in model.World.Walls)
                arch.SetWall((short)(wall.X+Offset.X), (short)(wall.Y+Offset.Y), (sbyte)(wall.Level+1), new WallTile() //todo: these should read out in their intended formats - a cast shouldn't be necessary
                {
                    Segments = wall.Segments,
                    TopLeftPattern = (ushort)wall.TopLeftPattern,
                    TopRightPattern = (ushort)wall.TopRightPattern,
                    BottomLeftPattern = (ushort)wall.BottomLeftPattern,
                    BottomRightPattern = (ushort)wall.BottomRightPattern,
                    TopLeftStyle = (ushort)wall.LeftStyle,
                    TopRightStyle = (ushort)wall.RightStyle
                });
            }
            arch.RegenRoomMap();
            VM.Context.RegeneratePortalInfo();

            foreach (var obj in model.Objects)
            {
                CreateObject(obj);
            }

            if (VM.UseWorld)
            {
                foreach (var obj in model.Sounds)
                {
                    VM.Context.Ambience.SetAmbience(VM.Context.Ambience.GetAmbienceFromGUID(obj.ID), (obj.On == 1));
                    
                    
                }
                Blueprint.Terrain = CreateTerrain(model);
            }

            arch.SignalTerrainRedraw();
            VM.Context.World?.InitBlueprint(Blueprint);
            if (VM.UseWorld) World.State.WorldSize = size;
            arch.Tick();
            return this.Blueprint;
        }

        private TerrainComponent CreateTerrain(XmlHouseData model)
        {
            var terrain = new TerrainComponent(new Rectangle(0, 0, model.Size, model.Size), Blueprint);
            this.InitWorldComponent(terrain);
            return terrain;
        }

        public VMAvatar CreateAvatar()
        {
            return (VMAvatar)VM.Context.CreateObjectInstance(VMAvatar.TEMPLATE_PERSON, LotTilePos.OUT_OF_WORLD, Direction.NORTH).Objects[0];
        }

        public VMEntity CreateObject(XmlHouseDataObject obj){
            LotTilePos pos = LotTilePos.OUT_OF_WORLD;
            var nobj = VM.Context.CreateObjectInstance(obj.GUIDInt, pos, obj.Direction).Objects[0];
            if (obj.Level != 0)
                nobj.SetPosition(LotTilePos.FromBigTile((short)(obj.X + Offset.X), (short)(obj.Y + Offset.Y), (sbyte)obj.Level), obj.Direction, VM.Context, VMPlaceRequestFlags.AcceptSlots);

            if (obj.Group != 0)
            {
                foreach (var sub in nobj.MultitileGroup.Objects)
                {
                    sub.SetValue(VMStackObjectVariable.GroupID, (short)obj.Group);
                }
            }

            for (int i = 0; i < nobj.MultitileGroup.Objects.Count; i++) nobj.MultitileGroup.Objects[i].ExecuteEntryPoint(11, VM.Context, true);

            return nobj;
        }


        private void InitWorldComponent(WorldComponent component)
        {
            component.Initialize(this.World.State.Device, this.World.State);
        }

    }
}
