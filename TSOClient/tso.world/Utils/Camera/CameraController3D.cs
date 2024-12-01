using System;
using FSO.Common;
using FSO.Common.Rendering.Framework.Camera;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using FSO.LotView.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.LotView.Utils.Camera
{
    public class CameraController3D : ICameraController, I3DRotate
    {
        public WorldCamera3D Camera;
        private bool MouseWasDown;
        private Point LastMouse;
        private WorldState State;
        public float CamHeight;

        public bool UseZoomHold => true;
        public bool UseRotateHold => true;
        public WorldRotation CutRotation => (WorldRotation)(DirectionUtils.PosMod(Math.Round(RotationX / (float)(Math.PI / 2) + 0.5f), 4));
        public ICamera BaseCamera => Camera;

        protected float _RotationX = -(float)(Math.PI / 4);
        protected float _RotationY = 1f;
        protected float _Zoom3D = 3.7f;

        public float RotationX
        {
            get { return _RotationX; }
            set { _RotationX = value; InvalidateCamera(State); }
        }
        
        public float RotationY
        {
            get { return _RotationY; }
            set { _RotationY = (float)Math.Min(Math.PI, Math.Max(0, value)); InvalidateCamera(State); }
        }
        
        public float Zoom3D
        {
            get { return _Zoom3D; }
            set { value = Math.Min(100, Math.Max(0, value)); _Zoom3D = value; InvalidateCamera(State); }
        }

        public CameraController3D(GraphicsDevice gd, WorldState state)
        {
            Camera = new WorldCamera3D(gd, Vector3.Zero, Vector3.Zero, Vector3.Up);
            State = state;
        }

        public void SetDimensions(Vector2 dim)
        {
            Camera.ProjectionOrigin = dim / 2;
        }

        public virtual void InvalidateCamera(WorldState state)
        {
            if (state == null) return;
            var baseHeight = CamHeight;
                Camera.Target = new Vector3(state.CenterTile.X * WorldSpace.WorldUnitsPerTile, baseHeight + 3, state.CenterTile.Y * WorldSpace.WorldUnitsPerTile);
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

        public virtual ICameraController BeforeActive(ICameraController previous, World world)
        {
            if (previous is CameraControllerFP)
            {
                var fp = (CameraControllerFP)previous;
                _RotationX = fp.RotationX;
                _RotationY = fp.SavedYRot;
                _Zoom3D = fp.Zoom3D;
                InvalidateCamera(world.State);
                var relative = ComputeCenterRelative();
                SwitchCenter = world.State.CenterTile - new Vector2(relative.X / WorldSpace.WorldUnitsPerTile, relative.Z / WorldSpace.WorldUnitsPerTile);

                CamHeight = fp.CamHeight;
                CamHeight -= relative.Y - fp.FPCamHeight;

                if (previous is CameraControllerDirect direct)
                {
                    if (direct.FirstPersonAvatar != null)
                    {
                        direct.FirstPersonAvatar.Avatar.HideHead = false;
                    }
                }
            }
            else if (previous is CameraController2D)
            {
                //just guess camera zoom and rotation?
                Inherit2D((CameraController2D)previous, world);
            }
            return previous;
        }

        public Vector2? SwitchCenter;

        public virtual void OnActive(ICameraController previous, World world)
        {
            if (SwitchCenter != null) world.State.CenterTile = SwitchCenter.Value;
            SwitchCenter = null;
        }

        public void Inherit2D(CameraController2D controller, World world)
        {
            var cam = controller.Camera;
            switch (cam.Zoom)
            {
                case WorldZoom.Near:
                    Zoom3D = 3.7f; break;
                case WorldZoom.Medium:
                    Zoom3D = 7f; break;
                case WorldZoom.Far:
                    Zoom3D = 11f; break;
            }

            RotationX = (float)Math.PI * (((int)cam.Rotation) / 2f - 0.25f);
            RotationY = 0;

            if (world.State.ProjectTilePos != null) {
                world.State.Cameras.WithTransitionsDisabled(() =>
                {
                    var pos = world.State.ProjectTilePos(world.State.WorldSpace.WorldPx / 2);
                    SwitchCenter = new Vector2(pos.X, pos.Y);
                });
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
                RotationX += (mpos.X - LastMouse.X) / 250f;
                RotationY += (mpos.Y - LastMouse.Y) / 150f;
            }

            if (md)
            {
                LastMouse = state.MouseState.Position;
            }
            MouseWasDown = md;
        }

        public virtual void PreDraw(World world)
        {

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
