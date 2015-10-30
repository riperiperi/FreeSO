using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.IDE.EditorComponent.Model
{
    public static class PGroupStyles
    {
        public static Dictionary<PrimitiveGroup, PrimitiveStyle> ByType = new Dictionary<PrimitiveGroup, PrimitiveStyle>
        {
            { PrimitiveGroup.Subroutine, new PrimitiveStyle(new Color(0xEE,0xEE,0xEE), new Color(0x21,0x21,0x21), new Color(0x44,0x44,0x44), 1.0f) },
            { PrimitiveGroup.Control, new PrimitiveStyle(new Color(0xEF,0xBF,0x00), new Color(0x66,0x4C,0x00), new Color(0x66,0x4C,0x00), 0.2f) },
            { PrimitiveGroup.Debug, new PrimitiveStyle(new Color(0xFF,0x73,0x73), new Color(0x40,0x00,0x00), new Color(0x66,0x00,0x00), 0.2f) },
            { PrimitiveGroup.Math, new PrimitiveStyle(new Color(0x46,0x8C,0x00), new Color(0x00,0x66,0x33), Color.White, 0.1f) },
            { PrimitiveGroup.Sim, new PrimitiveStyle(new Color(0xFF,0x97,0xFD), new Color(0x4C,0x00,0x66), new Color(0x69,0x00,0x8C), 0.2f) },
            { PrimitiveGroup.Object, new PrimitiveStyle(new Color(0x69,0x00,0x8C), new Color(0x4C,0x00,0x66), Color.White, 0.1f) },
            { PrimitiveGroup.Looks, new PrimitiveStyle(new Color(0x73,0xDC,0xFF), new Color(0x00,0x33,0x66), new Color(0x00,0x69,0x8C), 0.2f) },
            { PrimitiveGroup.Position, new PrimitiveStyle(new Color(0x00,0x59,0xB2), new Color(0x00,0x20,0x40), Color.White, 0.1f) },
            { PrimitiveGroup.TSO, new PrimitiveStyle(new Color(0x8C,0x00,0x00), new Color(0x3F,0x00,0x00), Color.White, 0.1f) },
            { PrimitiveGroup.Unknown, new PrimitiveStyle(new Color(0xAA,0xAA,0xAA), new Color(0x21,0x21,0x21), new Color(0x44,0x44,0x44), 0.2f) },
            { PrimitiveGroup.Placement, new PrimitiveStyle(Color.White*0.2f, new Color(0x22,0x22,0x22), new Color(0x44,0x44,0x44), 1f) },
        };
    }

    public class PrimitiveStyle
    {
        public Color Background;
        public Color Title;
        public Color Body;
        public float DiagBrightness; //generally 0.1f for darker primitives, 0.2 for normal

        public PrimitiveStyle(Color bg, Color title, Color fg, float diagBrightness)
        {
            Background = bg;
            Title = title;
            Body = fg;
            DiagBrightness = diagBrightness;
        }
    }
}
