using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.world.components;
using tso.world.utils;
using Microsoft.Xna.Framework.Graphics;

namespace tso.world.model
{
    /// <summary>
    /// Holds all the objects that exist in the world for rendering
    /// and SimAntics.
    /// </summary>
    public class Blueprint
    {
        public List<BlueprintDamage> Damage = new List<BlueprintDamage>();
        private List<BlueprintOccupiedTile> OccupiedTiles = new List<BlueprintOccupiedTile>();
        private WorldRotation OccupiedTilesOrder = WorldRotation.TopLeft;
        private bool OccupiedTilesDirty = false;

        public int Width;
        public int Height;

        public List<WorldComponent> All = new List<WorldComponent>();

        /// <summary>
        /// Holds information about the ground such as colour
        /// </summary>
        public BlueprintGround[] Ground;

        /// <summary>
        /// Only read these arrays, do not modify them!
        /// </summary>
        public FloorComponent[] Floor;
        public BlueprintRoom[] Room;
        public BlueprintObjectList[] Objects;

        public TerrainComponent Terrain;

        /// <summary>
        /// Avatars
        /// </summary>
        public List<AvatarComponent> Avatars = new List<AvatarComponent>();

        private GraphicsDevice Device;

        public Blueprint(int width, int height){
            this.Width = width;
            this.Height = height;

            var numTiles = width * height;
            this.Ground = new BlueprintGround[numTiles];
            this.Floor = new FloorComponent[numTiles];
            this.Room = new BlueprintRoom[numTiles];
            this.Objects = new BlueprintObjectList[numTiles];
        }

        public void AddAvatar(AvatarComponent avatar){
            this.Avatars.Add(avatar);
        }

        public bool IsTileOccupied(short tileX, short tileY)
        {
            var offset = GetOffset(tileX, tileY);
            var hasFloor = Floor[offset] != null;
            var hasObject = Objects[offset] != null && Objects[offset].Objects.Count > 0;

            return hasFloor || hasObject;
        }

        public FloorComponent GetFloor(short tileX, short tileY)
        {
            var offset = GetOffset(tileX, tileY);
            return Floor[offset];
        }

        public void SetFloor(short tileX, short tileY, FloorComponent component){
            var offset = GetOffset(tileX, tileY);
            Floor[offset] = component;
            component.TileX = tileX;
            component.TileY = tileY;

            if (!All.Contains(component))
            {
                All.Add(component);
            }

            Damage.Add(new BlueprintDamage(BlueprintDamageType.FLOOR_CHANGED, tileX, tileY, 1));
            OccupiedTilesDirty = true;
        }

        public BlueprintObjectList GetObjects(short tileX, short tileY)
        {
            var offset = GetOffset(tileX, tileY);
            return Objects[offset];
        }

        public List<BlueprintOccupiedTile> GetOccupiedTiles(WorldRotation order){
            if (OccupiedTilesDirty){
                OccupiedTiles.Clear();

                foreach (var tile in IsometricTileIterator.Tiles(order, 0, 0, (short)Width, (short)Height))
                {
                    var offset = GetOffset(tile.TileX, tile.TileY);
                    var hasFloor = Floor[offset] != null;
                    var hasObject = Objects[offset] != null && Objects[offset].Objects.Count > 0;

                    if (hasFloor || hasObject)
                    {
                        var inst = new BlueprintOccupiedTile();
                        inst.TileX = tile.TileX;
                        inst.TileY = tile.TileY;

                        if (hasFloor){
                            inst.Type |= BlueprintOccupiedTileType.FLOOR;
                        }
                        if (hasObject){
                            inst.Type |= BlueprintOccupiedTileType.OBJECT;
                        }
                        OccupiedTiles.Add(inst);
                    }
                }
                OccupiedTilesOrder = order;
                OccupiedTilesDirty = false;
            }
            /** Has rotation changed? **/
            if (order != OccupiedTilesOrder){
                /** Re-sort **/
                OccupiedTiles.Sort(new IsometricTileSorter<BlueprintOccupiedTile>(order));
                OccupiedTilesOrder = order;
            }

            return OccupiedTiles;
        }

        public void ChangeObjectLocation(ObjectComponent component, short tileX, short tileY, sbyte level)
        {
            /** It has never been placed before if tileX == -2 **/
            if (component.TileX != -2){
                var currentOffset = GetOffset(tileX, tileY);
                var currentList = Objects[currentOffset];
                if (currentList != null){
                    currentList.RemoveObject(component);
                }
            }

            var newOffset = GetOffset(tileX, tileY);
            var newList = Objects[newOffset];
            if (newList == null){
                newList = Objects[newOffset] = new BlueprintObjectList();
            }
            newList.AddObject(component);
            component.blueprint = this;
            component.TileX = tileX;
            component.TileY = tileY;
            component.Level = level;
            component.Position = new Microsoft.Xna.Framework.Vector3(tileX, tileY, 0);

            if (!All.Contains(component))
            {
                All.Add(component);
            }
            Damage.Add(new BlueprintDamage(BlueprintDamageType.OBJECT_MOVE, tileX, tileY, level) { Component = component });
            OccupiedTilesDirty = true;
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
        FLOOR_CHANGED,
        SCROLL,
        ROTATE,
        ZOOM
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

    public struct BlueprintGround {
    }
    public struct BlueprintRoom {
        public ushort RoomID;
    }
}
