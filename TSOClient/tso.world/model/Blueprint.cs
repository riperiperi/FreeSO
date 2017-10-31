/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.LotView.Components;
using FSO.LotView.Utils;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using FSO.Vitaboy;
using FSO.LotView.LMap;
using FSO.LotView.RC;
using FSO.Common;

namespace FSO.LotView.Model
{
    /// <summary>
    /// Holds all the objects that exist in the world for rendering
    /// and SimAntics.
    /// </summary>
    public class Blueprint
    {
        public List<BlueprintDamage> Damage = new List<BlueprintDamage>();

        public int Width;
        public int Height;
        public sbyte Stories = 5;

        /// <summary>
        /// Only read these arrays, do not modify them!
        /// </summary>
        public WallTile[][] Walls;
        public List<int>[] WallsAt;
        public WallComponent WallComp;

        public FloorTile[][] Floors;
        public FloorComponent FloorComp;
        public _3DFloorGeometry FloorGeom;
        public WallComponentRC WCRC;

        public RoofComponent RoofComp;

        public bool[][] Supported; //directly the VM's copy at all times. DO NOT MODIFY.

        public List<ObjectComponent> Objects = new List<ObjectComponent>();
        public List<AvatarComponent> Avatars = new List<AvatarComponent>();
        public List<SubWorldComponent> SubWorlds = new List<SubWorldComponent>();
        public TerrainComponent Terrain;

        /// <summary>
        /// Walls Cutaway sections. Remember to manage these correctly - i.e remove when you're finished with them!
        /// </summary>
        /// 
        public bool[] Cutaway;

        private Color _OutsideColor = Color.White;
        public Color MinOut;
        public Color OutsideColor
        {
            get
            {
                return _OutsideColor;
            }
            set
            {
                _OutsideColor = value;
                MinOut = value * (150 / 400f);
                //MinOut = value * (float)(150 / Math.Sqrt(value.R * value.R + value.G * value.G + value.B * value.B));
            }
        }
        public double OutsideTime;
        public RoomLighting[] Light = new RoomLighting[0];
        public uint[][] RoomMap;
        public List<Room> Rooms = new List<Room>();

        public Color[] RoomColors;
        public Rectangle BuildableArea;
        public Rectangle TargetBuildableArea;
        public LightData OutdoorsLight;

        public short[] Altitude;
        public short[] AltitudeCenters;
        public float TerrainFactor = 3 / 160f;
        public int BaseAlt;

        public Blueprint(int width, int height){
            this.Width = width;
            this.Height = height;

            var numTiles = width * height;
            this.WallComp = new WallComponent();
            WallComp.blueprint = this;
            this.RoofComp = new RoofComponent(this);
            this.FloorGeom = new _3DFloorGeometry(this);
            if (WorldConfig.Current.Shadow3D || FSOEnvironment.Enable3D)
            {
                this.WCRC = new WallComponentRC();
                WCRC.blueprint = this;
            }
        
            RoomColors = new Color[65536];
            this.WallsAt = new List<int>[Stories];
            this.Walls = new WallTile[Stories][];
            this.RoomMap = new uint[Stories][];

            this.Floors = new FloorTile[Stories][];

            for (int i=0; i<Stories; i++)
            {
                this.WallsAt[i] = new List<int>();
                this.Walls[i] = new WallTile[numTiles];

                this.Floors[i] = new FloorTile[numTiles];
            }
            this.Cutaway = new bool[numTiles];
        }

        public float GetAltitude(int x, int y)
        {
            if (x <= 0 || y <= 0) return 0f;
            return (AltitudeCenters[((y % Height) * Width + (x % Width))] - BaseAlt) * TerrainFactor;
        }

        public float InterpAltitude(Vector3 Position)
        {
            if (Altitude == null) return 0f;
            var baseX = (int)Math.Max(1, Math.Min(Width-1, Position.X));
            var baseY = (int)Math.Max(1, Math.Min(Height-1, Position.Y));
            if (baseX < 0 || baseY < 0) return 0;
            var nextX = (int)Math.Max(1, Math.Min(Width - 1, Math.Ceiling(Position.X)));
            var nextY = (int)Math.Max(1, Math.Min(Height - 1, Math.Ceiling(Position.Y)));
            var xLerp = Position.X % 1f;
            var yLerp = Position.Y % 1f;
            float h00 = Altitude[((baseY % Height) * Width + (baseX % Width))] * TerrainFactor;
            float h01 = Altitude[((nextY % Height) * Width + (baseX % Width))] * TerrainFactor;
            float h10 = Altitude[((baseY % Height) * Width + (nextX % Width))] * TerrainFactor;
            float h11 = Altitude[((nextY % Height) * Width + (nextX % Width))] * TerrainFactor;

            float xl1 = xLerp * h10 + (1 - xLerp) * h00;
            float xl2 = xLerp * h11 + (1 - xLerp) * h01;

            return yLerp * xl2 + (1 - yLerp) * xl1 - BaseAlt * TerrainFactor;
        }

        public void SetLightColor(Effect effect, Color outside, Color minOut)
        {
            //return;
            effect.Parameters["OutsideLight"]?.SetValue(outside.ToVector4());
            effect.Parameters["OutsideDark"]?.SetValue(minOut.ToVector4());
            effect.Parameters["MaxLight"]?.SetValue(Color.White.ToVector4());
            var avg = (minOut.R + minOut.G + minOut.B) / (255 * 3f);
            effect.Parameters["MinAvg"]?.SetValue(new Vector2(avg, 1/(1-avg)));
        }

        public void GenerateRoomLights()
        {
            var minOut = MinOut;
            minOut.A = 255;

            SetLightColor(WorldContent._2DWorldBatchEffect, OutsideColor, minOut);
            SetLightColor(WorldContent.GrassEffect, OutsideColor, minOut);
            SetLightColor(Avatar.Effect, OutsideColor, minOut);
            SetLightColor(WorldContent.RCObject, OutsideColor, minOut);

            for (int i=0; i<Light.Length; i++)
            {
                var outside = OutsideColor * (Light[i].OutsideLight / 100f);
                var ambient = Color.White * (Light[i].AmbientLight / 100f);

                outside.R = Math.Max(minOut.R, outside.R);
                outside.G = Math.Max(minOut.G, outside.G);
                outside.B = Math.Max(minOut.B, outside.B);

                RoomColors[i] = new Color(
                    Math.Min(255, outside.R + ambient.R),
                    Math.Min(255, outside.G + ambient.G),
                    Math.Min(255, outside.B + ambient.B),
                    255);
            }
            RoomColors[65535] = Color.White;
        }

        public void AddAvatar(AvatarComponent avatar){
            this.Avatars.Add(avatar);
        }

        public void RemoveAvatar(AvatarComponent avatar)
        {
            this.Avatars.Remove(avatar);
        }

        public void SignalWallChange()
        {
            Damage.Add(new BlueprintDamage(BlueprintDamageType.WALL_CHANGED, 0, 0, 1)); 
            //todo: should this even have a position? we're rerendering the whole thing atm
            //should eventually consider level
        }

        public void SignalRoomChange()
        {
            Damage.Add(new BlueprintDamage(BlueprintDamageType.ROOM_CHANGED, 0, 0, 1));
            //todo: should this even have a position? we're rerendering the whole thing atm
            //should eventually consider level
        }

        public void SignalFloorChange()
        {
            Damage.Add(new BlueprintDamage(BlueprintDamageType.FLOOR_CHANGED, 0, 0, 1));
        }

        public WallTile GetWall(short tileX, short tileY, sbyte level)
        {
            return Walls[level-1][GetOffset(tileX, tileY)];
        }

        public FloorTile GetFloor(short tileX, short tileY, sbyte level)
        {
            var offset = GetOffset(tileX, tileY);
            return Floors[level-1][offset];
        }

        public void ChangeObjectLocation(ObjectComponent component, LotTilePos pos)
        {
            short tileX = (pos.x < 0) ? (short)0 : pos.TileX;
            short tileY = (pos.y < 0) ? (short)0 : pos.TileY;
            sbyte level = pos.Level;

            Damage.Add(new BlueprintDamage(BlueprintDamageType.OBJECT_MOVE, tileX, tileY, level) { Component = component });

            component.blueprint = this;
            component.TileX = tileX;
            component.TileY = tileY;
            component.Level = level;
        }

        public void AddObject(ObjectComponent component)
        {
            Objects.Add(component);
        }

        public void RemoveObject(ObjectComponent component)
        {
            Damage.Add(new BlueprintDamage(BlueprintDamageType.OBJECT_MOVE, component.TileX, component.TileY, component.Level) { Component = component });
            Objects.Remove(component);
        }

        private ushort GetOffset(int tileX, int tileY){
            return (ushort)((tileY * Width) + tileX);
        }
    }

    [Flags]
    public enum BlueprintOccupiedTileType {
        OBJECT = 0x1,
        FLOOR = 0x2
    }

    public class BlueprintOccupiedTile : IIsometricTile {
        public short TileX { get; set; }
        public short TileY { get; set; }
        public BlueprintOccupiedTileType Type;
    }

    public class BlueprintDamage {
        public BlueprintDamageType Type;
        public short TileX;
        public short TileY;
        public sbyte Level;
        public WorldComponent Component;

        public BlueprintDamage(BlueprintDamageType type)
        {
            this.Type = type;
        }

        public BlueprintDamage(BlueprintDamageType type, short tileX, short tileY, sbyte level, WorldComponent component)
        {
            this.Type = type;
            this.TileX = tileX;
            this.TileY = tileY;
            this.Level = level;
            this.Component = component;
        }

        public BlueprintDamage(BlueprintDamageType type, short tileX, short tileY, sbyte level){
            this.Type = type;
            this.TileX = tileX;
            this.TileY = tileY;
            this.Level = level;
        }
    }

    public enum BlueprintDamageType {
        OBJECT_MOVE,
        OBJECT_GRAPHIC_CHANGE,
        OBJECT_RETURN_TO_STATIC,
        FLOOR_CHANGED,
        WALL_CHANGED,
        SCROLL,
        ROTATE,
        ZOOM,
        PRECISE_ZOOM,
        WALL_CUT_CHANGED,
        LEVEL_CHANGED,
        LIGHTING_CHANGED,
        OUTDOORS_LIGHTING_CHANGED,
        ROOM_CHANGED,
        ROOF_STYLE_CHANGED,
        ROOM_MAP_CHANGED
    }

    public class BlueprintObjectList {
        public List<ObjectComponent> Objects = new List<ObjectComponent>();

        public void RemoveObject(ObjectComponent comp){
            Objects.Remove(comp);
        }

        public void AddObject(ObjectComponent comp){
            Objects.Add(comp);
        }
    }
}
