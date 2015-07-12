/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using tso.world.model;
using Microsoft.Xna.Framework;
using TSO.Common.utils;
using TSO.Common.rendering.framework;
using TSOClient.Code.Utils;
using tso.world.components;
using System.IO;

namespace tso.world
{
    /// <summary>
    /// Handles rendering the 2D world
    /// </summary>
    public class World2D
    {
        public static SurfaceFormat[] BUFFER_SURFACE_FORMATS = new SurfaceFormat[] {
            SurfaceFormat.Color,
            SurfaceFormat.Color,
            /** Depth buffer must be single surface format for precision reasons **/
            SurfaceFormat.Single,
            SurfaceFormat.Color,
            /** Object ID buffer **/
            SurfaceFormat.Single,
            /** Archetecture buffers **/
            SurfaceFormat.Color,
            SurfaceFormat.Single,
            /** Thumbnail buffer **/
            SurfaceFormat.Color
        };

        public static readonly int NUM_2D_BUFFERS = 8;
        public static readonly int BUFFER_STATIC_FLOOR = 0;
        public static readonly int BUFFER_STATIC_OBJECTS_PIXEL = 1;
        public static readonly int BUFFER_STATIC_OBJECTS_DEPTH = 2;
        public static readonly int BUFFER_STATIC_TERRAIN = 3;
        public static readonly int BUFFER_OBJID = 4;
        public static readonly int BUFFER_ARCHETECTURE_PIXEL = 5;
        public static readonly int BUFFER_ARCHETECTURE_DEPTH = 6;
        public static readonly int BUFFER_THUMB = 7; //used for drawing thumbnails


        private Blueprint Blueprint;
        private Dictionary<WorldComponent, WorldObjectRenderInfo> RenderInfo = new Dictionary<WorldComponent, WorldObjectRenderInfo>();

        private Texture2D StaticTerrain;
        private Texture2D StaticFloor;

        private Texture2D StaticObjects;
        private Texture2D StaticObjectsDepth;

        private Texture2D StaticArch;
        private Texture2D StaticArchDepth;

        public bool DrawCenterPoint = true;
        private Texture2D CenterPixel;


        public void Initialize(_3DLayer layer)
        {
            CenterPixel = TextureUtils.TextureFromColor(layer.Device, Color.Purple, 4, 4);
        }

        public void Init(Blueprint blueprint)
        {
            this.Blueprint = blueprint;
        }

        private WorldObjectRenderInfo GetRenderInfo(WorldComponent component)
        {
            return ((ObjectComponent)component).renderInfo;
        }

        /// <summary>
        /// Gets an object's ID given an object's screen position.
        /// </summary>
        /// <param name="x">The object's X position.</param>
        /// <param name="y">The object's Y position.</param>
        /// <param name="gd">GraphicsDevice instance.</param>
        /// <param name="state">WorldState instance.</param>
        /// <returns>Object's ID if the object was found at the given position.</returns>
        public short GetObjectIDAtScreenPos(int x, int y, GraphicsDevice gd, WorldState state)
        {
            /** Draw all objects to a texture as their IDs **/
            var occupiedTiles = Blueprint.GetOccupiedTiles(state.Rotation);
            var pxOffset = state.WorldSpace.GetScreenOffset();
            pxOffset.X -= x;
            pxOffset.Y -= y;
            var _2d = state._2D;
            Promise<Texture2D> bufferTexture = null;
            state._2D.OBJIDMode = true;
            using (var buffer = state._2D.WithBuffer(BUFFER_OBJID, ref bufferTexture))
            {
                
                while (buffer.NextPass())
                {
                    foreach (var tile in occupiedTiles)
                    {

                        /** Objects **/
                        if ((tile.Type & BlueprintOccupiedTileType.OBJECT) == BlueprintOccupiedTileType.OBJECT)
                        {
                            var objects = Blueprint.GetObjects(tile.TileX, tile.TileY, 1); //TODO: Level
                            foreach (var obj in objects.Objects)
                            {
                                var tilePosition = obj.Position;
                                _2d.OffsetPixel(state.WorldSpace.GetScreenFromTile(tilePosition) + pxOffset);
                                _2d.OffsetTile(tilePosition);
                                _2d.SetObjID(obj.ObjectID);
                                obj.Draw(gd, state);
                            }
                        }
                    }
                }
                
            }
            state._2D.OBJIDMode = false;

            var tex = bufferTexture.Get();
            Single[] data = new float[1];
            tex.GetData<Single>(data);
            return (short)Math.Round(data[0]*65535f);
        }

        /// <summary>
        /// Gets an object group's thumbnail provided an array of objects.
        /// </summary>
        /// <param name="objects">The object components to draw.</param>
        /// <param name="gd">GraphicsDevice instance.</param>
        /// <param name="state">WorldState instance.</param>
        /// <returns>Object's ID if the object was found at the given position.</returns>
        public Texture2D GetObjectThumb(ObjectComponent[] objects, Vector3[] positions, GraphicsDevice gd, WorldState state)
        {
            var oldZoom = state.Zoom;
            var oldRotation = state.Rotation;
            /** Center average position **/
            Vector3 average = new Vector3();
            for (int i = 0; i < positions.Length; i++)
            {
                average += positions[i];
            }
            average /= positions.Length;

            state.SilentZoom = WorldZoom.Near;
            state.SilentRotation = WorldRotation.BottomRight;
            state.WorldSpace.Invalidate();
            state.InvalidateCamera();
            var pxOffset = new Vector2(442, 275) - state.WorldSpace.GetScreenFromTile(average);

            var _2d = state._2D;
            Promise<Texture2D> bufferTexture = null;
            state._2D.OBJIDMode = false;
            Rectangle bounds = new Rectangle();
            using (var buffer = state._2D.WithBuffer(BUFFER_THUMB, ref bufferTexture))
            {
                while (buffer.NextPass())
                {
                    for (int i=0; i<objects.Length; i++)
                    {
                        var obj = objects[i];
                        var tilePosition = positions[i];

                        //we need to trick the object into believing it is in a set world state.
                        var oldObjRot = obj.Direction;

                        obj.Direction = Direction.NORTH;
                        state.SilentZoom = WorldZoom.Near;
                        state.SilentRotation = WorldRotation.BottomRight;
                        obj.OnRotationChanged(state);
                        obj.OnZoomChanged(state);

                        _2d.OffsetPixel(state.WorldSpace.GetScreenFromTile(tilePosition) + pxOffset);
                        _2d.OffsetTile(tilePosition);
                        _2d.SetObjID(obj.ObjectID);
                        obj.Draw(gd, state);

                        //return everything to normal
                        obj.Direction = oldObjRot;
                        state.SilentZoom = oldZoom;
                        state.SilentRotation = oldRotation;
                        obj.OnRotationChanged(state);
                        obj.OnZoomChanged(state);
                    }
                    bounds = _2d.GetSpriteListBounds();
                }
            }
            bounds.X = Math.Max(0, Math.Min(1023, bounds.X));
            bounds.Y = Math.Max(0, Math.Min(1023, bounds.Y));
            if (bounds.Width + bounds.X > 1024) bounds.Width = 1024 - bounds.X;
            if (bounds.Height + bounds.Y > 1024) bounds.Height = 1024 - bounds.Y;

            //return things to normal
            state.WorldSpace.Invalidate();
            state.InvalidateCamera();

            var tex = bufferTexture.Get();
            return TextureUtils.Clip(gd, tex, bounds);
        }

        /// <summary>
        /// Prep work before screen is painted
        /// </summary>
        /// <param name="gd"></param>
        /// <param name="state"></param>
        public void PreDraw(GraphicsDevice gd, WorldState state)
        {
            var damage = Blueprint.Damage;
            var _2d = state._2D;

            /**
             * Tasks:
             *  If scroll, zoom or rotation has changed, redraw all static layers
             *  If architecture has changed, redraw appropriate static layer
             *  If there is a new object in the static layer, redraw the static layer
             *  If an objects in the static layer has changed, redraw the static layer and move the object to the dynamic layer
             *  If wall visibility has changed, redraw wall layer (should think about how this works with breakthrough wall mode
             */

            var redrawTerrain = StaticTerrain == null;
            var redrawStaticObjects = false;
            var redrawFloors = false;
            var redrawWalls = false;

            WorldObjectRenderInfo info = null;

            foreach (var item in damage){
                switch (item.Type){
                    case BlueprintDamageType.ROTATE:
                    case BlueprintDamageType.ZOOM:
                    case BlueprintDamageType.SCROLL:
                        redrawFloors = true;
                        redrawWalls = true;
                        redrawStaticObjects = true;
                        redrawTerrain = true;
                        break;
                    case BlueprintDamageType.OBJECT_MOVE:
                        /** Redraw if its in static layer **/
                        info = GetRenderInfo(item.Component);
                        if (info.Layer == WorldObjectRenderLayer.STATIC){
                            redrawStaticObjects = true;
                        }
                        break;
                    case BlueprintDamageType.OBJECT_GRAPHIC_CHANGE:
                        /** Redraw if its in static layer **/
                        info = GetRenderInfo(item.Component);
                        if (info.Layer == WorldObjectRenderLayer.STATIC){
                            redrawStaticObjects = true;
                            info.Layer = WorldObjectRenderLayer.DYNAMIC;
                        }
                        break;
                    case BlueprintDamageType.OBJECT_RETURN_TO_STATIC:
                        info = GetRenderInfo(item.Component);
                        if (info.Layer == WorldObjectRenderLayer.DYNAMIC)
                        {
                            redrawStaticObjects = true;
                            info.Layer = WorldObjectRenderLayer.STATIC;
                        }
                        break;

                    case BlueprintDamageType.WALL_CUT_CHANGED:
                        redrawWalls = true;
                        break;
                    case BlueprintDamageType.FLOOR_CHANGED:
                        redrawFloors = true;
                        break;
                    case BlueprintDamageType.WALL_CHANGED:
                        redrawWalls = true;
                        break;
                }
            }
            damage.Clear();

            var occupiedTiles = Blueprint.GetOccupiedTiles(state.Rotation);
            var pxOffset = state.WorldSpace.GetScreenOffset();

            if (redrawTerrain)
            {
                Promise<Texture2D> bufferTexture = null;
                using (var buffer = state._2D.WithBuffer(BUFFER_STATIC_TERRAIN, ref bufferTexture)){
                    while (buffer.NextPass()){
                        Blueprint.Terrain.Draw(gd, state);
                    }
                }
                StaticTerrain = bufferTexture.Get();
            }

            if (redrawFloors){
                Promise<Texture2D> bufferTexture = null;
                using (var buffer = state._2D.WithBuffer(BUFFER_STATIC_FLOOR, ref bufferTexture))
                {
                    while (buffer.NextPass())
                    {
                        foreach (var tile in occupiedTiles)
                        {
                            var tilePosition = new Vector3(tile.TileX, tile.TileY, 0.0f);
                            _2d.OffsetPixel(state.WorldSpace.GetScreenFromTile(tilePosition) + pxOffset);
                            _2d.OffsetTile(tilePosition);
                            _2d.SetObjID(0);

                            if ((tile.Type & BlueprintOccupiedTileType.FLOOR) == BlueprintOccupiedTileType.FLOOR)
                            {
                                var floor = Blueprint.GetFloor(tile.TileX, tile.TileY, 1); //TODO: levels
                                floor.Draw(gd, state);
                            }
                        }
                    }
                }
                StaticFloor = bufferTexture.Get();
                //StaticFloor.Save("C:\\floor.png", ImageFileFormat.Png);
            }

            if (redrawWalls)
            {
                /** Draw archetecture to a texture **/
                Promise<Texture2D> bufferTexture = null;
                Promise<Texture2D> depthTexture = null;
                using (var buffer = state._2D.WithBuffer(BUFFER_ARCHETECTURE_PIXEL, ref bufferTexture, BUFFER_ARCHETECTURE_DEPTH, ref depthTexture))
                {
                    while (buffer.NextPass())
                    {
                        Blueprint.WallComp.Draw(gd, state);
                    }
                }
                StaticArch = bufferTexture.Get();
                StaticArchDepth = depthTexture.Get();
            }

            if (redrawStaticObjects){
                /** Draw static objects to a texture **/
                Promise<Texture2D> bufferTexture = null;
                Promise<Texture2D> depthTexture = null;
                using (var buffer = state._2D.WithBuffer(BUFFER_STATIC_OBJECTS_PIXEL, ref bufferTexture, BUFFER_STATIC_OBJECTS_DEPTH, ref depthTexture))
                {
                    while (buffer.NextPass())
                    {
                        foreach (var tile in occupiedTiles)
                        {

                            /** Objects **/
                            if ((tile.Type & BlueprintOccupiedTileType.OBJECT) == BlueprintOccupiedTileType.OBJECT)
                            {
                                var objects = Blueprint.GetObjects(tile.TileX, tile.TileY, 1); //TODO: Level
                                foreach (var obj in objects.Objects)
                                {
                                    var renderInfo = GetRenderInfo(obj);
                                    if (renderInfo.Layer == WorldObjectRenderLayer.STATIC)
                                    {
                                        var tilePosition = obj.Position;
                                        _2d.OffsetPixel(state.WorldSpace.GetScreenFromTile(tilePosition) + pxOffset);
                                        _2d.OffsetTile(tilePosition);
                                        _2d.SetObjID(obj.ObjectID);
                                        obj.Draw(gd, state);
                                    }
                                }
                            }
                        }
                    }
                }

                StaticObjects = bufferTexture.Get();
                StaticObjectsDepth = depthTexture.Get();

                //StaticObjects.Save("C:\\static.png", ImageFileFormat.Png);
            }
        }

        /// <summary>
        /// Paint to screen
        /// </summary>
        /// <param name="gd"></param>
        /// <param name="state"></param>
        public void Draw(GraphicsDevice gd, WorldState state){

            var _2d = state._2D;
            _2d.ResetMatrices(gd.Viewport.Width, gd.Viewport.Height); //todo: render to texture support
            /**
             * Draw static layers
             */
            _2d.OffsetPixel(Vector2.Zero);

            if (StaticTerrain != null){
                _2d.DrawBasic(StaticTerrain, Vector2.Zero);
            }
            if (StaticFloor != null){
                _2d.DrawBasic(StaticFloor, Vector2.Zero);
            }
            if (StaticArch != null && StaticArchDepth != null)
            {
                _2d.DrawBasicRestoreDepth(StaticArch, StaticArchDepth, Vector2.Zero);
            }

            if (StaticObjects != null && StaticObjectsDepth != null)
            {
                _2d.DrawBasicRestoreDepth(StaticObjects, StaticObjectsDepth, Vector2.Zero);
            }

            /**
             * Draw dynamic objects. If an object has been static for X frames move it back into the static layer
             */

            var occupiedTiles = Blueprint.GetOccupiedTiles(state.Rotation);
            var pxOffset = state.WorldSpace.GetScreenOffset();

            foreach (var tile in occupiedTiles)
            {

                /** Objects **/
                if ((tile.Type & BlueprintOccupiedTileType.OBJECT) == BlueprintOccupiedTileType.OBJECT)
                {
                    var objects = Blueprint.GetObjects(tile.TileX, tile.TileY, 1); //TODO: Level
                    foreach (var obj in objects.Objects)
                    {
                        var renderInfo = GetRenderInfo(obj);
                        if (renderInfo.Layer == WorldObjectRenderLayer.DYNAMIC)
                        {
                            var tilePosition = obj.Position;
                            _2d.OffsetPixel(state.WorldSpace.GetScreenFromTile(tilePosition) + pxOffset);
                            _2d.OffsetTile(tilePosition);
                            _2d.SetObjID(obj.ObjectID);
                            obj.Draw(gd, state);
                        }
                    }
                }
            }
        }
    }

    public class WorldObjectRenderInfo
    {
        public WorldObjectRenderLayer Layer = WorldObjectRenderLayer.STATIC;
    }

    public enum WorldObjectRenderLayer
    {
        STATIC,
        DYNAMIC
    }

    public struct WorldTileRenderingInfo
    {
        public bool Dirty;
        public Texture2D Pixel;
        public Texture2D ZBuffer;
    }
}
