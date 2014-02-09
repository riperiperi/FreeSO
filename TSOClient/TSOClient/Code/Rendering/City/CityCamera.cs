using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using tso.common.rendering.framework.camera;

namespace TSOClient.Code.Rendering.City
{
    public class CityCamera : ICamera
    {
        private float _FarZoomScale = 5.10f;
        private float _NearZoomScale = 144.0f;
        private float _ScreenWidth = 1024.0f;
        private float _ScreenHeight = 768.0f;
        private float _RotationX = 45.0f;
        private float _RotationY = 30.0f;
        private float _TranslationX = -360.0f;
        private float _TranslationY = -512.0f;
        private float _ZoomProgress = 0.0f;

        private float _ViewOffX = 0.0f;
        private float _ViewOffY = 0.0f;


        private bool _DirtyView = true;
        private Matrix _View;

        private bool _DirtyProjection = true;
        private Matrix _Projection;

        public float ZoomProgress
        {
            get
            {
                return _ZoomProgress;
            }
            set
            {
                _ZoomProgress = value;
                _DirtyView = true;
                _DirtyProjection = true;
            }
        }

        public float TranslationY
        {
            get
            {
                return _TranslationY;
            }
            set
            {
                _TranslationY = value;
                _DirtyView = true;
                _DirtyProjection = true;
            }
        }

        public float TranslationX
        {
            get
            {
                return _TranslationX;
            }
            set
            {
                _TranslationX = value;
                _DirtyView = true;
                _DirtyProjection = true;
            }
        }

        public float RotationY
        {
            get
            {
                return _RotationY;
            }
            set
            {
                _RotationY = value;
                _DirtyView = true;
                _DirtyProjection = true;
            }
        }


        public float RotationX
        {
            get
            {
                return _RotationX;
            }
            set
            {
                _RotationX = value;
                _DirtyView = true;
                _DirtyProjection = true;
            }
        }


        public float FarZoomScale
        {
            get
            {
                return _FarZoomScale;
            }
            set
            {
                _FarZoomScale = value;
                _DirtyView = true;
                _DirtyProjection = true;
            }
        }

        public float NearZoomScale
        {
            get
            {
                return _NearZoomScale;
            }
            set
            {
                _NearZoomScale = value;
                _DirtyView = true;
                _DirtyProjection = true;
            }
        }

        public float ScreenWidth
        {
            get
            {
                return _ScreenWidth;
            }
            set
            {
                _ScreenWidth = value;
                _DirtyView = true;
                _DirtyProjection = true;
            }
        }

        public float ScreenHeight
        {
            get
            {
                return _ScreenHeight;
            }
            set
            {
                _ScreenHeight = value;
                _DirtyView = true;
                _DirtyProjection = true;
            }
        }

        #region ICamera Members

        public Microsoft.Xna.Framework.Matrix View
        {
            get {
                if (_DirtyView)
                {
                    _View = Matrix.Identity;
                    _View *= Matrix.CreateRotationY((_RotationX / 180.0f) * MathHelper.Pi);
                    _View *= Matrix.CreateRotationX((_RotationY / 180.0f) * MathHelper.Pi);
                    _View *= Matrix.CreateTranslation(new Vector3(_TranslationX, 0.0f, _TranslationY));
                    _View *= Matrix.CreateScale(1.0f, 0.5f + ((1.0f - _ZoomProgress) / 2.0f), 1.0f);

                    _DirtyView = false;
                }
                return _View;
            }
        }

        public Microsoft.Xna.Framework.Matrix Projection
        {
            get {

                if (_DirtyProjection)
                {
                    var device = GameFacade.GraphicsDevice;
                    var aspect = device.Viewport.AspectRatio * AspectRatioMultiplier;


                    var fisoScale = (float)Math.Sqrt(0.5f * 0.5f * 2.0f) / _FarZoomScale; // is 5.10 on far zoom
                    var zisoScale = (float)Math.Sqrt(0.5f * 0.5f * 2.0f) / _NearZoomScale; // currently set 144 to near zoom

                    var zoomProgress = 0f;
                    var isoScale = (float)((1.0f - _ZoomProgress) * fisoScale + (_ZoomProgress) * zisoScale);

                    var hb = ((_ScreenWidth) * isoScale);
                    var vb = ((_ScreenHeight) * isoScale) * AspectRatioMultiplier;


                    _Projection = Microsoft.Xna.Framework.Matrix.CreateOrthographicOffCenter((float)-hb + _ViewOffX, (float)hb + _ViewOffX, ((float)-vb + _ViewOffY), ((float)vb + _ViewOffY), 0.1f, 1000000);
                    _DirtyProjection = false;
                }

                
                return _Projection;
            }
        }

        public Microsoft.Xna.Framework.Vector3 Position
        {
            get
            {
                return Vector3.Zero;
            }
            set
            {
            }
        }

        public Microsoft.Xna.Framework.Vector3 Target
        {
            get
            {
                return Vector3.Zero;
            }
            set
            {
            }
        }

        public Microsoft.Xna.Framework.Vector3 Up
        {
            get
            {
                return Vector3.Up;
            }
            set
            {
            }
        }

        public Microsoft.Xna.Framework.Vector3 Translation
        {
            get
            {
                return Vector3.Zero;
            }
            set
            {
            }
        }

        public Microsoft.Xna.Framework.Vector2 ProjectionOrigin
        {
            get
            {
                return Vector2.Zero;
            }
            set
            {
            }
        }

        public float NearPlane
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public float FarPlane
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public float Zoom
        {
            get
            {
                return 1.0f;
            }
            set
            {
            }
        }

        private float _AspectRatioMultiplier = 1.0f;
        public float AspectRatioMultiplier
        {
            get
            {
                return _AspectRatioMultiplier;
            }
            set
            {
                _AspectRatioMultiplier = value;
                _DirtyProjection = true;
            }
        }

        #endregion
    }
}
