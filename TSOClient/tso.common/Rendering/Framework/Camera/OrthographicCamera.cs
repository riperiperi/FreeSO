using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Common.Rendering.Framework.Camera
{
    /// <summary>
    /// Orthographic camera for the game. Used for rendering lots.
    /// </summary>
    public class OrthographicCamera : BasicCamera
    {
        public OrthographicCamera(GraphicsDevice device, Vector3 Position, Vector3 Target, Vector3 Up) 
            : base(device, Position, Target, Up)
        {
        }

        protected override void CalculateProjection()
        {
            var device = m_Device;
            var aspect = device.Viewport.AspectRatio * AspectRatioMultiplier;

            var ratioX = m_ProjectionOrigin.X / device.Viewport.Width;
            var ratioY = m_ProjectionOrigin.Y / device.Viewport.Height;

            var projectionX = 0.0f - (1.0f * ratioX);
            var projectionY = (1.0f * ratioY);

            m_Projection = Matrix.CreateOrthographicOffCenter(
                projectionX, projectionX + 1.0f,
                ((projectionY - 1.0f) / aspect), (projectionY) / aspect,
                NearPlane, FarPlane
            );

            var zoom = 1 / m_Zoom;
            m_Projection = m_Projection * Matrix.CreateScale(zoom);
        }
    }
}
