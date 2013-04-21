using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace TSOClient.Code.UI.Framework
{
    /// <summary>
    /// Simpler Matrix object which requires less new object creation for UI system
    /// </summary>
    public static class Matrix2D
    {
        public static float[] IDENTITY = new float[6] { 1, 0, 0, 1, 0, 0 };
        public static float[] DEFAULT_SCALE = new float[2] { 1, 1 };

        public static float[] Invert(this float[] M)
        {
            var d = 1 / (M[0] * M[3] - M[1] * M[2]);
            var m0 = M[3] * d;
            var m1 = -M[1] * d;
            var m2 = -M[2] * d;
            var m3 = M[0] * d;
            var m4 = d * (M[2] * M[5] - M[3] * M[4]);
            var m5 = d * (M[1] * M[4] - M[0] * M[5]);

            return new float[6] { m0, m1, m2, m3, m4, m5 };
        }

        public static void Translate(this float[] M, float x, float y)
        {
            M[4] += M[0] * x + M[2] * y;
            M[5] += M[1] * x + M[3] * y;
        }

        public static void Scale(this float[] M, float sx, float sy)
        {
            M[0] *= sx;
            M[1] *= sx;
            M[2] *= sy;
            M[3] *= sy;
        }

        public static void TransformPoint(this float[] M, ref float x, ref float y)
        {
            x = x * M[0] + y * M[2] + M[4];
            y = x * M[1] + y * M[3] + M[5];
        }

        public static Vector2 TransformPoint(this float[] M, float x, float y)
        {
            return new Vector2(
                x * M[0] + y * M[2] + M[4], 
                x * M[1] + y * M[3] + M[5]
            );
        }

        public static Vector2 TransformPoint(this float[] M, Vector2 point)
        {
            return new Vector2(
                point.X * M[0] + point.Y * M[2] + M[4],
                point.X * M[1] + point.Y * M[3] + M[5]
            );
        }

        public static float[] ExtractScale(this float[] M)
        {
            return new float[2]{
                (float)Math.Sqrt(M[0] * M[0] + M[1] * M[1]),
                (float)Math.Sqrt(M[2] * M[2] + M[3] * M[3])
            };
        }

        public static Vector2 ExtractScaleVector(this float[] M)
        {
            float[] result = ExtractScale(M);
            return new Vector2(result[0], result[1]);
        }

        public static float[] CloneMatrix(this float[] M)
        {
            return new float[6] { M[0], M[1], M[2], M[3], M[4], M[5] };
        }



    }
}
