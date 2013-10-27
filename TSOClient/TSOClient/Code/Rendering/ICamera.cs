using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace TSOClient.Code.Rendering
{
    public interface ICamera
    {
        Matrix View { get; }
        Matrix Projection { get; }

        Vector3 Position { get; set; }
        Vector3 Target { get; set; }
        Vector3 Up { get; set; }
        Vector3 Translation { get; set; }

        Vector2 ProjectionOrigin { get; set; }
        float NearPlane { get; set; }
        float FarPlane { get; set; }
        float Zoom { get; set; }
        float AspectRatioMultiplier { get; set; }

    }
}
