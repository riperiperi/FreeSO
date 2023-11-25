using Microsoft.Xna.Framework;
using System;

namespace FSO.Common.Rendering
{
    public static class TimeOfDayConfig
    {
        public static float DarknessMultiplier = 0.8f;
        private static Color[] m_TimeColors = new Color[]
        {
            new Color(50, 70, 122)*1.25f,
            new Color(50, 70, 122)*1.25f,
            new Color(55, 75, 111)*1.25f,
            new Color(70, 70, 70)*1.25f,
            new Color(217, 109, 50), //sunrise
            new Color(255, 255, 255),
            new Color(255, 255, 255), //peak
            new Color(255, 255, 255), //peak
            new Color(255, 255, 255),
            new Color(255, 255, 255),
            new Color(217, 109, 50), //sunset
            new Color(70, 70, 70)*1.25f,
            new Color(55, 75, 111)*1.25f,
            new Color(50, 70, 122)*1.25f,
        };

        private static float[] m_SkyColors = new float[]
        {
            4/8f,
            4/8f,
            4/8f,
            5/8f,
            6/8f, //sunrise
            7/8f,
            8/8f, //peak
            0/8f, //peak
            0/8f,
            0/8f,
            1/8f, //sunset
            2/8f,
            3/8f,
            4/8f,
        };

        private static Color PowColor(Color col, float pow)
        {
            var vec = col.ToVector4();
            vec.X = (float)Math.Pow(vec.X, pow);
            vec.Y = (float)Math.Pow(vec.Y, pow);
            vec.Z = (float)Math.Pow(vec.Z, pow);

            return new Color(vec);
        }

        public static Color ColorFromTime(double time)
        {
            Color col1 = m_TimeColors[(int)Math.Floor(time * (m_TimeColors.Length - 1))]; //first colour
            Color col2 = m_TimeColors[(int)Math.Floor(time * (m_TimeColors.Length - 1)) + 1]; //second colour
            if (DarknessMultiplier != 1.0)
            {
                col1 = Color.Lerp(Color.White, col1, DarknessMultiplier);
                col2 = Color.Lerp(Color.White, col2, DarknessMultiplier);
            }
            double Progress = (time * (m_TimeColors.Length - 1)) % 1; //interpolation progress (mod 1)

            return PowColor(Color.Lerp(col1, col2, (float)Progress), 2.2f); //linearly interpolate between the two colours for this specific time.
        }
    }
}
