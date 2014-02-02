using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace TSOClient.Code.Rendering.Lot.Framework
{
    public class HouseBatchSprite
    {
        public HouseBatchRenderMode RenderMode { get; set; }
        public Texture2D Pixel { get; set; }
        public Texture2D Depth { get; set; }
        public Vector2 TilePosition { get; set; }

        public Rectangle SrcRect { get; set; }
        public Rectangle DestRect { get; set; }

        //For internal use, do not set this
        public int DrawOrder { get; set; }
    }
}
