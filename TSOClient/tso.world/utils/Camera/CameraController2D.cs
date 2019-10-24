using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Common;
using FSO.Common.Rendering.Framework.Camera;
using FSO.Common.Rendering.Framework.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.LotView.Utils.Camera
{
    public class CameraController2D : ICameraController
    {
        public WorldCamera Camera;
        public ICamera BaseCamera => Camera;
        public bool UseZoomHold => false;
        public bool UseRotateHold => false;
        public WorldRotation CutRotation => Camera.Rotation;

        private GraphicsDevice GD;

        public CameraController2D(GraphicsDevice gd)
        {
            GD = gd;
            Camera = new WorldCamera(gd);
        }

        public void InvalidateCamera(WorldState state)
        {
            var ctr = state.WorldSpace.GetScreenFromTile(state.CenterTile);
            ctr.X = (float)Math.Round(ctr.X);
            ctr.Y = (float)Math.Round(ctr.Y);
            var test = new Vector2(-0.5f, 0);
            test *= 1 << (3 - (int)state.Zoom);
            var back = state.WorldSpace.GetTileFromScreen(ctr + test);
            //Camera.RotationAnchor = null;
            Camera.CenterTile = new Vector3(back, 0);
            Camera.Zoom = state.Zoom;
            Camera.Rotation = state.Rotation;
            Camera.PreciseZoom = state.PreciseZoom;
        }

        public void SetDimensions(Vector2 dim)
        {
            Camera.ViewDimensions = dim;
        }

        public void RotateHold(float intensity)
        {
            return;
        }

        public void RotatePress(float intensity)
        {
            throw new NotImplementedException();
        }

        public Vector2 SwitchCenter;

        public virtual ICameraController BeforeActive(ICameraController previous, World world)
        {
            if (previous is CameraControllerFP)
            {
                //convert to 3d then to 2d.
                var c3d = new CameraController3D(GD, world.State);
                c3d.BeforeActive(previous, world);
                previous = c3d;
            }
            if (previous is CameraController3D)
            {
                //set rotation based on 3d camera
                var cam3d = (CameraController3D)previous;
                var target = cam3d.Camera.Target / WorldSpace.WorldUnitsPerTile;
                SwitchCenter = world.State.Project2DCenterTile(new Vector3(target.X, target.Z, target.Y));
            }
            return previous;
        }

        public void OnActive(ICameraController previous, World world)
        {
            
            if (previous is CameraController3D)
            {
                //set rotation based on 3d camera
                var cam3d = (CameraController3D)previous;
                var target = cam3d.Camera.Target / WorldSpace.WorldUnitsPerTile;
                world.State.CenterTile = SwitchCenter;
                var rot = Math.Round((Common.Utils.DirectionUtils.PosMod(cam3d.RotationX, Math.PI * 2) / Math.PI + 0.25f) * 2) % 4;
                world.State.Rotation = (WorldRotation)rot;
                Camera.RotateOff = 0;
                RotationOffFrom = 0;
            }
        }

        private Vector3 ReprojectCenterTile(WorldState state)
        {
            var center = new Vector3(state.CenterTile, 0);
            state.Cameras.WithTransitionsDisabled(() =>
            {
                if (state.ProjectTilePos != null) center = state.ProjectTilePos(state.WorldSpace.WorldPx / 2);
            });
            Camera.RotationAnchor = center;
            return center;
        }

        public void SetRotation(WorldState state, WorldRotation rot)
        {
            var old = state.Rotation;
            var oldRot = Camera.RotateOff;
            Camera.RotateOff = 0;

            //reproject into new 2d space
            var center = (state.ScrollAnchor != null) ? (Camera.RotationAnchor ?? ReprojectCenterTile(state)) : ReprojectCenterTile(state);

            state.SilentRotation = rot;
            InvalidateCamera(state);

            state.Cameras.WithTransitionsDisabled(() =>
            {
                //project 3d back into 2d center tile
                state.CenterTile = state.Project2DCenterTile(center);
            });

            if (state.DisableSmoothRotation)
            {
                RotationOffFrom = 0;
                RotationOffPct = 0;
                Camera.RotateOff = 0;
            }
            else
            {
                RotationOffFrom = ((rot - old) * 90) + oldRot;
                RotationOffFrom = ((RotationOffFrom + 540) % 360) - 180;
                Camera.RotateOff = RotationOffFrom;
                RotationOffPct = 0;
            }
        }

        private float RotationOffFrom;
        private float RotationOffPct;
        private bool MouseWasDown;
        private Vector2? LastScrollPos;
        private Point LastMouse;

        public void Update(UpdateState state, World world)
        {
            var wstate = world.State;
            var md = state.MouseState.MiddleButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;

            if (!wstate.DisableSmoothRotation)
            {
                if (md)
                {
                    var mpos = state.MouseState.Position;
                    if (wstate.ScrollAnchor == null && LastScrollPos != wstate.CenterTile)
                    {
                        ReprojectCenterTile(wstate);
                    }
                    if (MouseWasDown)
                    {
                        //rotate relative to last mouse position
                        Camera.RotateOff -= ((mpos.X - LastMouse.X) / 250f) * 180 / (float)Math.PI;
                    }
                    LastScrollPos = wstate.CenterTile;
                    LastMouse = mpos;
                }
                else
                {
                    if (MouseWasDown)
                    {
                        //end middle mouse scroll. decide which rotation to settle on
                        var targRot = (int)Common.Utils.DirectionUtils.PosMod(((int)Camera.Rotation - (int)Math.Round(Camera.RotateOff / 90)), 4);
                        world.State.Rotation = (WorldRotation)targRot;
                        LastScrollPos = null;
                    }
                    if (RotationOffFrom != 0)
                    {
                        RotationOffPct += 3f / FSOEnvironment.RefreshRate;
                        if (RotationOffPct > 1)
                        {
                            RotationOffFrom = 0;
                            RotationOffPct = 0;
                        }

                        Camera.RotateOff = RotationOffFrom * (float)(Math.Cos((RotationOffPct) * Math.PI) + 1) / 2;
                    }
                }
            }
            MouseWasDown = md;
        }

        public void ZoomHold(float intensity)
        {
            return;
        }

        public void ZoomPress(float intensity)
        {
            throw new NotImplementedException();
        }
    }
}
