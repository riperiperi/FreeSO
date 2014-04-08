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

            m_Projection = Matrix.CreateOrthographicOffCenter(-hb, hb, -vb, vb, -5000.0f, 5000.0f);
        }

        protected override void CalculateView()
        {
        //    var radius = WorldSpace.WorldUnitsPerTile;
        //    var opposite = (float)Math.Cos(MathHelper.ToRadians(30.0f)) * radius;

        //    var position = new Vector3(0.0f, 2.0f, 7.0f);
        //    var target = new Vector3(0.0f, 0.0f, 0.0f);

        //    var translation = Matrix.CreateTranslation(WorldSpace.GetWorldFromTile(CenterTile));
        //    position = Vector3.Transform(position, translation);
        //    target = Vector3.Transform(target, translation);

        //    m_View = Matrix.CreateLookAt(position, target, m_Up);

            //var target = Vector3.Zero;//new Vector3(_CenterTile.X, 0.0f, _CenterTile.Y);
            //var position = Vector3.Zero;//new Vector3(target.X, target.Y, target.Z);
            //position.X += WorldSpace.WorldUnitsPerTile;
            ////position.Z += WorldSpace.WorldUnitsPerTile;

            //position = Vector3.Transform(position, Matrix.CreateRotationX(MathHelper.ToRadians(30)));
            //position = Vector3.Transform(position, Matrix.CreateRotationY(MathHelper.ToRadians(45)));

            //var translation = Matrix.CreateTranslation(_CenterTile.X, 0.0f, _CenterTile.Y);
            //target = Vector3.Transform(target, translation);
            //position = Vector3.Transform(position, translation);

            //m_View = Matrix.CreateLookAt(position, target, m_Up);

            /*
            var translate = Matrix.CreateTranslation(m_Translation);
            var position = Vector3.Transform(m_Position, translate);
            var target = Vector3.Transform(m_Target, translate);

            m_View = Matrix.CreateLookAt(position, target, m_Up);
            */

            //var offset = new Vector3(CenterTile.X * WorldSpace.WorldUnitsPerTile, 0.0f, CenterTile.Y * WorldSpace.WorldUnitsPerTile);
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

            if (_Rotation == WorldRotation.TopRight){
                //offset = new Vector3((CenterTile.X * WorldSpace.WorldUnitsPerTile), 0.0f, 0.0f);
                //offset = new Vector3(0.0f, (CenterTile.X * WorldSpace.WorldUnitsPerTile), (CenterTile.Y * WorldSpace.WorldUnitsPerTile));

                //offset = new Vector3(0.0f, centerX, centerY);
                //offset = new Vector3(0.0f, -centerX, centerY);
                //offset = new Vector3(0.0f, centerX, -centerY);
                //offset = new Vector3(0.0f, -centerX, -centerY);

                //offset = new Vector3(0.0f, centerY, centerX);
                //offset = new Vector3(0.0f, -centerY, centerX);
                //offset = new Vector3(0.0f, centerY, -centerX);
                //offset = new Vector3(0.0f, -centerY, -centerX);

                //offset = new Vector3(centerX, 0.0f, centerY);
                //offset = new Vector3(-centerX, 0.0f, centerY);
                //offset = new Vector3(centerX, 0.0f, -centerY);    > wrong position but right movement
                //offset = new Vector3(centerX - (size), 0.0f, -centerY + 0.0f);//   > wrong movement but right position

                //offset = new Vector3(centerY, 0.0f, centerX);
                //offset = new Vector3(-centerY, 0.0f, centerX);
                //offset = new Vector3(centerY, 0.0f, -centerX);
                //offset = new Vector3(-centerY, 0.0f, -centerX);

                //offset = new Vector3(centerX, centerY, 0.0f);
                //offset = new Vector3(-centerX, centerY, 0.0f);
                //offset = new Vector3(centerX, -centerY, 0.0f);
                //offset = new Vector3(-centerX, -centerY, 0.0f);

                //offset = new Vector3(centerY, centerX, 0.0f);
                //offset = new Vector3(-centerY, centerX, 0.0f);
                //offset = new Vector3(centerY, -centerX, 0.0f);
                //offset = new Vector3(-centerY, -centerX, 0.0f);

            }

            /**45,
            135
            225
            315**/
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
            
            //view *= Matrix.CreateRotationX(MathHelper.ToRadians(_RotateX));
            //view *= Matrix.CreateRotationZ(MathHelper.ToRadians(_RotateZ));
            //view *= Matrix.CreateRotationY(MathHelper.ToRadians(_RotateY));
            ////view *= Matrix.CreateTranslation(WorldSpace.GetWorldFromTile(CenterTile));

            //view += Matrix.CreateTranslation(offset);

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
