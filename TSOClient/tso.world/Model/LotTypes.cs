using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.LotView.Model
{
    public static class LotTypeGrassInfo
    {
        public static Color[] LightGreen = {
            new Color(80, 116, 59),
            new Color(181, 171, 149),
                        new Color(126,96,70),
            new Color(240,245,250),
            new Color(0,0,255)
        };
        public static Color[] LightBrown = {
            new Color(157, 117, 65),
            new Color(196, 185, 162),
                        new Color(126,96,70),
            new Color(240,245,250),
            new Color(0,0,255)

        };
        public static Color[] DarkGreen = {
            new Color(8, 52, 8),
            new Color(115, 109, 95),
                        new Color(107,77,57),
            new Color(180,180,190),
            new Color(0,0,255)
        };
        public static Color[] DarkBrown = {
            new Color(81, 60, 18),
            new Color(121, 114, 100),
                        new Color(107,77,57),
            new Color(180,180,190),
            new Color(0,0,255)
        };

        public static int[] Heights =
        {
            6,
            1,
            1,
            1,
            0
        };

        public static float[] GrassDensity =
        {
            1f,
            1f,
            1f,
            1f,
            1f
        };
    }
}
