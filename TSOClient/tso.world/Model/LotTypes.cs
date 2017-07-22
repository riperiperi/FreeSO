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
            new Color(0,0,255),

            new Color(74,89,66), //TS1 Dark Grass
            new Color(140,113,49), //TS1 Autumn Grass
            new Color(240,245,250), //TS1 Cloud
        };
        public static Color[] LightBrown = {
            new Color(157, 117, 65),
            new Color(196, 185, 162),
            new Color(126,96,70),
            new Color(240,245,250),
            new Color(0,0,255),

            new Color(90,69,41), //TS1 Dark Grass
            new Color(115,73,33), //TS1 Autumn Grass
            new Color(15,20,140), //TS1 Cloud
        };
        public static Color[] DarkGreen = {
            new Color(8, 52, 8),
            new Color(115, 109, 95),
            new Color(107,77,57),
            new Color(180,180,190),
            new Color(0,0,255),

            new Color(21,30,13), //new Color(41,52,33), //TS1 Dark Grass
            new Color(109,63,35), //new Color(123,85,41), //TS1 Autumn Grassi
            new Color(180,180,190), //TS1 Cloud
        };
        public static Color[] DarkBrown = {
            new Color(81, 60, 18),
            new Color(121, 114, 100),
            new Color(107,77,57),
            new Color(180,180,190),
            new Color(0,0,255),

            new Color(64,69,14), //new Color(74,69,24), //TS1 Dark Grass
            new Color(56,35,17), //new Color(82,52,24), //TS1 Autumn Grass
            new Color(15,20,140), //TS1 Cloud
        };

        public static int[] Heights =
        {
            6,
            1,
            1,
            1,
            0,

            6,
            6,
            1
        };

        public static float[] GrassDensity =
        {
            1f,
            1f,
            1f,
            1f,
            1f,

            0.8f,
            0.8f,
            1f,
        };
    }
}
