/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using FSO.LotView.Model;
using Microsoft.Xna.Framework;
using FSO.Common.Utils;
using FSO.Common.Rendering.Framework;
using FSO.LotView.Components;
using System.IO;
using FSO.LotView.Utils;
using FSO.Common;

namespace FSO.LotView
{
    /// <summary>
    /// Handles rendering the 2D world
    /// </summary>
    public class World2D : IDisposable
    {
        public static SurfaceFormat[] BUFFER_SURFACE_FORMATS = new SurfaceFormat[] {
            /** Static Buffers **/
            SurfaceFormat.Color,
            SurfaceFormat.Color, //depth, using a 24-bit packed format

            /** Object ID buffer **/
            SurfaceFormat.Color,

            /** Obj thumbnail buffers **/
            SurfaceFormat.Color,
            SurfaceFormat.Color, //depth, using a 24-bit packed format

            /** Lot Thumbnail Buffer **/
            SurfaceFormat.Color
        };

        public static bool[] FORMAT_ALWAYS_DEPTHSTENCIL = new bool[] {
            /** Static Object Buffers **/
            true,
            false, //depth, using a 24-bit packed format

            /** Object ID buffer **/
            true,

            //Thumbnail depth
            true,
            false,

            //lot thumb
            true,
        };

        public static readonly int NUM_2D_BUFFERS = 6;
        public static readonly int BUFFER_STATIC = 0;
        public static readonly int BUFFER_STATIC_DEPTH = 1;
        public static readonly int BUFFER_OBJID = 2;
        public static readonly int BUFFER_THUMB = 3; //used for drawing thumbnails
        public static readonly int BUFFER_THUMB_DEPTH = 4; //used for drawing thumbnails
        public static readonly int BUFFER_LOTTHUMB = 5;


        public static readonly int SCROLL_BUFFER = 512; //resolution to add to render size for scroll reasons
        
        protected Blueprint Blueprint;
        private Dictionary<WorldComponent, WorldObjectRenderInfo> RenderInfo = new Dictionary<WorldComponent, WorldObjectRenderInfo>();

        private List<_2DDrawBuffer> StaticObjectsCache = new List<_2DDrawBuffer>();
        private ScrollBuffer StaticObjects;

        private List<_2DDrawBuffer> StaticFloorCache = new List<_2DDrawBuffer>();
        private List<_2DDrawBuffer> StaticWallCache = new List<_2DDrawBuffer>();
        private ScrollBuffer StaticFloor;
        private ScrollBuffer StaticWall;
        private int LastSubLightUpdate = 0; //rotate through subworlds to update shadows periodically.

        protected int TicksSinceLight = 0;

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
        public virtual short GetObjectIDAtScreenPos(int x, int y, GraphicsDevice gd, WorldState state)
        {
            /** Draw all objects to a texture as their IDs **/
            var oldCenter = state.CenterTile;
            var tileOff = state.WorldSpace.GetTileFromScreen(new Vector2(x, y));
            state.CenterTile += tileOff;
            var pxOffset = state.WorldSpace.GetScreenOffset();
            var _2d = state._2D;
            Promise<Texture2D> bufferTexture = null;

            var worldBounds = new Rectangle((-pxOffset).ToPoint(), new Point(1, 1));

            state.TempDraw = true;
            state._2D.OBJIDMode = true;
            state._3D.OBJIDMode = true;
            using (var buffer = state._2D.WithBuffer(BUFFER_OBJID, ref bufferTexture))
            {
                _2d.SetScroll(-pxOffset);
                
                while (buffer.NextPass())
                {
                    foreach (var obj in Blueprint.Objects) { 

                                var tilePosition = obj.Position;

                                if (obj.Level != state.Level) continue;

                                var oPx = state.WorldSpace.GetScreenFromTile(tilePosition);
                                obj.ValidateSprite(state);
                                var offBound = new Rectangle(obj.Bounding.Location + oPx.ToPoint(), obj.Bounding.Size);
                                if (!offBound.Intersects(worldBounds)) continue;

                                var renderInfo = GetRenderInfo(obj);
                                
                                _2d.OffsetPixel(oPx);
                                _2d.OffsetTile(tilePosition);
                                _2d.SetObjID(obj.ObjectID);
                                obj.Draw(gd, state);
                    }

                    state._3D.Begin(gd);
                    foreach (var avatar in Blueprint.Avatars)
                    {
                        _2d.OffsetPixel(state.WorldSpace.GetScreenFromTile(avatar.Position));
                        _2d.OffsetTile(avatar.Position);
                        avatar.Draw(gd, state);
                    }
                    state._3D.End();
                }
                
            }
            state._3D.OBJIDMode = false;
            state._2D.OBJIDMode = false;
            state.TempDraw = false;
            state.CenterTile = oldCenter;

            var tex = bufferTexture.Get();
            Color[] data = new Color[1];
            tex.GetData<Color>(data);
            var f = Vector3.Dot(new Vector3(data[0].R / 255.0f, data[0].G / 255.0f, data[0].B / 255.0f), new Vector3(1.0f, 1/255.0f, 1/65025.0f));
            return (short)Math.Round(f*65535f);
        }

        /// <summary>
        /// Gets an object group's thumbnail provided an array of objects.
        /// </summary>
        /// <param name="objects">The object components to draw.</param>
        /// <param name="gd">GraphicsDevice instance.</param>
        /// <param name="state">WorldState instance.</param>
        /// <returns>Object's ID if the object was found at the given position.</returns>
        public virtual Texture2D GetObjectThumb(ObjectComponent[] objects, Vector3[] positions, GraphicsDevice gd, WorldState state)
        {
            var oldZoom = state.Zoom;
            var oldRotation = state.Rotation;
            var oldPreciseZoom = state.PreciseZoom;
            /** Center average position **/
            Vector3 average = new Vector3();
            for (int i = 0; i < positions.Length; i++)
            {
                average += positions[i];
            }
            average /= positions.Length;

            state.SilentZoom = WorldZoom.Near;
            state.SilentRotation = WorldRotation.BottomRight;
            state.SilentPreciseZoom = 1;
            state._2D.PreciseZoom = state.PreciseZoom;
            state.WorldSpace.Invalidate();
            state.InvalidateCamera();
            state.DrawOOB = true;
            state.TempDraw = true;
            var pxOffset = new Vector2(442, 275) - state.WorldSpace.GetScreenFromTile(average);

            var _2d = state._2D;
            Promise<Texture2D> bufferTexture = null;
            Promise<Texture2D> depthTexture = null;
            state._2D.OBJIDMode = false;
            Rectangle bounds = new Rectangle();
            state.ClearLighting(false);

            //Blueprint.SetLightColor(WorldContent._2DWorldBatchEffect, Color.White, Color.White);
            //Blueprint.SetLightColor(WorldContent.GrassEffect, Color.White, Color.White);
            //Blueprint.SetLightColor(Vitaboy.Avatar.Effect, Color.White, Color.White);

            using (var buffer = state._2D.WithBuffer(BUFFER_THUMB, ref bufferTexture, BUFFER_THUMB_DEPTH, ref depthTexture))
            {
                _2d.SetScroll(new Vector2());
                while (buffer.NextPass())
                {
                    for (int i=0; i<objects.Length; i++)
                    {
                        var obj = objects[i];
                        var tilePosition = positions[i];

                        //we need to trick the object into believing it is in a set world state.
                        var oldObjRot = obj.Direction;
                        var oldRoom = obj.Room;

                        obj.Direction = Direction.NORTH;
                        obj.Room = 65535;
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
                        obj.Room = oldRoom;
                        state.SilentZoom = oldZoom;
                        state.SilentRotation = oldRotation;
                        obj.OnRotationChanged(state);
                        obj.OnZoomChanged(state);
                    }
                    bounds = _2d.GetSpriteListBounds();
                }
            }
            bounds.Inflate(1, 1);
            bounds.X = Math.Max(0, Math.Min(1023, bounds.X));
            bounds.Y = Math.Max(0, Math.Min(1023, bounds.Y));
            if (bounds.Width + bounds.X > 1024) bounds.Width = 1024 - bounds.X;
            if (bounds.Height + bounds.Y > 1024) bounds.Height = 1024 - bounds.Y;

            //return things to normal
            state.DrawOOB = false;
            state.SilentPreciseZoom = oldPreciseZoom;
            state.WorldSpace.Invalidate();
            state.InvalidateCamera();
            state.TempDraw = false;

            var tex = bufferTexture.Get();
            return TextureUtils.Clip(gd, tex, bounds);
        }

        /// <summary>
        /// Gets the current lot's thumbnail.
        /// </summary>
        /// <param name="objects">The object components to draw.</param>
        /// <param name="gd">GraphicsDevice instance.</param>
        /// <param name="state">WorldState instance.</param>
        /// <returns>Object's ID if the object was found at the given position.</returns>
        public virtual Texture2D GetLotThumb(GraphicsDevice gd, WorldState state, Action<Texture2D> rooflessCallback)
        {
            if (!(state.Camera is WorldCamera)) return new Texture2D(gd, 8, 8);
            var oldZoom = state.Zoom;
            var oldRotation = state.Rotation;
            var oldLevel = state.Level;
            var oldCutaway = Blueprint.Cutaway;
            var wCam = (WorldCamera)state.Camera;
            var oldViewDimensions = wCam.ViewDimensions;
            //wCam.ViewDimensions = new Vector2(-1, -1);
            var oldPreciseZoom = state.PreciseZoom;

            //full invalidation because we must recalculate all object sprites. slow but necessary!
            state.Zoom = WorldZoom.Far;
            state.Rotation = WorldRotation.TopLeft;
            state.Level = Blueprint.Stories;
            state.PreciseZoom = 1/4f;
            state._2D.PreciseZoom = state.PreciseZoom;
            state.WorldSpace.Invalidate();
            state.InvalidateCamera();

            var oldCenter = state.CenterTile;
            state.CenterTile = Blueprint.GetThumbCenterTile(state);
            state.CenterTile -= state.WorldSpace.GetTileFromScreen(new Vector2((576 - state.WorldSpace.WorldPxWidth)*4, (576 - state.WorldSpace.WorldPxHeight)*4) / 2);
            var pxOffset = -state.WorldSpace.GetScreenOffset();
            state.TempDraw = true;
            Blueprint.Cutaway = new bool[Blueprint.Cutaway.Length];

            var _2d = state._2D;
            state.ClearLighting(false);
            Promise<Texture2D> bufferTexture = null;
            var lastLight = state.OutsideColor;
            state.OutsideColor = Color.White;
            state._2D.OBJIDMode = false;
            using (var buffer = state._2D.WithBuffer(BUFFER_LOTTHUMB, ref bufferTexture))
            {
                _2d.SetScroll(pxOffset);
                while (buffer.NextPass())
                {
                    _2d.Pause();
                    _2d.Resume(); 
                    Blueprint.FloorGeom.SliceReset(gd, new Rectangle(6, 6, Blueprint.Width - 13, Blueprint.Height - 13));
                    //Blueprint.SetLightColor(WorldContent.GrassEffect, Color.White, Color.White);
                    Blueprint.Terrain.Draw(gd, state);
                    Blueprint.Terrain.DrawMask(gd, state, state.Camera.View, state.Camera.Projection);
                    Blueprint.WallComp.Draw(gd, state);
                    _2d.Pause();
                    _2d.Resume();
                    foreach (var obj in Blueprint.Objects)
                    {
                        var renderInfo = GetRenderInfo(obj);
                        var tilePosition = obj.Position;
                        _2d.OffsetPixel(state.WorldSpace.GetScreenFromTile(tilePosition));
                        _2d.OffsetTile(tilePosition);
                        obj.Draw(gd, state);
                    }
                    _2d.Pause();
                    _2d.Resume();
                    rooflessCallback?.Invoke(bufferTexture.Get());
                    Blueprint.RoofComp.Draw(gd, state);
                }

            }

            Blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.LIGHTING_CHANGED));
            Blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.FLOOR_CHANGED));
            //return things to normal
            //state.PrepareLighting();
            state.OutsideColor = lastLight;
            state.PreciseZoom = oldPreciseZoom;
            state.WorldSpace.Invalidate();
            state.InvalidateCamera();
            wCam.ViewDimensions = oldViewDimensions;
            state.TempDraw = false;
            state.CenterTile = oldCenter;

            state.Zoom = oldZoom;
            state.Rotation = oldRotation;
            state.Level = oldLevel;
            Blueprint.Cutaway = oldCutaway;

            var tex = bufferTexture.Get();
            return tex; //TextureUtils.Clip(gd, tex, bounds);
        }

        /// <summary>
        /// Prep work before screen is painted
        /// </summary>
        /// <param name="gd"></param>
        /// <param name="state"></param>
        public virtual void PreDraw(GraphicsDevice gd, WorldState state)
        {
            //var oht = state.BaseHeight;
            //state.BaseHeight = 0;
            var pxOffset = -state.WorldSpace.GetScreenOffset();
            //state.BaseHeight = oht;
            var damage = Blueprint.Damage;
            var _2d = state._2D;

            /**
             * Tasks:
             *  If zoom or rotation has changed, redraw all static layers
             *  If scroll has changed, redraw static layer if the scroll is outwith the buffered region
             *  If architecture has changed, redraw appropriate static layer
             *  If there is a new object in the static layer, redraw the static layer
             *  If an objects in the static layer has changed, redraw the static layer and move the object to the dynamic layer
             *  If wall visibility has changed, redraw wall layer (should think about how this works with breakthrough wall mode
             */

            var im = state.ThisFrameImmediate;
            var redrawStaticObjects = im;
            var redrawFloor = im;
            var redrawWall = im;

            var recacheWalls = false;
            var recacheCutaway = false;
            var recacheFloors = false;
            var recacheTerrain = false;
            var recacheObjects = false;
            var drawImmediate = false;

            var lightChangeType = 0;

            if (TicksSinceLight++ > 60 * 4) damage.Add(new BlueprintDamage(BlueprintDamageType.OUTDOORS_LIGHTING_CHANGED));

            WorldObjectRenderInfo info = null;

            foreach (var item in damage){
                switch (item.Type){
                    case BlueprintDamageType.OPENGL_SECOND_DRAW:
                        recacheFloors = true;
                        break;
                    case BlueprintDamageType.ROTATE:
                    case BlueprintDamageType.ZOOM:
                    case BlueprintDamageType.LEVEL_CHANGED:
                        recacheObjects = true;
                        recacheWalls = true;
                        recacheFloors = true;
                        if (item.Type != BlueprintDamageType.OPENGL_SECOND_DRAW && !FSOEnvironment.DirectX)
                        {
                            //need to draw one frame after this in opengl.
                            //to mitigate a problem with floor content not setting to "wrap" mode
                            GameThread.NextUpdate(x =>
                            {
                                Blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.OPENGL_SECOND_DRAW));
                            });
                        }
                        break;
                    case BlueprintDamageType.SCROLL:
                        if (StaticObjects == null || StaticObjects.PxOffset != GetScrollIncrement(pxOffset, state))
                        {
                            redrawFloor = true;
                            redrawWall = true;
                            redrawStaticObjects = true;
                        }
                        break;
                    case BlueprintDamageType.PRECISE_ZOOM:
                        drawImmediate = true;
                        redrawFloor = redrawWall = redrawStaticObjects = true;
                        break;
                    case BlueprintDamageType.LIGHTING_CHANGED:
                        if (lightChangeType >= 2) break;
                        var room = (ushort)item.TileX;
                        redrawFloor = true;
                        redrawWall = true;
                        redrawStaticObjects = true;

                        state.Light?.InvalidateRoom(room);
                        Blueprint.GenerateRoomLights();
                        state.OutsideColor = Blueprint.RoomColors[1];
                        state._3D.RoomLights = Blueprint.RoomColors;
                        state.OutsidePx.SetData(new Color[] { new Color(Blueprint.OutsideColor, (Blueprint.OutsideColor.R + Blueprint.OutsideColor.G + Blueprint.OutsideColor.B) / (255 * 3f)) });
                        if (state.AmbientLight != null)
                        {
                            state.AmbientLight.SetData(Blueprint.RoomColors);
                        }
                        TicksSinceLight = 0;
                        break;
                    case BlueprintDamageType.OUTDOORS_LIGHTING_CHANGED:
                        if (lightChangeType >= 1) break;
                        lightChangeType = 1;
                        redrawFloor = true;
                        redrawWall = true;
                        redrawStaticObjects = true;

                        Blueprint.GenerateRoomLights();
                        state.OutsideColor = Blueprint.RoomColors[1];
                        state._3D.RoomLights = Blueprint.RoomColors;
                        state.OutsidePx.SetData(new Color[] { new Color(Blueprint.OutsideColor, (Blueprint.OutsideColor.R + Blueprint.OutsideColor.G + Blueprint.OutsideColor.B) / (255 * 3f)) });
                        if (state.AmbientLight != null)
                        {
                            state.AmbientLight.SetData(Blueprint.RoomColors);
                        }
                        state.Light?.InvalidateOutdoors();

                        if (Blueprint.SubWorlds.Count > 0)
                        {
                            Blueprint.SubWorlds[LastSubLightUpdate].RefreshLighting();
                            LastSubLightUpdate = (LastSubLightUpdate + 1) % Blueprint.SubWorlds.Count;
                        }

                        TicksSinceLight = 0;
                        break;
                    case BlueprintDamageType.OBJECT_MOVE:
                        /** Redraw if its in static layer **/
                        info = GetRenderInfo(item.Component);
                        if (info.Layer == WorldObjectRenderLayer.STATIC){
                            recacheObjects = true;
                            info.Layer = WorldObjectRenderLayer.DYNAMIC;
                        }
                        if (item.Component is ObjectComponent) ((ObjectComponent)item.Component).DynamicCounter = 0;
                        break;
                    case BlueprintDamageType.OBJECT_GRAPHIC_CHANGE:
                        /** Redraw if its in static layer **/
                        info = GetRenderInfo(item.Component);
                        if (info.Layer == WorldObjectRenderLayer.STATIC){
                            recacheObjects = true;
                            info.Layer = WorldObjectRenderLayer.DYNAMIC;
                        }
                        if (item.Component is ObjectComponent) ((ObjectComponent)item.Component).DynamicCounter = 0;
                        break;
                    case BlueprintDamageType.OBJECT_RETURN_TO_STATIC:
                        info = GetRenderInfo(item.Component);
                        if (info.Layer == WorldObjectRenderLayer.DYNAMIC)
                        {
                            recacheObjects = true;
                            info.Layer = WorldObjectRenderLayer.STATIC;
                        }
                        break;
                    case BlueprintDamageType.WALL_CUT_CHANGED:
                        recacheCutaway = true;
                        break;
                    case BlueprintDamageType.ROOF_STYLE_CHANGED:
                        Blueprint.RoofComp.StyleDirty = true;
                        break;
                    case BlueprintDamageType.ROOM_CHANGED:
                        for (sbyte i = 0; i < Blueprint.RoomMap.Length; i++)
                        {
                            state.Rooms.SetRoomMap(i, Blueprint.RoomMap[i]);
                        }
                        if (state.Light != null)
                        {
                            if (lightChangeType < 2)
                            {
                                lightChangeType = 2;
                                state.Light.InvalidateAll();
                            }
                        }
                        Blueprint.Indoors = null;
                        Blueprint.RoofComp.ShapeDirty = true;
                        break;
                    case BlueprintDamageType.FLOOR_CHANGED:
                    case BlueprintDamageType.WALL_CHANGED:
                        //recacheTerrain = true;
                        recacheFloors = true;
                        recacheWalls = true;
                        Blueprint.RoofComp.ShapeDirty = true;
                        break;
                }
            }
            if (recacheFloors || recacheTerrain) redrawFloor = true;
            if (recacheWalls || recacheCutaway) redrawWall = true;
            if (recacheObjects) redrawStaticObjects = true;
            damage.Clear();

            //scroll buffer loads in increments of SCROLL_BUFFER
            var newOff = GetScrollIncrement(pxOffset, state);
            var oldCenter = state.CenterTile;
            state.CenterTile += state.WorldSpace.GetTileFromScreen(newOff-pxOffset); //offset the scroll to the position of the scroll buffer.
            var tileOffset = state.CenterTile;

            pxOffset = newOff;

            if (recacheTerrain)
                Blueprint.Terrain.RegenTerrain(gd, Blueprint);

            if (recacheWalls)
                Blueprint.WCRC?.Generate(gd, state, false);
            else if (recacheCutaway)
                Blueprint.WCRC?.Generate(gd, state, true);

            state.Light?.ParseInvalidated((sbyte)(state.Level + ((state.DrawRoofs) ? 1 : 0)), state);

            if (recacheWalls || recacheCutaway)
            {
                _2d.Pause();
                _2d.Resume(); //clear the sprite buffer before we begin drawing what we're going to cache
                Blueprint.WallComp.Draw(gd, state);
                ClearDrawBuffer(StaticWallCache);
                state.PrepareLighting();
                _2d.End(StaticWallCache, true);
            }

            if (recacheFloors)
            {
                _2d.Pause();
                _2d.Resume(); //clear the sprite buffer before we begin drawing what we're going to cache
                //Blueprint.FloorComp.Draw(gd, state);
                Blueprint.FloorGeom.FullReset(gd, state.BuildMode > 1);
                ClearDrawBuffer(StaticFloorCache);
                _2d.End(StaticFloorCache, true);
            }

            if (recacheObjects)
            {
                _2d.Pause();
                _2d.Resume();

                foreach (var obj in Blueprint.Objects)
                {
                    if (obj.Level > state.Level) continue;
                    var renderInfo = GetRenderInfo(obj);
                    if (renderInfo.Layer == WorldObjectRenderLayer.STATIC)
                    {
                        var tilePosition = obj.Position;
                        _2d.OffsetPixel(state.WorldSpace.GetScreenFromTile(tilePosition));
                        _2d.OffsetTile(tilePosition);
                        _2d.SetObjID(obj.ObjectID);
                        obj.Draw(gd, state);
                    }
                }
                ClearDrawBuffer(StaticObjectsCache);
                _2d.End(StaticObjectsCache, true);
            }

            if (!drawImmediate)
            {
                state.PrepareLighting();

                if (redrawStaticObjects || redrawFloor || redrawWall)
                {
                    /** Draw static objects to a texture **/
                    Promise<Texture2D> bufferTexture = null;
                    Promise<Texture2D> depthTexture = null;
                    using (var buffer = state._2D.WithBuffer(BUFFER_STATIC, ref bufferTexture, BUFFER_STATIC_DEPTH, ref depthTexture))
                    {

                        while (buffer.NextPass())
                        {
                            DrawFloorBuf(gd, state, pxOffset);
                            DrawWallBuf(gd, state, pxOffset);
                            DrawObjBuf(gd, state, pxOffset);
                        }
                    }
                    StaticObjects = new ScrollBuffer(bufferTexture.Get(), depthTexture.Get(), newOff, new Vector3(tileOffset, 0));
                }
            }
            //state._2D.PreciseZoom = state.PreciseZoom;
            state.CenterTile = oldCenter; //revert to our real scroll position

            state.ThisFrameImmediate = drawImmediate;
        }

        private void DrawFloorBuf(GraphicsDevice gd, WorldState state, Vector2 pxOffset)
        {
            var _2d = state._2D;
            _2d.SetScroll(pxOffset);
            if (Blueprint.Terrain != null)
            {
                Blueprint.Terrain.DepthMode = _2d.OutputDepth;
                Blueprint.Terrain.Draw(gd, state);
            }
            _2d.RenderCache(StaticFloorCache);
            foreach (var sub in Blueprint.SubWorlds) sub.DrawArch(gd, state);
        }

        private void DrawWallBuf(GraphicsDevice gd, WorldState state, Vector2 pxOffset)
        {
            var _2d = state._2D;
            _2d.SetScroll(pxOffset);
            _2d.RenderCache(StaticWallCache);
        }

        private void DrawObjBuf(GraphicsDevice gd, WorldState state, Vector2 pxOffset)
        {
            var _2d = state._2D;
            foreach (var sub in Blueprint.SubWorlds) sub.DrawObjects(gd, state);
            _2d.SetScroll(pxOffset);
            _2d.RenderCache(StaticObjectsCache);
        }

        private Vector2 GetScrollIncrement(Vector2 pxOffset, WorldState state)
        {
            var scrollSize = SCROLL_BUFFER / state.PreciseZoom;
            return new Vector2((float)Math.Floor(pxOffset.X / scrollSize) * scrollSize, (float)Math.Floor(pxOffset.Y / scrollSize) * scrollSize);
        }

        /// <summary>
        /// Paint to screen
        /// </summary>
        /// <param name="gd"></param>
        /// <param name="state"></param>
        public virtual void Draw(GraphicsDevice gd, WorldState state){

            var _2d = state._2D;
            /**
             * Draw static layers
             */
            _2d.OffsetPixel(Vector2.Zero);
            _2d.SetScroll(new Vector2());

            var pxOffset = -state.WorldSpace.GetScreenOffset();
            var tileOffset = state.CenterTile;

            if (state.ThisFrameImmediate)
            {
                _2d.SetScroll(pxOffset);
                _2d.Begin(state.Camera);

                var p2O = pxOffset;
                DrawFloorBuf(gd, state, p2O);
                DrawWallBuf(gd, state, p2O);
                DrawObjBuf(gd, state, p2O);
            }
            else
            {
                _2d.SetScroll(new Vector2());
                _2d.Begin(state.Camera);
                state._2D.PreciseZoom = 1f;
                if (StaticObjects != null)
                {
                    _2d.DrawScrollBuffer(StaticObjects, pxOffset, new Vector3(tileOffset, 0), state);
                    _2d.Pause();
                    _2d.Resume();
                }
                state._2D.PreciseZoom = state.PreciseZoom;
            }
            _2d.SetScroll(pxOffset);

            _2d.End();
            _2d.Begin(state.Camera);

            /**
             * Draw dynamic objects. If an object has been static for X frames move it back into the static layer
             */

            _2d.SetScroll(pxOffset);

            var size = new Vector2(state.WorldSpace.WorldPxWidth, state.WorldSpace.WorldPxHeight);
            var mainBd = state.WorldSpace.GetScreenFromTile(state.CenterTile);
            var diff = pxOffset - mainBd;
            var worldBounds = new Rectangle((pxOffset).ToPoint(), size.ToPoint());

            foreach (var obj in Blueprint.Objects)
            {
                if (obj.Level > state.Level) continue;
                var renderInfo = GetRenderInfo(obj);
                if (renderInfo.Layer == WorldObjectRenderLayer.DYNAMIC)
                {
                    var tilePosition = obj.Position;
                    var oPx = state.WorldSpace.GetScreenFromTile(tilePosition);
                    obj.ValidateSprite(state);
                    var offBound = new Rectangle(obj.Bounding.Location + oPx.ToPoint(), obj.Bounding.Size);
                    if (!offBound.Intersects(worldBounds)) continue;
                    _2d.OffsetPixel(oPx);
                    _2d.OffsetTile(tilePosition);
                    _2d.SetObjID(obj.ObjectID);
                    obj.Draw(gd, state);
                }
            }

            foreach (var op in Blueprint.ObjectParticles)
            {
                if (op.Level <= state.Level && op.Owner.Visible && (op.Owner.Position.X > -2043 || op.Owner.Position.Y > -2043))
                    op.Draw(gd, state);
            }
        }

        public void ClearDrawBuffer(List<_2DDrawBuffer> buf)
        {
            foreach (var b in buf) b.Dispose();
            buf.Clear();
        }

        public virtual void Dispose()
        {
            ClearDrawBuffer(StaticWallCache);
            ClearDrawBuffer(StaticFloorCache);
            ClearDrawBuffer(StaticObjectsCache);
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
