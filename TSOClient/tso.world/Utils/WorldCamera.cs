using System;
using FSO.Common.Rendering.Framework.Camera;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.LotView.Utils
{
    /// <summary>
    /// Camera used to draw world.
    /// </summary>
    public class WorldCamera : OrthographicCamera
    {
        private float _WorldSize;

        /// <summary>
        /// Gets or sets size of world.
        /// </summary>
        public float WorldSize
        {
            get
            {
                return _WorldSize;
            }
            set
            {
                _WorldSize = value;
                m_ViewDirty = true;
            }
        }
        private Vector2 _ViewDimensions = new Vector2(-1, -1);
        public Vector2 ViewDimensions
        {
            get
            {
                return _ViewDimensions;
            }
            set
            {
                _ViewDimensions = value;
                m_ProjectionDirty = true;
            }
        }

        private Vector3 _CenterTile;
        public Vector3 CenterTile
        {
            get { return _CenterTile; }
            set { _CenterTile = value; m_ViewDirty = true; }
        }

        private Vector3? _RotationAnchor;
        public Vector3? RotationAnchor
        {
            get { return _RotationAnchor; }
            set { _RotationAnchor = value; m_ViewDirty = true; }
        }


        private WorldRotation _Rotation;

        /// <summary>
        /// Gets or sets rotation of world.
        /// </summary>
        public WorldRotation Rotation
        {
            get{
                return _Rotation;
            }
            set
            {
                _Rotation = value;
                m_ViewDirty = true;
            }
        }

        private float _PreciseZoom = 1f;

        public float PreciseZoom
        {
            get { return _PreciseZoom; }
            set { _PreciseZoom = value; m_ProjectionDirty = true; }
        }

        private WorldZoom _Zoom;

        /// <summary>
        /// Gets or sets zoom of world.
        /// </summary>
        public new WorldZoom Zoom
        {
            get
            {
                return _Zoom;
            }
            set
            {
                _Zoom = value;
                m_ProjectionDirty = true;
            }
        }

        private Vector3 _Offset = new Vector3();
        public Vector3 Offset
        {
            get
            {
                return _Offset;
            }
            set
            {
                _Offset = value;
                m_ViewDirty = true;
            }
        }

        private float _RotateOff = 315.0f;

        /// <summary>
        /// Gets or sets the camera's offset rotation along the y axis.
        /// A non-zero value MUST be rendered using 3D components.
        /// </summary>
        public float RotateOff
        {
            get
            {
                return _RotateOff;
            }
            set
            {
                _RotateOff = value;
                m_ViewDirty = true;
            }
        }

        public Vector3 FrontDirection()
        {
            var rot = Rotation;
            switch (rot)
            {
                case WorldRotation.TopLeft:
                    return new Vector3(1, 0, 1);
                case WorldRotation.TopRight:
                    return new Vector3(1, 0, -1);
                case WorldRotation.BottomRight:
                    return new Vector3(-1, 0, -1);
                case WorldRotation.BottomLeft:
                    return new Vector3(-1, 0, 1);
            }
            return new Vector3(1, 0, 1);
        }

        /// <summary>
        /// Creates a new WorldCamera instance.
        /// </summary>
        /// <param name="device">GraphicsDevice instance, used for rendering.</param>
        public WorldCamera(GraphicsDevice device)
            : base(device, Vector3.Zero, Vector3.Zero, Vector3.Up)
        {
        }

        protected override void CalculateProjection()
        {
            var diagnal = 64.0f;
            var depthRange = 256.0f;
            if (_Zoom == WorldZoom.Medium)
            {
                diagnal = 128.0f;
                depthRange = 128.0f;
            }
            else if (_Zoom == WorldZoom.Near)
            {
                diagnal = 256.0f;
                depthRange = 64.0f;
            }
            depthRange *= 4;
            diagnal *= PreciseZoom;
            depthRange /= PreciseZoom;

            var isoScale = Math.Sqrt(WorldSpace.WorldUnitsPerTile * WorldSpace.WorldUnitsPerTile * 2.0f) / diagnal;
            var hb = (float)(m_Device.Viewport.Width * isoScale);
            var vb = (float)(m_Device.Viewport.Height * isoScale);

            var viewDim = (ViewDimensions.X == -1) ? new Vector2(m_Device.Viewport.Width, m_Device.Viewport.Height) : ViewDimensions;

            var hb2 = (float)(viewDim.X * isoScale);
            var vb2 = (float)(viewDim.Y * isoScale);

            m_Projection = Matrix.CreateOrthographicOffCenter(-hb2, -hb2+(hb*2), vb2 - (vb * 2), vb2, -(150.0f+depthRange-64), depthRange);
        }

        public Matrix GetInnerRotationMatrix(bool withOff = false)
        {
            var rotationY = 0.0f;
            switch (_Rotation)
            {
                case WorldRotation.TopLeft:
                    rotationY = 315.0f;
                    break;
                case WorldRotation.TopRight:
                    rotationY = 225.0f;
                    break;
                case WorldRotation.BottomRight:
                    rotationY = 135.0f;
                    break;
                case WorldRotation.BottomLeft:
                    rotationY = 45.0f;
                    break;
            }
            if (withOff) rotationY += _RotateOff;
            return Matrix.CreateRotationY(MathHelper.ToRadians(rotationY));
        }

        public Matrix GetRotationMatrix()
        {
            var view = GetInnerRotationMatrix();
            view *= Matrix.CreateRotationX(MathHelper.ToRadians(30.0f));
            return view;
        }

        protected override void CalculateView()
        {
            var centerX = CenterTile.X * WorldSpace.WorldUnitsPerTile;
            var centerY = CenterTile.Y * WorldSpace.WorldUnitsPerTile;
            var size = WorldSize * WorldSpace.WorldUnitsPerTile;
            var halfSize = (WorldSize/2.0f) * WorldSpace.WorldUnitsPerTile;

            var offset = new Vector3(-centerX, -CenterTile.Z*WorldSpace.WorldUnitsPerTile, -centerY);
            var view = Matrix.Identity;

            if (_RotationAnchor != null && RotateOff != 0)
            {
                var anchor = _RotationAnchor.Value;
                anchor = new Vector3(anchor.X, anchor.Z, anchor.Y) * -WorldSpace.WorldUnitsPerTile;
                var diff = anchor - offset;
                view *= Matrix.CreateTranslation(anchor);
                view *= Matrix.CreateRotationY(MathHelper.ToRadians(_RotateOff));
                view *= Matrix.CreateTranslation(-anchor);
                view *= Matrix.CreateTranslation(offset);
                view *= GetRotationMatrix();
                //view *= Matrix.CreateRotationX(MathHelper.ToRadians(30.0f));
            }
            else
            {
                view *= Matrix.CreateTranslation(offset.X, offset.Y, offset.Z);
                view *= GetRotationMatrix();
            }
            m_View = view;
            
        }
    }
}
