using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace tso.world.utils
{
    public class _2DSprite
    {
        public _2DBatchRenderMode RenderMode;
        public Texture2D Pixel;
        public Texture2D Depth;
        public Texture2D Mask;
        public Vector3 TilePosition;
        public Vector3 WorldPosition;
        public short ObjectID; //used for mouse hit test render mode

        public Rectangle SrcRect;
        public Rectangle DestRect;

        //For internal use, do not set this
        public int DrawOrder;
        public bool FlipHorizontally;
        public bool FlipVertically;

        public Rectangle AbsoluteDestRect;
        public Vector3 AbsoluteWorldPosition;
        public Vector3 AbsoluteTilePosition;

    }
}
