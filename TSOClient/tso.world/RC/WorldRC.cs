using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using FSO.Common.Rendering.Framework.Camera;
using FSO.Common.Rendering.Framework;
using FSO.Common;
using FSO.Common.Utils;
using Microsoft.Xna.Framework;
using FSO.Common.Rendering.Framework.Model;
using FSO.LotView.Components;
using Microsoft.Xna.Framework.Input;
using FSO.LotView.Utils;
using FSO.LotView.Facade;

namespace FSO.LotView.RC
{
    /// <summary>
    /// An alternate implenentation of World that renders the game with a 3D camera.
    /// 
    /// RC stands for reconstruction, the primary method used to render game objects in 3D.
    /// </summary>
    public class WorldRC : World
    {
        public WorldRC(GraphicsDevice Device) : base(Device)
        {
            UseBackbuffer = true;
            _2DWorld = new World2DRC();
            PPXDepthEngine.SSAAFunc = SSAADownsample.Draw;
        }

        public bool MouseWasDown;
        public Point LastMouse;
        public Vector3 FPCamVelocity;
        private bool LastFP;

        public override void Update(UpdateState state)
        {
            base.Update(state);
            var rcState = ((WorldStateRC)State);

            float terrainHeight = 0;
            var hz = FSOEnvironment.RefreshRate;
            if (Blueprint != null)
            {
                terrainHeight = (Blueprint.InterpAltitude(new Vector3(State.CenterTile, 0))) * 3;
                var targHeight = terrainHeight + (State.Level - 1) * 2.95f * 3;
                targHeight = Math.Max((Blueprint.InterpAltitude(new Vector3(State.Camera.Position.X, State.Camera.Position.Z, 0)/3) + (State.Level - 1) * 2.95f) * 3, terrainHeight);
                rcState.CamHeight += (targHeight - rcState.CamHeight) * (1f-(float)Math.Pow(0.8f, 60f/hz));
            }

            if (Visible && !rcState.FixedCam)
            {

                if (state.NewKeys.Contains(Microsoft.Xna.Framework.Input.Keys.Tab))
                {
                    rcState.CameraMode = !rcState.CameraMode;
                    ((WorldCamera3D)rcState.Camera).FOV = (float)Math.PI / 4f;
                }

                if (rcState.CameraMode)
                {
                    if (state.WindowFocused)
                    {
                        var mx = (int)State.WorldSpace.WorldPxWidth / (2 * PPXDepthEngine.SSAA);
                        var my = (int)State.WorldSpace.WorldPxHeight / (2 * PPXDepthEngine.SSAA);

                        var mpos = state.MouseState.Position;
                        var camera = rcState.Camera3D;
                        if (LastFP)
                        {
                            rcState.RotationX -= ((mpos.X - mx) / 500f) * camera.FOV;
                            rcState.RotationY += ((mpos.Y - my) / 500f) * camera.FOV;
                        }
                        Mouse.SetPosition(mx, my);

                        var speed = (state.KeyboardState.IsKeyDown(Keys.LeftShift)) ? 1.5f : 0.5f;
                        
                        if (camera.FOV < Math.PI * 0.6f)
                            if (state.KeyboardState.IsKeyDown(Keys.Z))
                                camera.FOV += 1f/hz;

                        if (camera.FOV > Math.PI * 0.025f)
                            if (state.KeyboardState.IsKeyDown(Keys.X))
                                camera.FOV -= 1f/hz;

                        if (state.KeyboardState.IsKeyDown(Keys.W))
                            FPCamVelocity.Z -= speed;
                        if (state.KeyboardState.IsKeyDown(Keys.S))
                            FPCamVelocity.Z += speed;
                        if (state.KeyboardState.IsKeyDown(Keys.A))
                            FPCamVelocity.X -= speed;
                        if (state.KeyboardState.IsKeyDown(Keys.D))
                            FPCamVelocity.X += speed;
                        if (state.KeyboardState.IsKeyDown(Keys.Q))
                            FPCamVelocity.Y -= speed;
                        if (state.KeyboardState.IsKeyDown(Keys.E))
                            FPCamVelocity.Y += speed;
                        LastFP = true;
                    }
                    else
                    {
                        LastFP = false;
                    }

                    Scroll(new Vector2(FPCamVelocity.X / FSOEnvironment.RefreshRate, FPCamVelocity.Z / FSOEnvironment.RefreshRate));
                    rcState.FPCamHeight = Math.Max((terrainHeight - rcState.CamHeight) - 2, rcState.FPCamHeight + (FPCamVelocity.Y * 3) / FSOEnvironment.RefreshRate);
                    for (int i = 0; i < FSOEnvironment.RefreshRate / 60; i++)
                        FPCamVelocity *= 0.9f;
                }
                else if (Visible)
                {
                    LastFP = false;
                    var md = state.MouseState.MiddleButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;

                    if (MouseWasDown)
                    {
                        var mpos = state.MouseState.Position;
                        rcState.RotationX += (mpos.X - LastMouse.X) / 250f;
                        rcState.RotationY += (mpos.Y - LastMouse.Y) / 150f;
                    }

                    if (md)
                    {
                        LastMouse = state.MouseState.Position;
                    }
                    MouseWasDown = md;
                }
            }
        }

        public void SetSurroundingWorld(IRCSurroundings surround)
        {
            ((World2DRC)_2DWorld).Surroundings = surround;
        }

        public override Vector2[] GetScrollBasis(bool multiplied)
        {
            var mat = Matrix.CreateRotationZ(-((WorldStateRC)State).RotationX);
            var z = multiplied ? ((1 + (float)Math.Sqrt(((WorldStateRC)State).Zoom3D)) / 2):1;
            return new Vector2[] {
                Vector2.Transform(new Vector2(0, -1), mat) * z,
                Vector2.Transform(new Vector2(1, 0), mat) * z
            };
        }

        public override void Initialize(_3DLayer layer)
        {
            Parent = layer;

            /**
             * Setup world state, this object acts as a facade
             * to world objects as well as providing various
             * state settings for the world and helper functions
             */
            State = new WorldStateRC(layer.Device, layer.Device.Viewport.Width / FSOEnvironment.DPIScaleFactor, layer.Device.Viewport.Height / FSOEnvironment.DPIScaleFactor, this);

            State._3D = new FSO.LotView.Utils._3DWorldBatch(State);
            State._2D = new FSO.LotView.Utils._2DWorldBatch(layer.Device, 0, new SurfaceFormat[0], new bool[0], World2D.SCROLL_BUFFER);
            State.OutsidePx = new Texture2D(layer.Device, 1, 1);

            PPXDepthEngine.InitGD(layer.Device);
            ChangedWorldConfig(layer.Device);

            base.Camera = State.Camera;

            HasInitGPU = true;
            HasInit = HasInitGPU & HasInitBlueprint;
        }

        public override void PreDraw(GraphicsDevice device)
        {
            if (HasInit == false) { return; }

            var boundfactor = 0.5f;
            boundfactor *= Blueprint?.Width ?? 64;
            var off = 0.5f * (Blueprint?.Width ?? 64);
            var tile = State.CenterTile;
            tile = new Vector2(Math.Min(boundfactor + off, Math.Max(off - boundfactor, tile.X)), Math.Min(boundfactor + off, Math.Max(off - boundfactor, tile.Y)));
            if (tile != State.CenterTile) State.CenterTile = tile;

            State._2D.PreciseZoom = State.PreciseZoom;
            State.OutsideColor = Blueprint.OutsideColor;
            FSO.Common.Rendering.Framework.GameScreen.ClearColor = new Color(State.OutsideColor.ToVector4()); //new Color(0x72, 0x72, 0x72).ToVector4() * 

            var frustrum = new BoundingFrustum(State.Camera.View * State.Camera.Projection);
            foreach (var sub in Blueprint.SubWorlds)
            {
                var bounds = ((SubWorldComponentRC)sub).Bounds;
                if (bounds.Intersects(frustrum))
                    sub.PreDraw(device, State);
            }
            State.UpdateInterpolation();
            if (Blueprint != null)
            {
                foreach (var ent in Blueprint.Objects)
                {
                    ent.Update(null, State);
                }
            }

            //For all the tiles in the dirty list, re-render them
            State.PrepareLighting();
            _2DWorld.PreDraw(device, State);
            device.SetRenderTarget(null);

            State._3D.Begin(device);
            _3DWorld.PreDraw(device, State);
            State._3D.End();

            if (UseBackbuffer)
            {

                PPXDepthEngine.SetPPXTarget(null, null, true, State.OutsideColor);
                InternalDraw(device);
                device.SetRenderTarget(null);
            }
            return;
        }


        protected override void InternalDraw(GraphicsDevice device)
        {
            State.PrepareLighting();
            State.ThisFrameImmediate = true;
            State._2D.OutputDepth = true;

            State._3D.Begin(device);

            var pxOffset = -State.WorldSpace.GetScreenOffset();
            //State._2D.ResetMatrices(device.Viewport.Width, device.Viewport.Height);
            ((World2DRC)_2DWorld).DrawBg(device, State, SkyBounds, (Opacity < 1));
            _3DWorld.DrawBefore2D(device, State);
            _3DWorld.DrawAfter2D(device, State);
            State._3D.End();

            _2DWorld.Draw(device, State);

            foreach (var particle in Blueprint.Particles)
            {
                particle.Draw(device, State);
            }

            foreach (var debug in Blueprint.DebugLines)
            {
                debug.Draw(device, State);
            }
        }

        public override ObjectComponent MakeObjectComponent(Content.GameObject obj)
        {
            return new ObjectComponentRC(obj);
        }

        public override SubWorldComponent MakeSubWorld(GraphicsDevice gd)
        {
            return new SubWorldComponentRC(gd);
        }

        public override void ChangedWorldConfig(GraphicsDevice gd)
        {
            base.ChangedWorldConfig(gd);
            switch (WorldConfig.Current.AA)
            {
                case 1:
                    PPXDepthEngine.MSAA = 4;
                    PPXDepthEngine.SSAA = 1;
                    break;
                case 2:
                    PPXDepthEngine.SSAA = 2;
                    PPXDepthEngine.MSAA = 0;
                    break;
                default:
                    PPXDepthEngine.MSAA = 0;
                    PPXDepthEngine.SSAA = 1;
                    break;
            }
            PPXDepthEngine.InitScreenTargets();
            UseBackbuffer = WorldConfig.Current.AA > 0;
        }

        public BoundingBox[] SkyBounds;

        public override void InitSubWorlds()
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
                ((SubWorldComponentRC)world).UpdateBounds();
                overall = BoundingBox.CreateMerged(overall, ((SubWorldComponentRC)world).Bounds);
            }
            //update sky bounding box edge

            SkyBounds = new BoundingBox[4];
            SkyBounds[0] = new BoundingBox(new Vector3(overall.Min.X-1, overall.Min.Y, overall.Min.Z), new Vector3(overall.Min.X, overall.Max.Y, overall.Max.Z));
            SkyBounds[1] = new BoundingBox(new Vector3(overall.Min.X, overall.Min.Y, overall.Min.Z-1), new Vector3(overall.Max.X, overall.Max.Y, overall.Min.Z));
            SkyBounds[2] = new BoundingBox(new Vector3(overall.Min.X, overall.Min.Y, overall.Max.Z), new Vector3(overall.Max.X, overall.Max.Y, overall.Max.Z+1));
            SkyBounds[3] = new BoundingBox(new Vector3(overall.Max.X, overall.Min.Y, overall.Min.Z), new Vector3(overall.Max.X+1, overall.Max.Y, overall.Max.Z));
        }
    }
}
