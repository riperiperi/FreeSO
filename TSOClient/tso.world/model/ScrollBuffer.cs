using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.LotView.Model
{
    public class ScrollBuffer
    {
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
