using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace TSOClient.Code.UI.Framework
{
    public class TextStyle
    {
        public static TextStyle Default;

        public float Scale = 1.0f;
        public SpriteFont Font;
        public Color Color = Color.Wheat;
    }

    [Flags]
    public enum TextAlignment
    {
        Left,
        Center,
        Right,
        Top,
        Middle,
        Bottom
    }
}
