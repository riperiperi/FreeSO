/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        private Vector2 _CenterTile;
        public Vector2 CenterTile
        {
            get { return _CenterTile; }
            set { _CenterTile = value; m_ViewDirty = true; }
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

        private float _RotateX = 30.0f;

        /// <summary>
        /// Gets or sets camera's rotation along the x axis.
        /// </summary>
        public float RotateX
        {
            get
            {
                return _RotateX;
            }
            set
            {
                _RotateX = value;
                m_ViewDirty = true;
            }
        }

        private float _RotateY = 315.0f;

        /// <summary>
        /// Gets or sets the camera's rotation along the y axis.
        /// </summary>
        public float RotateY
        {
            get
            {
                return _RotateY;
            }
            set
            {
                _RotateY = value;
                m_ViewDirty = true;
            }
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
            var isoScale = Math.Sqrt(WorldSpace.WorldUnitsPerTile * WorldSpace.WorldUnitsPerTile * 2.0f) / diagnal;
            var hb = (float)(m_Device.Viewport.Width * isoScale);
            var vb = (float)(m_Device.Viewport.Height * isoScale);

            var viewDim = (ViewDimensions.X == -1) ? new Vector2(m_Device.Viewport.Width, m_Device.Viewport.Height) : ViewDimensions;

            var hb2 = (float)(viewDim.X * isoScale);
            var vb2 = (float)(viewDim.Y * isoScale);

            m_Projection = Matrix.CreateOrthographicOffCenter(-hb2, -hb2+(hb*2), vb2 - (vb * 2), vb2, -(150.0f+depthRange-64), depthRange);
        }

        public Matrix GetRotationMatrix()
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

            var view = Matrix.Identity;
            view *= Matrix.CreateRotationY(MathHelper.ToRadians(rotationY));
            view *= Matrix.CreateRotationX(MathHelper.ToRadians(30.0f));
            return view;
        }

        protected override void CalculateView()
        {
            var offset = new Vector3((CenterTile.X * WorldSpace.WorldUnitsPerTile) / 2.0f, 0.0f, (CenterTile.Y * WorldSpace.WorldUnitsPerTile) / 2.0f);
            offset = Vector3.Zero;
            offset = new Vector3(32.0f, 0.0f, -32.0f);

            var centerX = CenterTile.X * WorldSpace.WorldUnitsPerTile;
            var centerY = CenterTile.Y * WorldSpace.WorldUnitsPerTile;
            var size = WorldSize * WorldSpace.WorldUnitsPerTile;
            var halfSize = (WorldSize/2.0f) * WorldSpace.WorldUnitsPerTile;

            offset = new Vector3(-centerX, 0.0f, -centerY);
            var view = Matrix.Identity;

            view *= Matrix.CreateTranslation(offset.X, offset.Y, offset.Z);
            //view = Matrix.Identity;
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
            view *= Matrix.CreateRotationY(MathHelper.ToRadians(rotationY));
            view *= Matrix.CreateRotationX(MathHelper.ToRadians(30.0f));
            m_View = view;
        }
    }
}
