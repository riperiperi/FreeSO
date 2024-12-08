using Microsoft.Xna.Framework;
using System;

namespace FSO.Common.Rendering
{
    public static class FinaleUtils
    {
        private static Color[] FinaleColors = new Color[]
        {
            new Color(2, 2, 2),
            new Color(2, 2, 2),
            Color.Lerp(new Color(0, 0, 0), new Color(50, 70, 122)*1.25f, 0.5f),
            new Color(70, 70, 70)*1.25f,
            new Color(217, 109, 50), //sunrise
            new Color(255, 255, 255),
            new Color(255, 255, 255), //peak
            new Color(255, 255, 255), //peak
            new Color(255, 255, 255),
            new Color(255, 255, 255),
            Color.Lerp(new Color(255, 255, 255), new Color(217, 109, 50), 0.33f),
            Color.Lerp(new Color(255, 255, 255), new Color(217, 109, 25), 0.66f),
            new Color(225, 64, 0), //sunset
            new Color(0, 0, 0)
        };

        private static double DayOffset = 0.25;
        private static double DayDuration = 0.60;

        private static double TargetEnd = 1.04;

        public static double BiasSunTime(double modTime)
        {
            if (IsFinale())
            {
                double dayMid = DayOffset + DayDuration / 2;
                double mainEnd = DayOffset + DayDuration;

                double rescaleDay = (TargetEnd - dayMid) / (mainEnd - dayMid);

                if (modTime > dayMid)
                {
                    modTime = dayMid + (modTime - dayMid) / rescaleDay;
                }
            }

            return modTime;
        }

        public static Vector3 BiasSunIntensity(Vector3 intensity, float time)
        {
            if (IsFinale())
            {
                if (time > 0.70)
                {
                    intensity = Vector3.Lerp(new Vector3(0.6f), new Vector3(1, 0.2f, 0.1f), (time - 0.70f) / 0.25f);
                }
                else if (time > 0.5)
                {
                    intensity = Vector3.Lerp(intensity, new Vector3(0.6f), (time - 0.5f) / 0.2f);
                }

                if (time > 0.995)
                {
                    intensity *= (1f - time) * 200f;
                }
            }

            return intensity;
        }

        public static bool IsFinale()
        {
            var time = DateTime.UtcNow;

            return time.Year == 2024 && time.Month == 12 && ((time.Day == 8 && time.Hour == 23) || (time.Day == 9 && time.Hour == 0));
        }

        public static float GetDarkness()
        {
            return IsFinale() ? 1.0f : 0.8f;
        }

        public static Color[] SwapFinaleColors(Color[] colors)
        {
            return IsFinale() ? FinaleColors : colors;
        }
    }
}
