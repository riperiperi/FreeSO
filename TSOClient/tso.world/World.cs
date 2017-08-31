/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Common.Rendering.Framework;
using FSO.Common.Rendering.Framework.Camera;
using Microsoft.Xna.Framework;
using FSO.LotView.Model;
using Microsoft.Xna.Framework.Graphics;
using FSO.Common.Rendering.Framework.Model;
using FSO.LotView.Components;
using FSO.Common.Utils;
using FSO.Common;
using FSO.LotView.LMap;

namespace FSO.LotView
{
    /// <summary>
    /// Represents world (I.E lots in the game.)
    /// </summary>
    public class World : _3DScene
    {
        /// <summary>
        /// Creates a new World instance.
        /// </summary>
        /// <param name="Device">A GraphicsDevice instance.</param>
        public World(GraphicsDevice Device)
            : base(Device)
        {
        }

        /** How many pixels from each edge of the screen before we start scrolling the view **/
        public int ScrollBounds = 20;
        public uint FrameCounter = 0;
        public uint LastCacheClear = 0;
        public static bool DirectX = false;
        public float Opacity = 1f;
        public float BackbufferScale = 1f;

        public float SmoothZoomTimer = -1;
        public float SmoothZoomFrom = 1f;

        public WorldState State;
        public bool UseBackbuffer = true;
        protected bool HasInitGPU;
        protected bool HasInitBlueprint;
        protected bool HasInit;

        protected World2D _2DWorld = new World2D();
        protected World3D _3DWorld = new World3D();
        protected LMapBatch Light;
        protected Blueprint Blueprint;

        public sbyte Stories
        {
            get
            {
                return Blueprint.Stories;
            }
        }

        /// <summary>
        /// Setup anything that needs a GraphicsDevice
        /// </summary>
        /// <param name="layer"></param>
        public override void Initialize(_3DLayer layer)
        {
            base.Initialize(layer);

            /**
             * Setup world state, this object acts as a facade
             * to world objects as well as providing various
             * state settings for the world and helper functions
             */
            State = new WorldState(layer.Device, layer.Device.Viewport.Width/FSOEnvironment.DPIScaleFactor, layer.Device.Viewport.Height/FSOEnvironment.DPIScaleFactor, this);

            State._3D = new FSO.LotView.Utils._3DWorldBatch(State);
            State._2D = new FSO.LotView.Utils._2DWorldBatch(layer.Device, World2D.NUM_2D_BUFFERS, 
                World2D.BUFFER_SURFACE_FORMATS, World2D.FORMAT_ALWAYS_DEPTHSTENCIL, World2D.SCROLL_BUFFER);

            ChangedWorldConfig(layer.Device);

            PPXDepthEngine.InitGD(layer.Device);
            PPXDepthEngine.InitScreenTargets();

            base.Camera = State.Camera;

            HasInitGPU = true;
            HasInit = HasInitGPU & HasInitBlueprint;
        }

        public void GameResized()
        {
            PPXDepthEngine.InitScreenTargets();
            var newSize = PPXDepthEngine.GetWidthHeight();
            State._2D.GenBuffers(newSize.X, newSize.Y);
            State.SetDimensions(newSize.ToVector2());

            Blueprint?.Damage.Add(new BlueprintDamage(BlueprintDamageType.ZOOM));
        }

        public virtual void InitBlueprint(Blueprint blueprint)
        {
            this.Blueprint = blueprint;
            _2DWorld.Init(blueprint);
            _3DWorld.Init(blueprint);
            Light?.Init(blueprint);
            State.Rooms.Init(blueprint);

            HasInitBlueprint = true;
            HasInit = HasInitGPU & HasInitBlueprint;
        }

        public void InvalidateZoom()
        {
            if (Blueprint == null) { return; }

            foreach (var item in Blueprint.Objects){
                item.OnZoomChanged(State);
            }
            foreach (var sub in Blueprint.SubWorlds) sub.State.Zoom = State.Zoom;
            Blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.ZOOM));

            State._2D?.ClearTextureCache();
        }

        public void InvalidatePreciseZoom()
        {
            if (Blueprint == null) { return; }
            Blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.PRECISE_ZOOM));
        }

        public void InvalidateRotation()
        {
            if (Blueprint == null) { return; }

            foreach (var item in Blueprint.Objects)
            {
                item.OnRotationChanged(State);
            }
            foreach (var sub in Blueprint.SubWorlds) sub.State.Rotation = State.Rotation;
            Blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.ROTATE));

            State._2D?.ClearTextureCache();
        }

        public void InvalidateScroll()
        {
            if (Blueprint == null) { return; }

            foreach (var item in Blueprint.Objects){
                item.OnScrollChanged(State);
            }
            Blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.SCROLL));
        }

        public void InvalidateFloor()
        {
            if (Blueprint == null) { return; }
            Blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.LEVEL_CHANGED));
        }

        public bool TestScroll(UpdateState state)
        {
            var mouse = state.MouseState;

            if (State == null) { return false; }

            var screenWidth = State.WorldSpace.WorldPxWidth;
            var screenHeight = State.WorldSpace.WorldPxHeight;

            /** Corners **/
            var xBound = screenWidth - ScrollBounds;
            var yBound = screenHeight - ScrollBounds;

            var cursor = CursorType.Normal;
            var scrollVector = new Vector2(0, 0);
            if (mouse.X > 0 && mouse.Y > 0 && mouse.X < screenWidth && mouse.Y < screenHeight)
            {
                if (mouse.Y <= ScrollBounds)
                {
                    if (mouse.X <= ScrollBounds)
                    {
                        /** Scroll top left **/
                        cursor = CursorType.ArrowUpLeft;
                        scrollVector = new Vector2(-1, -1);
                    }
                    else if (mouse.X >= xBound)
                    {
                        /** Scroll top right **/
                        cursor = CursorType.ArrowUpRight;
                        scrollVector = new Vector2(1, -1);
                    }
                    else
                    {
                        /** Scroll up **/
                        cursor = CursorType.ArrowUp;
                        scrollVector = new Vector2(0, -1);
                    }
                }
                else if (mouse.Y <= yBound)
                {
                    if (mouse.X <= ScrollBounds)
                    {
                        /** Left **/
                        cursor = CursorType.ArrowLeft;
                        scrollVector = new Vector2(-1, 0);
                    }
                    else if (mouse.X >= xBound)
                    {
                        /** Right **/
                        cursor = CursorType.ArrowRight;
                        scrollVector = new Vector2(1, -1);
                    }
                }
                else
                {
                    if (mouse.X <= ScrollBounds)
                    {
                        /** Scroll bottom left **/
                        cursor = CursorType.ArrowDownLeft;
                        scrollVector = new Vector2(-1, 1);
                    }
                    else if (mouse.X >= xBound)
                    {
                        /** Scroll bottom right **/
                        cursor = CursorType.ArrowDownRight;
                        scrollVector = new Vector2(1, 1);
                    }
                    else
                    {
                        /** Scroll down **/
                        cursor = CursorType.ArrowDown;
                        scrollVector = new Vector2(0, 1);
                    }
                }
            }

            if (cursor != CursorType.Normal)
            {
                /**
                 * Calculate scroll vector based on rotation & scroll type
                 */
                scrollVector = new Vector2();

                var basis = GetScrollBasis(true);

                switch (cursor)
                {
                    case CursorType.ArrowDown:
                        scrollVector = basis[1];
                        break;

                    case CursorType.ArrowUp:
                        scrollVector = -basis[1];
                        break;

                    case CursorType.ArrowLeft:
                        scrollVector = -basis[0];
                        break;

                    case CursorType.ArrowRight:
                        scrollVector = basis[0];
                        break;

                    case CursorType.ArrowUpLeft:
                        scrollVector = -basis[1] - basis[0];
                        scrollVector *= new Vector2(1, 0.5f);
                        break;

                    case CursorType.ArrowUpRight:
                        scrollVector = basis[0] - basis[1];
                        scrollVector *= new Vector2(1, 0.5f);
                        break;

                    case CursorType.ArrowDownLeft:
                        scrollVector = basis[1] - basis[0];
                        scrollVector *= new Vector2(1, 0.5f);
                        break;

                    case CursorType.ArrowDownRight:
                        scrollVector = basis[1] + basis[0];
                        scrollVector *= new Vector2(1, 0.5f);
                        break;

                }

                /** We need to scroll **/
                if (scrollVector != Vector2.Zero)
                {
                    State.CenterTile += scrollVector * 0.0625f * (60f / FSOEnvironment.RefreshRate);
                    State.ScrollAnchor = null;
                }
            }

            if (cursor != CursorType.Normal)
            {
                CursorManager.INSTANCE.SetCursor(cursor);
                return true; //we scrolled, return true and set cursor
            }
            return false;
        }


        public virtual void Scroll (Vector2 dir, bool multiplied)
        {
            var basis = GetScrollBasis(multiplied);
            State.CenterTile += dir.X*basis[0] + dir.Y*basis[1];
        }

        public void Scroll(Vector2 dir)
        {
            Scroll(dir, true);
        }

        public virtual Vector2[] GetScrollBasis(bool multiplied)
        {
            Vector2[] output = new Vector2[2];
            switch (State.Rotation)
            {
                case WorldRotation.TopLeft:
                    output[1] = new Vector2(2, 2);
                    output[0] = new Vector2(1, -1);
                    break;
                case WorldRotation.TopRight:
                    output[1] = new Vector2(2, -2);
                    output[0] = new Vector2(-1, -1);
                    break;
                case WorldRotation.BottomRight:
                    output[1] = new Vector2(-2, -2);
                    output[0] = new Vector2(-1, 1);
                    break;
                case WorldRotation.BottomLeft:
                    output[1] = new Vector2(-2, 2);
                    output[0] = new Vector2(1, 1);
                    break;
            }
            if (multiplied)
            {
                int multiplier = ((1 << (3 - (int)State.Zoom)) * 3) / 2;
                output[0] *= multiplier;
                output[1] *= multiplier;
            }
            return output;
        }

        public void InitiateSmoothZoom(WorldZoom zoom)
        {
            if (!WorldConfig.Current.SmoothZoom)
            {
                return;
            }
            SmoothZoomTimer = 0;
            var curScale = (1 << (3 - (int)State.Zoom));
            var zoomScale = (1 << (3 - (int)zoom));

            SmoothZoomFrom = (float)zoomScale / curScale;
            State.PreciseZoom = SmoothZoomFrom;
        }

        public void CenterTo(EntityComponent comp)
        {
            Vector3 pelvisCenter;
            if (comp is AvatarComponent)
            {
                pelvisCenter = ((AvatarComponent)comp).GetPelvisPosition();
            } else
            {
                pelvisCenter = comp.Position;
            }
            State.CenterTile = new Vector2(pelvisCenter.X, pelvisCenter.Y);
            if (State.Level != comp.Level) State.Level = comp.Level;

            State.CenterTile -= (pelvisCenter.Z/2.95f) * State.WorldSpace.GetTileFromScreen(new Vector2(0, 230)) / (1 << (3 - (int)State.Zoom));

        }

        public void RestoreTerrainToCenterTile()
        {
            //center tiles center the lot on a tile at the base level of 0 elevation.
            var pos = Blueprint.InterpAltitude(new Vector3(State.CenterTile, 0)) + (State.Level - 1) * 2.95f;
            State.CenterTile -= (pos / 2.95f) * State.WorldSpace.GetTileFromScreen(new Vector2(0, 230)) / (1 << (3 - (int)State.Zoom));
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);

            if (State.ScrollAnchor != null)
            {
                CenterTo(State.ScrollAnchor);
            }

            if (SmoothZoomTimer > -1)
            {
                SmoothZoomTimer += 60f / FSOEnvironment.RefreshRate;
                if (SmoothZoomTimer >= 15)
                {
                    State.PreciseZoom = 1f;
                    SmoothZoomTimer = -1;
                }
                else
                {
                    var p = Math.Sin((SmoothZoomTimer / 30.0) * Math.PI);
                    State.PreciseZoom = (float)((p) + (1 - p) * SmoothZoomFrom);
                }
            }
        }

        protected void BoundView()
        {
            //bound the scroll so we can't see gray space.
            float boundfactor = 0.5f;
            switch (State.Zoom)
            {
                case WorldZoom.Near:
                    boundfactor = 1.20f; break;
                case WorldZoom.Medium:
                    boundfactor = 1.05f; break;
            }
            boundfactor *= Blueprint?.Width ?? 64;
            var off = 0.5f * (Blueprint?.Width ?? 64);
            var tile = State.CenterTile;
            tile = new Vector2(Math.Min(boundfactor + off, Math.Max(off - boundfactor, tile.X)), Math.Min(boundfactor + off, Math.Max(off - boundfactor, tile.Y)));
            if (tile != State.CenterTile) State.CenterTile = tile;
        }

        /// <summary>
        /// Pre-Draw
        /// </summary>
        /// <param name="device"></param>
        public override void PreDraw(GraphicsDevice device)
        {
            base.PreDraw(device);
            if (HasInit == false) { return; }
            BoundView();
            State._2D.PreciseZoom = State.PreciseZoom;
            State.OutsideColor = Blueprint.OutsideColor;
            FSO.Common.Rendering.Framework.GameScreen.ClearColor = new Color(new Color(0x72, 0x72, 0x72).ToVector4() * State.OutsideColor.ToVector4());
            foreach (var sub in Blueprint.SubWorlds) sub.PreDraw(device, State);
            if (Blueprint != null)
            {
                foreach (var ent in Blueprint.Objects)
                {
                    ent.Update(null, State);
                }
            }

            //For all the tiles in the dirty list, re-render them
            //PPXDepthEngine.SetPPXTarget(null, null, true);
            State.PrepareLighting();
            State._2D.Begin(this.State.Camera);
            _2DWorld.PreDraw(device, State);
            device.SetRenderTarget(null);
            State._2D.End();

            State._3D.Begin(device);
            _3DWorld.PreDraw(device, State);
            State._3D.End();

            if (UseBackbuffer)
            {

                PPXDepthEngine.SetPPXTarget(null, null, true);
                InternalDraw(device);
                device.SetRenderTarget(null);
            }

            return;
        }

        /// <summary>
        /// We will just take over the whole rendering of this scene :)
        /// </summary>
        /// <param name="device"></param>
        public override void Draw(GraphicsDevice device){
            if (HasInit == false) { return; }

            FrameCounter++;
            if (FrameCounter < LastCacheClear + 60*60)
            {
                State._2D.ClearTextureCache();
            }
            if (!UseBackbuffer)
                InternalDraw(device);
            else
                PPXDepthEngine.DrawBackbuffer(Opacity, BackbufferScale);
            return;
        }

        protected virtual void InternalDraw(GraphicsDevice device)
        {
            device.RasterizerState = RasterizerState.CullNone;
            State.PrepareLighting();
            State._2D.OutputDepth = true;

            State._3D.Begin(device);
            State._2D.Begin(this.State.Camera);

            var pxOffset = -State.WorldSpace.GetScreenOffset();
            //State._2D.PreciseZoom = State.PreciseZoom;
            State._2D.ResetMatrices(device.Viewport.Width, device.Viewport.Height);
            _3DWorld.DrawBefore2D(device, State);

            _2DWorld.Draw(device, State);

            State._2D.Pause();
            State._2D.Resume();

            _3DWorld.DrawAfter2D(device, State);
            State._2D.SetScroll(pxOffset);
            State._2D.End();
            State._3D.End();
            State._2D.OutputDepth = false;
        }

        public float? BoxRC(Ray ray, BoundingBox box)
        {
            const float Epsilon = 1e-6f;

            float? tMin = null, tMax = null;

            if (Math.Abs(ray.Direction.X) < Epsilon)
            {
                if (ray.Position.X < box.Min.X || ray.Position.X > box.Max.X)
                    return null;
            }
            else
            {
                tMin = (box.Min.X - ray.Position.X) / ray.Direction.X;
                tMax = (box.Max.X - ray.Position.X) / ray.Direction.X;

                if (tMin > tMax)
                {
                    var temp = tMin;
                    tMin = tMax;
                    tMax = temp;
                }
                if (tMin < 0) tMin = tMax;
            }

            if (Math.Abs(ray.Direction.Z) < Epsilon)
            {
                if (ray.Position.Z < box.Min.Z || ray.Position.Z > box.Max.Z)
                    return null;
            }
            else
            {
                var tMinZ = (box.Min.Z - ray.Position.Z) / ray.Direction.Z;
                var tMaxZ = (box.Max.Z - ray.Position.Z) / ray.Direction.Z;

                if (tMinZ > tMaxZ)
                {
                    var temp = tMinZ;
                    tMinZ = tMaxZ;
                    tMaxZ = temp;
                }
                if (tMinZ < 0) tMinZ = tMaxZ;

                //if ((tMin.HasValue && tMin > tMaxZ) || (tMax.HasValue && tMinZ > tMax))
                //    return null;

                if (!tMin.HasValue || tMinZ > tMin) tMin = tMinZ;
                if (!tMax.HasValue || tMaxZ < tMax) tMax = tMaxZ;
            }

            // a negative tMin means that the intersection point is behind the ray's origin
            // we discard these as not hitting the AABB
            if (tMin < 0) return null;

            return tMin;
        }

        public Vector2 EstTileAtPosWithScroll(Vector2 pos)
        {
            var sPos = new Vector3(pos, 0);
            
            var p1 = State.Device.Viewport.Unproject(sPos, State.Camera.Projection, State.Camera.View, Matrix.Identity);
            sPos.Z = 1;
            var p2 = State.Device.Viewport.Unproject(sPos, State.Camera.Projection, State.Camera.View, Matrix.Identity);
            var dir = p2 - p1;
            dir.Normalize();
            var ray = new Ray(p1, p2 - p1);
            ray.Direction.Normalize();
            ray.Position -= new Vector3(0, (State.Level-1) * 2.95f * 3, 0);
            
            var baseBox = new BoundingBox(new Vector3(0, -5000, 0), new Vector3(Blueprint.Width*3, 5000, Blueprint.Height*3));
            if (baseBox.Contains(ray.Position) != ContainmentType.Contains)
            {
                //move ray start inside box
                var i = baseBox.Intersects(ray);
                if (i != null)
                {
                    ray.Position += ray.Direction * (i.Value + 0.01f);
                }
            }

            var mx = (int)ray.Position.X / 3;
            var my = (int)ray.Position.Z / 3;

            int iteration = 0;
            while (mx > 0 && mx < Blueprint.Width && my > 0 && my<Blueprint.Width)
            {
                var plane = new Plane(
                    new Vector3(mx * 3, Blueprint.Altitude[my * Blueprint.Width + mx] * Blueprint.TerrainFactor*3, my * 3),
                    new Vector3(mx * 3+3, Blueprint.Altitude[my * Blueprint.Width + ((mx+1)%Blueprint.Width)] * Blueprint.TerrainFactor*3, my * 3),
                    new Vector3(mx * 3+3, Blueprint.Altitude[((my+1)%Blueprint.Height) * Blueprint.Width + ((mx+1)%Blueprint.Width)] * Blueprint.TerrainFactor*3, my * 3+3)
                    );
                var tBounds = new BoundingBox(new Vector3(mx*3, -5000, my*3), new Vector3(mx*3+3, 5000, my*3+3));

                var t1 = ray.Intersects(plane);
                var t2 = BoxRC(ray, tBounds);
                if (plane.DotCoordinate(ray.Position) > 0) t1 = 0;
                if (t1 != null && t2 != null && t1.Value < t2.Value)
                {
                    //hit the ground...
                    ray.Position += ray.Direction * (t1.Value + 0.00001f);
                    return new Vector2(ray.Position.X / 3, ray.Position.Z / 3);
                }
                if (t2 == null) break;
                ray.Position += ray.Direction * (t2.Value + 0.00001f);
                mx = (int)ray.Position.X / 3;
                my = (int)ray.Position.Z / 3;
                if (iteration++ > 1000) break;
            }

            //fall back to base positioning
            var bplane = new Plane(new Vector3(0, 0, 0), new Vector3(Blueprint.Width * 3, 0, 0), new Vector3(0, 0, Blueprint.Height * 3));
            var cast = ray.Intersects(bplane);
            if (cast != null)
            {
                ray.Position += ray.Direction * (cast.Value + 0.01f);
                return new Vector2(ray.Position.X / 3, ray.Position.Z / 3);
            }

            return new Vector2(0, 0);
        }

        /// <summary>
        /// Gets the ID of the object at a given position.
        /// </summary>
        /// <param name="x">X position of object.</param>
        /// <param name="y">Y position of object.</param>
        /// <param name="gd">GraphicsDevice instance.</param>
        /// <returns>ID of object at position if found.</returns>
        public short GetObjectIDAtScreenPos(int x, int y, GraphicsDevice gd)
        {
            State._2D.Begin(this.State.Camera);
            return _2DWorld.GetObjectIDAtScreenPos(x, y, gd, State);
        }

         /// <summary>
        /// Gets an object group's thumbnail provided an array of objects.
        /// </summary>
        /// <param name="objects">The object components to draw.</param>
        /// <param name="gd">GraphicsDevice instance.</param>
        /// <param name="state">WorldState instance.</param>
        /// <returns>Object's ID if the object was found at the given position.</returns>
        public Texture2D GetObjectThumb(ObjectComponent[] objects, Vector3[] positions, GraphicsDevice gd)
        {
            State._2D.Begin(this.State.Camera);
            return _2DWorld.GetObjectThumb(objects, positions, gd, State);
        }

        public Texture2D GetLotThumb(GraphicsDevice gd)
        {
            State._2D.Begin(this.State.Camera);
            return _2DWorld.GetLotThumb(gd, State);
        }

        public virtual void ChangedWorldConfig(GraphicsDevice gd)
        {
            //destroy any features that are no longer enabled.

            var config = WorldConfig.Current;

            if (config.AdvancedLighting)
            {
                State.AmbientLight?.Dispose();
                State.AmbientLight = null;
                if (Light == null)
                {
                    Light = new LMapBatch(gd, 8);
                    if (Blueprint != null)
                    {
                        Light?.Init(Blueprint);
                        Blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.ROOM_CHANGED));
                        Blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.OUTDOORS_LIGHTING_CHANGED));
                    }
                    State.Light = Light;
                }
            } else
            {
                Light?.Dispose();
                Light = null;
                State.Light = null;
                if (State.AmbientLight == null)
                    State.AmbientLight = new Texture2D(gd, 256, 256);
                if (Blueprint != null) Blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.OUTDOORS_LIGHTING_CHANGED));
            }

            if (Blueprint != null && !FSOEnvironment.Enable3D)
            {
                var shad3D = (Blueprint.WCRC != null);
                if (config.Shadow3D != shad3D)
                {
                    if (config.AdvancedLighting && config.Shadow3D)
                    {
                        Blueprint.WCRC = new RC.WallComponentRC();
                        Blueprint.WCRC.blueprint = Blueprint;
                        Blueprint.WCRC.Generate(gd, State, false);
                    }
                    else
                    {
                        Blueprint.WCRC?.Dispose();
                        Blueprint.WCRC = null;
                    }
                    Blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.OUTDOORS_LIGHTING_CHANGED));
                }
            }
        }

        public virtual ObjectComponent MakeObjectComponent(Content.GameObject obj)
        {
            return new ObjectComponent(obj);
        }

        public virtual SubWorldComponent MakeSubWorld(GraphicsDevice gd)
        {
            return new SubWorldComponent(gd);
        }

        public virtual void InitSubWorlds()
        {

        }

        public override void Dispose()
        {
            base.Dispose();
            State.AmbientLight?.Dispose();
            Light?.Dispose();
            State.Rooms.Dispose();
            if (State._2D != null) State._2D.Dispose();
            if (_2DWorld != null) _2DWorld.Dispose();
            if (Blueprint != null)
            {
                foreach (var world in Blueprint.SubWorlds)
                {
                    world.Dispose();
                }
                Blueprint.Terrain?.Dispose();
                Blueprint.RoofComp?.Dispose();
            }
        }
    }
}
