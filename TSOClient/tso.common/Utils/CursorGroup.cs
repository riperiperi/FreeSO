using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace FSO.Common.Utils
{
    public struct CursorGroup
    {
        public Texture2D Texture;
        public MouseCursor MouseCursor;
        public Point Point;

        public CursorGroup(MouseCursor mouseCursor)
        {
            Texture = null;
            MouseCursor = mouseCursor;
            Point = new Point();
        }

        public CursorGroup(Texture2D texture, Point point)
        {
            Texture = texture;
            MouseCursor = MouseCursor.FromTexture2D(texture, point.X, point.Y);
            Point = point;
        }
    }
}
