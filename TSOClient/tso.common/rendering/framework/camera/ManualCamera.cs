using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSO.Common.rendering.framework.camera
{
    public class ManualCamera : ICamera
    {
        #region ICamera Members

        public Microsoft.Xna.Framework.Matrix View { get; set; }

        public Microsoft.Xna.Framework.Matrix Projection { get; set; }

        public Microsoft.Xna.Framework.Vector3 Position { get; set; }

        public Microsoft.Xna.Framework.Vector3 Target { get; set; }

        public Microsoft.Xna.Framework.Vector3 Up { get; set; }

        public Microsoft.Xna.Framework.Vector3 Translation { get; set; }

        public Microsoft.Xna.Framework.Vector2 ProjectionOrigin { get; set; }

        public float NearPlane { get; set; }

        public float FarPlane { get; set; }

        public float Zoom { get; set; }

        public float AspectRatioMultiplier { get; set; }

        #endregion
    }
}
