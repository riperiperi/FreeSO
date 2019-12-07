using FSO.Common.Rendering.Framework.Camera;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.LotView.Utils.Camera
{
    public class DummyCamera : ICamera
    {
        public Matrix View { get; set; }
        public Matrix Projection { get; set; }

        public Vector3 Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Vector3 Target { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Vector3 Up { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Vector3 Translation { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Vector2 ProjectionOrigin { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public float NearPlane { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public float FarPlane { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public float Zoom { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public float AspectRatioMultiplier { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void ProjectionDirty()
        {
        }
    }
}
