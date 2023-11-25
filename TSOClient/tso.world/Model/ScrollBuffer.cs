using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace FSO.LotView.Model
{
    public class ScrollBuffer
    {
        public static int BUFFER_PADDING = 512;

        public Vector2 GetScrollIncrement(Vector2 pxOffset, WorldState state)
        {
            var scrollSize = BUFFER_PADDING / state.PreciseZoom;
            return new Vector2((float)Math.Floor(pxOffset.X / scrollSize) * scrollSize, (float)Math.Floor(pxOffset.Y / scrollSize) * scrollSize);
        }

        public Texture2D Pixel;
        public Texture2D Depth;
        public Vector2 PxOffset;
        public Vector3 WorldPosition;

        public ScrollBuffer(Texture2D pixel, Texture2D depth, Vector2 px, Vector3 world)
        {
            Pixel = pixel;
            Depth = depth;
            PxOffset = px;
            WorldPosition = world;
        }
    }
}
