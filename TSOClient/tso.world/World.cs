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
using FSO.LotView.RC;
using System.Diagnostics;
using FSO.LotView.Platform;
using FSO.LotView.Utils;
using FSO.LotView.Utils.Camera;

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
            Effect e = WorldContent.Grad2DEffect;
            e = WorldContent.GrassEffect;
            e = WorldContent.Light2DEffect;
            e = WorldContent.ParticleEffect;
            e = WorldContent.RCObject;
            e = WorldContent.SSAA;
            e = WorldContent._2DWorldBatchEffect;
        }

        /** How many pixels from each edge of the screen before we start scrolling the view **/
        public int ScrollBounds = 20;
        public uint FrameCounter = 0;
        public uint LastCacheClear = 0;
        public static bool DirectX = false;
        public float Opacity = 1f;
        public float BackbufferScale = 1f;
        public bool ForceAdvLight;
        public bool LimitScroll = true;
        public IRCSurroundings Surroundings;

        public float SmoothZoomTimer = -1;
        public float SmoothZoomFrom = 1f;

        public WorldState State;
        public bool UseBackbuffer = true;
        protected bool HasInitGPU;
        protected bool HasInitBlueprint;
        protected bool HasInit;
        
        public WorldStatic Static;
        public WorldArchitecture Architecture;
        public WorldEntities Entities;
        public IWorldPlatform Platform;

        protected LMapBatch Light;
        protected Blueprint Blueprint;

        public event Action OnFullZoomOut;

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
            State = new WorldState(layer.Device, layer.Device.Viewport.Width, layer.Device.Viewport.Height, this);

            State._2D = new _2DWorldBatch(layer.Device, _2DWorldBatch.NUM_2D_BUFFERS,
                _2DWorldBatch.BUFFER_SURFACE_FORMATS, _2DWorldBatch.FORMAT_ALWAYS_DEPTHSTENCIL, _2DWorldBatch.SCROLL_BUFFER);

            Static = new WorldStatic(this);

            State.OutsidePx = new Texture2D(layer.Device, 1, 1);

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

            Blueprint?.Changes?.SetFlag(BlueprintGlobalChanges.ZOOM);
        }

        public virtual void InitDefaultGraphicsMode()
        {
            SetGraphicsMode(WorldConfig.Current.Mode);
        }

        public virtual void InitBlueprint(Blueprint blueprint)
        {
            this.Blueprint = blueprint;
            Platform?.Dispose();
            InitDefaultGraphicsMode();
            State.ProjectTilePos = EstTileAtPosWithScrollHeight;

            Entities = new WorldEntities(blueprint);
            Architecture = new WorldArchitecture(blueprint);
            Static?.InitBlueprint(blueprint);
            
            State.Changes = blueprint.Changes;
            GameThread.InUpdate(() =>
            {
                Light?.Init(blueprint);
                State.Rooms.Init(blueprint);
            });

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
            Blueprint.Changes.SetFlag(BlueprintGlobalChanges.ZOOM);

            State._2D?.ClearTextureCache();
        }

        public void InvalidatePreciseZoom()
        {
            if (Blueprint == null) { return; }
            Blueprint.Changes.SetFlag(BlueprintGlobalChanges.PRECISE_ZOOM);
        }

        public void InvalidateRotation()
        {
            if (Blueprint == null) { return; }

            foreach (var item in Blueprint.Objects)
            {
                item.OnRotationChanged(State);
            }
            foreach (var sub in Blueprint.SubWorlds) sub.State.Rotation = State.Rotation;
            Blueprint.Changes.SetFlag(BlueprintGlobalChanges.ROTATE);

            State._2D?.ClearTextureCache();
        }

        public void InvalidateScroll()
        {
            if (Blueprint == null) { return; }
            Blueprint.Changes.SetFlag(BlueprintGlobalChanges.SCROLL);
        }

        public void InvalidateFloor()
        {
            if (Blueprint == null) { return; }
            Blueprint.Changes.SetFlag(BlueprintGlobalChanges.LEVEL_CHANGED);
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

        public void SetGraphicsMode(GlobalGraphicsMode mode, bool instant = false)
        {
            var transTime = instant ? 0 : -1;
            switch (mode)
            {
                case GlobalGraphicsMode.Full2D:
                case GlobalGraphicsMode.Hybrid2D:
                    State.SetCameraType(this, Utils.Camera.CameraControllerType._2D, transTime);
                    Platform = new WorldPlatform2D(Blueprint);
                    break;
                case GlobalGraphicsMode.Full3D:
                    State.SetCameraType(this, Utils.Camera.CameraControllerType._3D, transTime);
                    Platform = new WorldPlatform3D(Blueprint);
                    State.Zoom = WorldZoom.Near;
                    break;
            }
            State.Platform = Platform;
        }

        public Tuple<float, float> Get3DTTHeights()
        {
            if (Blueprint == null) { return new Tuple<float, float>(0, 0); }
            var terrainHeight = (Blueprint.InterpAltitude(new Vector3(State.CenterTile, 0))) * 3;
            var targHeight = terrainHeight + (State.Level - 1) * 2.95f * 3;
            targHeight = Math.Max((Blueprint.InterpAltitude(new Vector3(State.Camera.Position.X, State.Camera.Position.Z, 0) / 3) + (State.Level - 1) * 2.95f) * 3, terrainHeight);
            return new Tuple<float, float>(terrainHeight, targHeight);
        }

        public virtual Vector2[] GetScrollBasis(bool multiplied)
        {
            if (State.CameraMode == CameraRenderMode._3D)
            {
                var cam = State.Cameras.ActiveCamera as CameraController3D;
                var mat = Matrix.CreateRotationZ(-(cam?.RotationX ?? 0));
                var z = multiplied ? ((1 + (float)Math.Sqrt(cam?.Zoom3D ?? 1)) / 2) : 1;
                return new Vector2[] {
                    Vector2.Transform(new Vector2(0, -1), mat) * z,
                    Vector2.Transform(new Vector2(1, 0), mat) * z
                };
            }
            else
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
        }

        public void InitiateSmoothZoom(WorldZoom zoom)
        {
            //TODO: disable in 3d
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

            if (State.CameraMode < CameraRenderMode._3D)
            {
                State.Cameras.WithTransitionsDisabled(() =>
                {
                    State.CenterTile = State.Project2DCenterTile(pelvisCenter);
                    State.Camera2D.RotationAnchor = pelvisCenter;
                });
            } else {
                State.CenterTile = new Vector2(pelvisCenter.X, pelvisCenter.Y);
            }
            if (State.Level != comp.Level) State.Level = comp.Level;
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
            State.FramesSinceLastDraw++;

            if (Blueprint != null)
            {
                if (!Content.Content.Get().TS1) Blueprint.Weather?.Update();
                var partiCopy = new List<ParticleComponent>(Blueprint.Particles);
                foreach (var particle in partiCopy)
                {
                    particle.Update(null, State);
                }

                partiCopy = new List<ParticleComponent>(Blueprint.ObjectParticles);
                foreach (var particle in partiCopy)
                {
                    particle.Update(null, State);
                }
            }

            if (State.ScrollAnchor != null)
            {
                CenterTo(State.ScrollAnchor);
            }

            State.Cameras.Update(state, this);
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

            if (state.WindowFocused && Visible)
            {
                if (state.NewKeys.Contains(Microsoft.Xna.Framework.Input.Keys.F12))
                {
                    WorldConfig.Current.Mode = (State.CameraMode == CameraRenderMode._3D) ? GlobalGraphicsMode.Hybrid2D : GlobalGraphicsMode.Full3D;
                    SetGraphicsMode(WorldConfig.Current.Mode);
                }
                if (state.NewKeys.Contains(Microsoft.Xna.Framework.Input.Keys.Tab))
                {
                    if (State.Cameras.ActiveType == Utils.Camera.CameraControllerType.FirstPerson)
                    {
                        SetGraphicsMode(WorldConfig.Current.Mode, false);
                    }
                    else
                    {
                        State.SetCameraType(this, Utils.Camera.CameraControllerType.FirstPerson, 0);
                    }
                }
            }
        }

        protected void BoundView()
        {
            if (!LimitScroll) return;
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
            State.UpdateInterpolation();
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
            State._2D.Begin(this.State.Camera2D);
            Blueprint.Changes.PreDraw(device, State);
            Static?.PreDraw(device, State);

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
            {
                PPXDepthEngine.WithOpacity = State.CameraMode < CameraRenderMode._3D;
                PPXDepthEngine.DrawBackbuffer(Opacity, BackbufferScale);
            }
            return;
        }

        protected virtual void InternalDraw(GraphicsDevice device)
        {
            device.RasterizerState = RasterizerState.CullNone;
            State.PrepareLighting();
            State._2D.OutputDepth = true;
            
            State._2D.Begin(this.State.Camera2D);

            //State._2D.PreciseZoom = State.PreciseZoom;
            State._2D.ResetMatrices(device.Viewport.Width, device.Viewport.Height);

            device.DepthStencilState = DepthStencilState.Default;
            if (State.CameraMode == CameraRenderMode._3D) Static?.DrawBg(State.Device, State, SkyBounds, false);
            Architecture.Draw2D(device, State);
            Static?.Draw(State);
            State.PrepareCamera();
            Entities.DrawAvatars(device, State);
            Entities.Draw(device, State);

            State._2D.OutputDepth = false;
        }

        public void Force2DPredraw(GraphicsDevice device)
        {
            Static.PreDraw(device, State);
        }

        public float? BoxRC2(Ray ray, float tileSize)
        {
            var px = (ray.Direction.X > 0);
            var py = (ray.Direction.Z > 0);
            //find current tile
            int x = (!px) ? (int)Math.Ceiling(ray.Position.X / tileSize) :
                           (int)(ray.Position.X / tileSize);
            int y = (!py) ? (int)Math.Ceiling(ray.Position.Z / tileSize) :
                           (int)(ray.Position.Z / tileSize);

            //find next tile boundary
            float nx = ((px) ? (x + 1) : (x - 1)) * 3;
            float ny = ((py) ? (y + 1) : (y - 1)) * 3;

            const float Epsilon = 1e-6f;
            float? min = null;
            if (Math.Abs(ray.Direction.X) > Epsilon)
            {
                min = (nx - ray.Position.X) / ray.Direction.X;
            }

            if (Math.Abs(ray.Direction.Z) > Epsilon)
            {
                var min2 = (ny - ray.Position.Z) / ray.Direction.Z;
                if (min == null || min.Value > min2) min = min2;
            }
            return min;
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
                if (tMin <= 0 || (ray.Direction.X >= 0 && tMin == 0)) tMin = tMax;
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
                if (tMinZ < 0 || (ray.Direction.Z >= 0 && tMinZ == 0)) tMinZ = tMaxZ;

                if (!tMin.HasValue || tMin > tMinZ) tMin = tMinZ;
                if (!tMax.HasValue || tMaxZ > tMax) tMax = tMaxZ;
            }

            // a negative tMin means that the intersection point is behind the ray's origin
            // we discard these as not hitting the AABB
            if (tMin < 0) return null;

            return tMin;
        }

        public Vector2 EstTileAtPosWithScroll(Vector2 pos, sbyte level = -1)
        {
            if (level == -1) level = State.Level;
            var ray = State.CameraRayAtScreenPos(pos, level);

            var baseBox = new BoundingBox(new Vector3(0, -5000, 0), new Vector3(Blueprint.Width * 3, 5000, Blueprint.Height * 3));
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

            var px = (ray.Direction.X > 0);
            var py = (ray.Direction.Z > 0);

            var canProj = Blueprint?.Altitude != null;

            int iteration = 0;
            while (mx >= 0 && mx < Blueprint.Width && my >= 0 && my < Blueprint.Width && canProj)
            {
                //test triangle 1. (centre of tile down xz, we lean towards positive x)
                var plane = new Plane(
                    new Vector3(mx * 3, Blueprint.GetAltPoint(mx, my) * Blueprint.TerrainFactor * 3, my * 3),
                    new Vector3(mx * 3 + 3, Blueprint.GetAltPoint(mx + 1, my) * Blueprint.TerrainFactor * 3, my * 3),
                    new Vector3(mx * 3 + 3, Blueprint.GetAltPoint(mx + 1, my + 1) * Blueprint.TerrainFactor * 3, my * 3 + 3)
                    );
                var tBounds = new BoundingBox(new Vector3(mx * 3, -5000, my * 3), new Vector3(mx * 3 + 3, 5000, my * 3 + 3));

                var t1 = ray.Intersects(plane);
                var t2 = BoxRC2(ray, 3);
                //var t2 = BoxRC(ray, tBounds);
                if (plane.DotCoordinate(ray.Position) > 0) t1 = 0;
                if (t1 != null && t2 != null && t1.Value < t2.Value)
                {
                    //hit the ground...
                    var tentative = ray.Position + ray.Direction * (t1.Value + 0.00001f);

                    //did it hit the correct side of the triangle?
                    var mySide = ((tentative.X / 3) % 1) - ((tentative.Z / 3) % 1);
                    if (mySide >= 0)
                    {
                        return new Vector2(tentative.X / 3, tentative.Z / 3);
                    }
                    else
                    {
                        //test the other side (positive z)
                        plane = new Plane(
                            new Vector3(mx * 3, Blueprint.GetAltPoint(mx, my) * Blueprint.TerrainFactor * 3, my * 3),
                            new Vector3(mx * 3, Blueprint.GetAltPoint(mx, my + 1) * Blueprint.TerrainFactor * 3, my * 3 + 3),
                            new Vector3(mx * 3 + 3, Blueprint.GetAltPoint(mx + 1, my + 1) * Blueprint.TerrainFactor * 3, my * 3 + 3)
                            );
                        t1 = ray.Intersects(plane);
                        if (t1 != null && t2 != null && t1.Value < t2.Value)
                        {
                            //hit the other side
                            tentative = ray.Position + ray.Direction * (t1.Value + 0.00001f);
                            return new Vector2(tentative.X / 3, tentative.Z / 3);
                        }
                    }
                }
                if (t2 == null) break;
                ray.Position += ray.Direction * (t2.Value + 0.00001f);

                mx = (!px) ? ((int)Math.Ceiling(ray.Position.X / 3) - 1) :
                               (int)(ray.Position.X / 3);
                my = (!py) ? ((int)Math.Ceiling(ray.Position.Z / 3) - 1) :
                               (int)(ray.Position.Z / 3);

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

        public Vector3 EstTileAtPosWithScroll3D(Vector2 pos, sbyte startFloor = -1)
        {
            if (startFloor == -1) startFloor = State.Level;
            for (sbyte floor = startFloor; floor > 0; floor--)
            {
                var result = EstTileAtPosWithScroll(pos, floor);
                if (floor == 1 || (Blueprint.TileInbounds(result) && Blueprint.GetFloor((short)result.X, (short)result.Y, floor).Pattern != 0))
                {
                    return new Vector3(result, floor);
                }
            }
            return new Vector3(EstTileAtPosWithScroll(pos), State.Level);
        }

        public Vector3 EstTileAtPosWithScrollHeight(Vector2 pos, sbyte startFloor = -1)
        {
            var result = EstTileAtPosWithScroll3D(pos, startFloor);
            result.Z = Blueprint.InterpAltitude(result) + (result.Z-1) * 2.95f;
            return result;
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
            State._2D.Begin(this.State.Camera2D);
            return Platform.GetObjectIDAtScreenPos(x, y, gd, State);
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
            State._2D.Begin(this.State.Camera2D);
            return Platform.GetObjectThumb(objects, positions, gd, State);
        }

        public Texture2D GetLotThumb(GraphicsDevice gd, Action<Texture2D> rooflessCallback)
        {
            State._2D.Begin(this.State.Camera2D);
            return Platform.GetLotThumb(gd, State, rooflessCallback);
        }

        public virtual void ChangedWorldConfig(GraphicsDevice gd)
        {
            //destroy any features that are no longer enabled.

            var config = WorldConfig.Current;
            if (ForceAdvLight)
            {
                config.LightingMode = Math.Max(config.LightingMode, 1);
            }

            if (config.AdvancedLighting)
            {
                State.AmbientLight?.Dispose();
                State.AmbientLight = null;
                Light?.Dispose();
                Light = null;
                if (Light == null)
                {
                    Light = new LMapBatch(gd, 16);
                    if (Blueprint != null)
                    {
                        Light?.Init(Blueprint);
                        Blueprint.Changes.SetFlag(BlueprintGlobalChanges.ROOM_CHANGED);
                        Blueprint.Changes.SetFlag(BlueprintGlobalChanges.OUTDOORS_LIGHTING_CHANGED);
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
                if (Blueprint != null) Blueprint.Changes.SetFlag(BlueprintGlobalChanges.OUTDOORS_LIGHTING_CHANGED);
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
                    Blueprint.Changes.SetFlag(BlueprintGlobalChanges.OUTDOORS_LIGHTING_CHANGED);
                }
            }

            var lastm = PPXDepthEngine.MSAA;
            var lasts = PPXDepthEngine.SSAA;
            PPXDepthEngine.SSAAFunc = SSAADownsample.Draw;
            switch (WorldConfig.Current.AA)
            {
                case 0:
                    PPXDepthEngine.MSAA = 0;
                    PPXDepthEngine.SSAA = 1;
                    break;
                case 1:
                    PPXDepthEngine.MSAA = 4;
                    PPXDepthEngine.SSAA = 1;
                    break;
                case 2:
                    PPXDepthEngine.MSAA = 0;
                    PPXDepthEngine.SSAA = 2;
                    break;
            }
            if (lastm != PPXDepthEngine.MSAA || lasts != PPXDepthEngine.SSAA) PPXDepthEngine.InitScreenTargets();
        }

        public virtual ObjectComponent MakeObjectComponent(Content.GameObject obj)
        {
            return new ObjectComponent(obj);
        }

        public virtual SubWorldComponent MakeSubWorld(GraphicsDevice gd)
        {
            return new SubWorldComponent(gd);
        }

        public BoundingBox[] SkyBounds;

        public virtual void InitSubWorlds()
        {
            float minAlt = 0;
            foreach (var height in Blueprint.Altitude)
            {
                var alt = height * Blueprint.TerrainFactor - Blueprint.BaseAlt;
                if (alt < minAlt)
                {
                    minAlt = alt;
                }
            }

            BoundingBox overall = new BoundingBox(new Vector3(0, minAlt, 0), new Vector3(Blueprint.Width * 3, 1000, Blueprint.Height * 3));
            foreach (var world in Blueprint.SubWorlds)
            {
                world.UpdateBounds();
                overall = BoundingBox.CreateMerged(overall, world.Bounds);
            }
            //update sky bounding box edge

            SkyBounds = new BoundingBox[4];
            SkyBounds[0] = new BoundingBox(new Vector3(overall.Min.X - 1, overall.Min.Y, overall.Min.Z), new Vector3(overall.Min.X, overall.Max.Y, overall.Max.Z));
            SkyBounds[1] = new BoundingBox(new Vector3(overall.Min.X, overall.Min.Y, overall.Min.Z - 1), new Vector3(overall.Max.X, overall.Max.Y, overall.Min.Z));
            SkyBounds[2] = new BoundingBox(new Vector3(overall.Min.X, overall.Min.Y, overall.Max.Z), new Vector3(overall.Max.X, overall.Max.Y, overall.Max.Z + 1));
            SkyBounds[3] = new BoundingBox(new Vector3(overall.Max.X, overall.Min.Y, overall.Min.Z), new Vector3(overall.Max.X + 1, overall.Max.Y, overall.Max.Z));
        }

        public int PreloadProgress;
        public int PreloadObjProgress;

        public bool Preload(GraphicsDevice gd)
        {
            var watch = new Stopwatch();
            watch.Start();

            if (PreloadProgress == 0) {
                var done = 0;
                for (int i = PreloadObjProgress; i < Blueprint.Objects.Count; i++)
                {
                    var obj = Blueprint.Objects[i];
                    obj.Preload(gd, State);
                    PreloadObjProgress++;
                    if (watch.ElapsedMilliseconds > 16 && done >= 6)
                    {
                        watch.Stop();
                        return false;
                    }
                    done++;
                }

                for (int i=0; i<Blueprint.Avatars.Count; i++)
                {
                    Blueprint.Avatars[i].Preload(gd, State);
                }

                PreloadProgress = 1;
                PreloadObjProgress = 0;
            }

            for (int i= PreloadProgress-1; i<Blueprint.SubWorlds.Count; i++)
            {
                var world = Blueprint.SubWorlds[i];
                for (int j = PreloadObjProgress; j < world.Blueprint.Objects.Count; j++)
                {
                    var obj = world.Blueprint.Objects[j];
                    obj.Preload(gd, State);
                    PreloadObjProgress++;
                    if (watch.ElapsedMilliseconds > 16)
                    {
                        watch.Stop();
                        return false;
                    }
                }

                PreloadProgress++;
                PreloadObjProgress = 0;
            }

            return true;
        }

        public override void Dispose()
        {
            base.Dispose();
            State.AmbientLight?.Dispose();
            State.OutsidePx.Dispose();
            Light?.Dispose();
            Platform?.Dispose();
            State.Rooms.Dispose();
            if (State._2D != null) State._2D.Dispose();
            if (Blueprint != null)
            {
                foreach (var world in Blueprint.SubWorlds)
                {
                    world.Dispose();
                }
                foreach (var obj in Blueprint.Objects)
                {
                    obj.Dispose();
                }
                foreach (var particle in Blueprint.Particles)
                {
                    particle.Dispose();
                }
                foreach (var particle in Blueprint.ObjectParticles)
                {
                    particle.Dispose();
                }
                Blueprint.Terrain?.Dispose();
                Blueprint.RoofComp?.Dispose();
            }
        }
    }
}
