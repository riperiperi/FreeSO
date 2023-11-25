using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Common.Rendering
{
    public class TextureInfo
    {
        public Vector2 UVScale;
        public Point Size;
        public Point Diff;

        public TextureInfo() { }

        public TextureInfo(Texture2D tex, int width, int height)
        {
            Size = new Point(width, height);
            Diff = new Point(tex.Width, tex.Height) - Size;
            UVScale = Size.ToVector2() / new Vector2(tex.Width, tex.Height);
        }
    }
}
