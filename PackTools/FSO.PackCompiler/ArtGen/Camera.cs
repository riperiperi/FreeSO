using System;

namespace FSO.PackCompiler.ArtGen
{
    public struct Vec3
    {
        public double X, Y, Z;
        public Vec3(double x, double y, double z) { X = x; Y = y; Z = z; }
        public static Vec3 operator +(Vec3 a, Vec3 b) => new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vec3 operator *(Vec3 a, double s) => new Vec3(a.X * s, a.Y * s, a.Z * s);
        public double Dot(Vec3 b) => X * b.X + Y * b.Y + Z * b.Z;
        public static Vec3 Cross(Vec3 a, Vec3 b) => new Vec3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
        public double Length() => Math.Sqrt(X * X + Y * Y + Z * Z);
        public Vec3 Normalized() { var l = Length(); return l > 1e-12 ? new Vec3(X / l, Y / l, Z / l) : this; }
    }

    /// <summary>
    /// TSO's derived render camera (ART-PIPELINE-CALIBRATION.md): orthographic, 30 deg pitch,
    /// 45-deg-offset 90-deg-step yaw. This basis is also what the lighting model in
    /// Renderer.cs is expressed relative to — a light fixed in this camera-space basis stays
    /// fixed relative to the screen across all 4 yaw directions, which is what
    /// ART-PIPELINE-CALIBRATION.md §7 found empirically (the same left-darker/right-brighter
    /// pattern held regardless of which world-facing direction was rendered).
    /// </summary>
    public class Camera
    {
        public const double Pitch = 30.0 * Math.PI / 180.0;

        public Vec3 ToCamera; // points from the scene toward the camera
        public Vec3 Right;    // screen-right, in world space
        public Vec3 Up;       // screen-up, in world space

        public Camera(double yawRadians, double pitchRadians)
        {
            ToCamera = new Vec3(Math.Sin(yawRadians) * Math.Cos(pitchRadians), Math.Sin(pitchRadians), Math.Cos(yawRadians) * Math.Cos(pitchRadians)).Normalized();
            var worldUp = new Vec3(0, 1, 0);
            Right = Vec3.Cross(worldUp, ToCamera).Normalized();
            Up = Vec3.Cross(ToCamera, Right).Normalized();
        }

        public (double sx, double sy, double depth) Project(Vec3 p)
        {
            var sx = p.Dot(Right);
            var sy = p.Dot(Up);
            var depth = -p.Dot(ToCamera); // more negative = nearer camera
            return (sx, sy, depth);
        }
    }
}
