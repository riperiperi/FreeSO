using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using FSO.Common.Rendering.Framework.Camera;
using Microsoft.Xna.Framework;
using FSO.Common.Utils;
using FSO.Vitaboy;
using FSO.LotView.Utils;

namespace FSO.LotView.RC
{

    public interface I3DRotate
    {
        float RotationX { get; set; }
        float RotationY { get; set; }
    }

    /// <summary>
    /// An alternate implenentation of WorldState that renders the game with a 3D camera.
    /// This changes the camera to use a 3D one, and remaps some of the old functionality to work with it.
    /// 
    /// RC stands for reconstruction, the primary method used to render game objects in 3D.
    /// </summary>
    public class WorldStateRC : WorldState, I3DRotate
    {
        public WorldStateRC(GraphicsDevice device, float worldPxWidth, float worldPxHeight, World world) : base(device, worldPxWidth, worldPxHeight, world)
        {
            _Camera = new WorldCamera3D(device, Vector3.Zero, Vector3.Zero, new Vector3(0, 1, 0));
        }

        private bool _Use2DCam;
        public bool Use2DCam
        {
            get
            {
                return _Use2DCam;
            }
            set
            {
                _Use2DCam = value;
                if (value)
                {
                    base.InvalidateCamera();
                }
            }
        }

        private WorldCamera3D _Camera;
        /*
        public override ICamera Camera
        {
            get { return (_Use2DCam)?WorldCamera:(ICamera)_Camera; }
        }
        */

        public WorldCamera3D Camera3D => _Camera;

        public bool FixedCam;

        public override void SetDimensions(Vector2 dim)
        {
            _Camera.ProjectionOrigin = dim/2;
            WorldSpace.SetDimensions(dim);
        }

        private float _RotationX = -(float)(Math.PI/4);
        private float _RotationY = 1f;
        private float _Zoom3D = 3.7f;

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

        public override WorldRotation CutRotation
        {
            get { return (WorldRotation)(DirectionUtils.PosMod(Math.Round(RotationX/(float)(Math.PI/2) + 0.5f), 4)); }
        }


        public float Zoom3D
        {
            get { return _Zoom3D; }
            set { value = Math.Min(100, Math.Max(0, value)); _Zoom3D = value; InvalidateCamera(); }
        }

        private float _CamHeight;
        public float CamHeight
        {
            get
            {
                return _CamHeight;
            }
            set
            {
                _CamHeight = value; InvalidateCamera();
            }
        }

        private bool _CameraMode;
        private bool _SwitchingMode;
        public bool CameraMode
        {
            get
            {
                return _CameraMode;
            }
            set
            {
                _SwitchingMode = true;
                if (value != CameraMode)
                {

                    if (value)
                    {
                        //switch into first person
                        var relative = ComputeCenterRelative();
                        SavedYRot = _RotationY;
                        var relNorm = relative;
                        relNorm.Normalize();
                        var rotY = (float)Math.Acos(Vector3.Dot(new Vector3(0, 1, 0), -relNorm));
                        //var rotY = (float)((1 - Math.Cos(_RotationY)) * Math.PI * 0.245f);
                        _RotationY = rotY;// - (float)Math.PI/2;
                        CenterTile += new Vector2(relative.X / WorldSpace.WorldUnitsPerTile, relative.Z / WorldSpace.WorldUnitsPerTile);
                        FPCamHeight = relative.Y;
                    } else
                    {
                        _RotationY = SavedYRot;
                        var relative = ComputeCenterRelative();
                        CenterTile -= new Vector2(relative.X / WorldSpace.WorldUnitsPerTile, relative.Z / WorldSpace.WorldUnitsPerTile);

                        _CamHeight -= relative.Y - FPCamHeight;

                    }
                }
                _SwitchingMode = false;
                _CameraMode = value;
                InvalidateCamera();
            }
        }

        private float SavedYRot;
        public float FPCamHeight;

        public override void InvalidateCamera()
        {
            if (Use2DCam)
            {
                base.InvalidateCamera();
                return;
            }
            if (_SwitchingMode) return;
            if (_Camera != null)
            {
                var baseHeight = CamHeight;
                if (CameraMode) {
                    if (FixedCam) return;
                    _Camera.Position = new Vector3(CenterTile.X * WorldSpace.WorldUnitsPerTile, baseHeight + 3 + FPCamHeight, CenterTile.Y * WorldSpace.WorldUnitsPerTile);

                    var mat = Matrix.CreateRotationZ((_RotationY - (float)Math.PI/2) * 0.99f) * Matrix.CreateRotationY(_RotationX);
                    _Camera.Target = _Camera.Position + Vector3.Transform(new Vector3(-10, 0, 0), mat);
                }
                else
                {
                    _Camera.Target = new Vector3(CenterTile.X * WorldSpace.WorldUnitsPerTile, baseHeight + 3, CenterTile.Y * WorldSpace.WorldUnitsPerTile);
                    _Camera.Position = _Camera.Target + ComputeCenterRelative();
                }
            }
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
    }
}
