using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Common;
using FSO.Common.Rendering.Framework.Camera;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using Microsoft.Xna.Framework;

namespace FSO.LotView.Utils.Camera
{
    public class CameraController3D : ICameraController
    {
        public WorldCamera3D Camera;
        private bool MouseWasDown;
        private Point LastMouse;
        protected float CamHeight;

        public bool UseZoomHold => true;
        public bool UseRotateHold => true;
        public ICamera BaseCamera => Camera;

        public float RotationX
        {
            get { return _RotationX; }
            set { _RotationX = value; InvalidateCamera(); }
        }
        
        public float RotationY
        {
            get { return _RotationY; }
            set { _RotationY = (float)Math.Min(Math.PI, Math.Max(0, value)); InvalidateCamera(); }
        }
        
        public float Zoom3D
        {
            get { return _Zoom3D; }
            set { value = Math.Min(100, Math.Max(0, value)); _Zoom3D = value; InvalidateCamera(); }
        }

        public void SetDimensions(Vector2 dim)
        {
            Camera.ProjectionOrigin = dim / 2;
        }

        public virtual void InvalidateCamera(WorldState state)
        {
            var baseHeight = CamHeight;
                Camera.Target = new Vector3(CenterTile.X * WorldSpace.WorldUnitsPerTile, baseHeight + 3, CenterTile.Y * WorldSpace.WorldUnitsPerTile);
                Camera.Position = Camera.Target + ComputeCenterRelative();
        }

        public Vector3 ComputeCenterRelative()
        {
            var mat = Matrix.CreateRotationY(_RotationX);

            var rotY = (float)((1 - Math.Cos(_RotationY)) * Math.PI * 0.245f);

            var panMat = Matrix.CreateRotationZ(rotY);
            var panMatFar = Matrix.CreateRotationZ(rotY / 2);
            var z = Zoom3D * Zoom3D;

            return Vector3.Transform(
                Vector3.Transform(new Vector3(10, 0, 0), panMat) +
                Vector3.Transform(new Vector3(z * 1.30f, z * 1f, 0), panMatFar)
                , mat);

        }

        public void RotateHold(float intensity)
        {
            throw new NotImplementedException();
        }

        public void RotatePress(float intensity)
        {
            throw new NotImplementedException();
        }

        public virtual void SetActive(ICameraController previous, World world)
        {
            if (previous is CameraControllerFP)
            {
                var fp = (CameraControllerFP)previous;
                _RotationY = SavedYRot;
                var relative = ComputeCenterRelative();
                CenterTile -= new Vector2(relative.X / WorldSpace.WorldUnitsPerTile, relative.Z / WorldSpace.WorldUnitsPerTile);

                _CamHeight -= relative.Y - FPCamHeight;
            }
            else if (previous is CameraController2D)
            {
                //just guess camera zoom and rotation?
            }
        }

    protected float CorrectCameraHeight(World world)
        {
            var tt = world.Get3DTTHeights();
            CamHeight += (tt.Item2 - CamHeight) * (1f - (float)Math.Pow(0.8f, 60f / FSOEnvironment.RefreshRate));
            return tt.Item1;
        }

        public virtual void Update(UpdateState state, World world)
        {
            var md = state.MouseState.MiddleButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
            CorrectCameraHeight(world);

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

        public void ZoomHold(float intensity)
        {
            throw new NotImplementedException();
        }

        public void ZoomPress(float intensity)
        {
            throw new NotImplementedException();
        }
    }
}
