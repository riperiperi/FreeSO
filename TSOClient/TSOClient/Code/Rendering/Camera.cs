using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace TSOClient.Code.Rendering
{
    public class Camera
    {
        public Matrix Projection { get; set; }

        public float NearPlane = 1.0f;
        public float FarPlane = 100.0f;

        private Vector3 m_Position;
        private Vector3 m_Target;
        private Vector3 m_Up;


        public Camera(Vector3 Position, Vector3 Target, Vector3 Up)
        {
            m_Position = Position;
            m_Target = Target;
            m_Up = Up;

            m_ViewDirty = true;


            /**
             * Assume the projection is full screen, center origin
             */

            ProjectionOrigin = new Vector2(
                GameFacade.GraphicsDevice.Viewport.Width / 2.0f,
                GameFacade.GraphicsDevice.Viewport.Height / 2.0f
            );

            //Projection = Matrix.CreatePerspectiveFieldOfView(
            //    MathHelper.PiOver4,
            //    (float)GameFacade.GraphicsDevice.Viewport.Width /
            //    (float)GameFacade.GraphicsDevice.Viewport.Height,
            //    NearPlane, FarPlane);

            /*
            View = Matrix.CreateLookAt(pos, target, up);

            Projection = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.PiOver4,
                (float)GameFacade.Game.Window.ClientBounds.Width /
                (float)GameFacade.Game.Window.ClientBounds.Height,
                1, 1000000);
             */
            //float width = GameFacade.Game.Window.ClientBounds.Width;
            //float height = GameFacade.Game.Window.ClientBounds.Height;
            //projection = Matrix.CreateOrthographic(width, height, 0, 6);

            //View = Matrix.CreateLookAt(pos, target, up) * Matrix.CreateScale(0.1f);
            //float width = GameFacade.Game.Window.ClientBounds.Width;
            //float height = GameFacade.Game.Window.ClientBounds.Height;
            //Projection = Matrix.CreateOrthographic(width, height, -2000, farPlane);
        }


        private Vector2 m_ProjectionOrigin = Vector2.Zero;
        public Vector2 ProjectionOrigin
        {
            get
            {
                return m_ProjectionOrigin;
            }
            set
            {
                m_ProjectionOrigin = value;
                CalculateProjection();
            }
        }



        private void CalculateProjection()
        {
            var device = GameFacade.GraphicsDevice;
            var aspect = device.Viewport.AspectRatio;


            var ratioX = m_ProjectionOrigin.X / device.Viewport.Width;
            var ratioY = m_ProjectionOrigin.Y / device.Viewport.Height;

            var projectionX = 0.0f - (1.0f * ratioX);
            var projectionY = (1.0f * ratioY);

            Projection = Matrix.CreatePerspectiveOffCenter(
                projectionX, projectionX + 1.0f,
                ((projectionY-1.0f) / aspect), (projectionY) / aspect,
                NearPlane, FarPlane
            );

            /*

            var ratioX = 1024.0f / 1024.0f;
            var ratioY = 10.0f / 768.0f;
            var projectionX = 0.0f - (1.0f * ratioX);
            var projectionY = 0.0f - (1.0f * ratioY);
            effect.Projection = Matrix.CreatePerspectiveOffCenter(projectionX, projectionX + 1.0f, (projectionY / aspect), (projectionY + 1.0f) / aspect, 1.0f, 100.0f);
            */
        }







        private bool m_ViewDirty = false;
        private Matrix m_View = Matrix.Identity;
        public Matrix View
        {
            get
            {
                if (m_ViewDirty)
                {
                    m_ViewDirty = false;
                    m_View = Matrix.CreateLookAt(m_Position, m_Target, m_Up);// *Matrix.CreateTranslation(new Vector3(1, 1, 0.0f));
                }
                return m_View;
            }
        }



        public Vector3 Position
        {
            get
            {
                return m_Position;
            }
            set
            {
                m_Position = value;
                m_ViewDirty = true;
            }
        }

        public Vector3 Target
        {
            get
            {
                return m_Target;
            }
            set
            {
                m_Target = value;
                m_ViewDirty = true;
            }
        }

        public Vector3 Up
        {
            get
            {
                return m_Up;
            }
            set
            {
                m_Up = value;
                m_ViewDirty = true;
            }
        }


    }
}
