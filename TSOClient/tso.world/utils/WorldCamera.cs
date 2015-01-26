/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Common.rendering.framework.camera;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace tso.world.utils
{
    public class WorldCamera : OrthographicCamera
    {
        private float _WorldSize;
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

        private Vector2 _CenterTile;
        public Vector2 CenterTile
        {
            get { return _CenterTile; }
            set { _CenterTile = value; m_ViewDirty = true; }
        }

        private WorldRotation _Rotation;
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

        public WorldCamera(GraphicsDevice device)
            : base(device, Vector3.Zero, Vector3.Zero, Vector3.Up)
        {
        }

        protected override void CalculateProjection()
        {
            var diagnal = 64.0f;
            if (_Zoom == WorldZoom.Medium)
            {
                diagnal = 128.0f;
            }
            else if (_Zoom == WorldZoom.Near)
            {
                diagnal = 256.0f;
            }
            var isoScale = Math.Sqrt(WorldSpace.WorldUnitsPerTile * WorldSpace.WorldUnitsPerTile * 2.0f) / diagnal;
            var hb = (float)(m_Device.Viewport.Width * isoScale);
            var vb = (float)(m_Device.Viewport.Height * isoScale);

            m_Projection = Matrix.CreateOrthographicOffCenter(-hb, hb, -vb, vb, -300.0f, 300.0f);
        }

        protected override void CalculateView()
        {

            var offset = new Vector3((CenterTile.X * WorldSpace.WorldUnitsPerTile) / 2.0f, 0.0f, (CenterTile.Y * WorldSpace.WorldUnitsPerTile) / 2.0f);
            offset = Vector3.Zero;
            offset = new Vector3(32.0f, 0.0f, -32.0f);
            //offset = _Offset;

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

        /**var isoScale = Math.sqrt(1.5*1.5*2)/128 // diagonally across a square is 128
		//mat4.perspective(pMatrix, 45, gl.viewportWidth / gl.viewportHeight, 0.1, 100.0);
		var hb = canvas.width*isoScale
		var vb = canvas.height*isoScale
		mat4.ortho(pMatrix, -hb, hb, -vb, vb, 0.1, 100)
        **/


        /**
		mat4.translate(mvMatrix, mvMatrix, [0.0, -2, -7.0]);
		mat4.rotateX(mvMatrix, mvMatrix, (30/180)*Math.PI);
		mat4.rotateY(mvMatrix, mvMatrix, (45/180)*Math.PI);
         */

    }
}
