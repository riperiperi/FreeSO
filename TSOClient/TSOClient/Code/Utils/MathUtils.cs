using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace TSOClient.Code.Utils
{
    public class MathUtils
    {
        public static Vector2 RotateVector2(Vector2 point, float radians, Vector2 pivot)
        {
            
            float cosRadians = (float)Math.Cos(radians);
            float sinRadians = (float)Math.Sin(radians);

            Vector2 translatedPoint = new Vector2();
            translatedPoint.X = point.X - pivot.X;
            translatedPoint.Y = point.Y - pivot.Y;

            Vector2 rotatedPoint = new Vector2();
            rotatedPoint.X = translatedPoint.X * cosRadians - translatedPoint.Y * sinRadians + pivot.X;
            rotatedPoint.Y = translatedPoint.X * sinRadians + translatedPoint.Y * cosRadians + pivot.Y;

            return rotatedPoint;
        }


        public static Vector2 GetCenter(Rectangle bounds)
        {
            return new Vector2(
                bounds.X + (bounds.Width / 2),
                bounds.Y + (bounds.Height / 2)
            );
        }
    }
}
