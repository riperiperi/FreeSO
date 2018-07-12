using FSO.Common.Rendering.Framework.Camera;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.LotView.Utils
{
    public class WorldCamera3D : BasicCamera
    {
        public WorldCamera3D(GraphicsDevice device, Vector3 Position, Vector3 Target, Vector3 Up) : base(device, Position, Target, Up)
        {
        }

        private float _fromIntensity = 0f;
        public float FromIntensity
        {
            get { return _fromIntensity; }
            set
            {
                _fromIntensity = value;
                m_ProjectionDirty = true;
                m_ViewDirty = true;
            }
        }

        public Matrix FromProjection;
        public Matrix FromView;

        protected override void CalculateView()
        {
            var trans = Translation;
            if (FromIntensity > 0) m_Translation = Vector3.Zero;
            base.CalculateView();

            if (FromIntensity > 0)
            {
                Vector3 scale; Quaternion quat; Vector3 translation;
                m_View.Decompose(out scale, out quat, out translation);

                Vector3 scale2; Quaternion quat2; Vector3 translation2;
                FromView.Decompose(out scale2, out quat2, out translation2);

                m_View = Matrix.CreateTranslation(-trans) * Matrix.Lerp(m_View, FromView, FromIntensity);

                /*
                m_View = Matrix.CreateScale(Vector3.Lerp(scale, scale2, FromIntensity))
                    * Matrix.CreateFromQuaternion(Quaternion.Slerp(quat, quat2, FromIntensity))
                    * Matrix.CreateTranslation(Vector3.Lerp(translation, translation2, FromIntensity));

                m_View = Matrix.CreateTranslation(-trans) * m_View;
                */
            }
            m_Translation = trans;
        }

        protected override void CalculateProjection()
        {
            base.CalculateProjection();
            if (FromIntensity > 0)
            {
                m_Projection = Matrix.Lerp(m_Projection, FromProjection, FromIntensity);
            }
        }

        public Matrix BaseProjection()
        {
            if (FromIntensity == 0) return Projection;
            var mat = m_Projection;
            base.CalculateProjection();
            var mat2 = m_Projection;
            m_Projection = mat;
            return mat2;
        }

        public Matrix BaseView()
        {
            if (FromIntensity == 0) return View;
            var mat = m_View;
            base.CalculateView();
            var mat2 = m_View;
            m_View = mat;
            return mat2;
        }
    }
}
