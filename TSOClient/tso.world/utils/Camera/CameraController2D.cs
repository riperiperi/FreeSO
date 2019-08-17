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

        public CameraController2D(GraphicsDevice gd)
        {
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

        public void SetActive(ICameraController previous, World world)
        {
            //from 3d??
            if (previous is CameraController3D)
            {
                //set rotation based on 3d camera
                var cam3d = (CameraController3D)previous;
                //(float)Math.PI * (((int)cam.Rotation) / 2f - 0.25f);
                var rot = Math.Round((Common.Utils.DirectionUtils.PosMod(cam3d.RotationX, Math.PI * 2) / Math.PI + 0.25f) * 2) % 4;
                world.State.Rotation = (WorldRotation)rot;
                Camera.RotateOff = 0;
                RotationOffFrom = 0;
            }
        }

        public void SetRotation(WorldState state, WorldRotation rot)
        {
            var old = state.Rotation;
            state.SilentRotation = rot;

            if (state.RenderingThumbnail)
            {
                RotationOffFrom = 0;
                RotationOffPct = 0;
                Camera.RotateOff = 0;
            }
            else
            {
                RotationOffFrom = ((rot - old) * 90) + Camera.RotateOff;
                RotationOffFrom = ((RotationOffFrom + 540) % 360) - 180;
                Camera.RotateOff = RotationOffFrom;
                RotationOffPct = 0;
            }
        }

        private float RotationOffFrom;
        private float RotationOffPct;

        public void Update(UpdateState state, World world)
        {
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
