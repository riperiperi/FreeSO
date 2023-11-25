using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using FSO.Common.Model;

namespace FSO.LotView.Utils
{
    /// <summary>
    /// Represents a 2D sprite in the game.
    /// </summary>
    public class _2DSprite
    {
        public _2DBatchRenderMode RenderMode;
        public Texture2D Pixel;
        public Texture2D Depth;
        public Texture2D Mask;
        public Vector3 WorldPosition;
        public Single ObjectID; //used for mouse hit test render mode
        public ushort Room = 0xFFFF; //room to use for ambient light
        public sbyte Floor = 1;

        public Rectangle SrcRect;
        public Rectangle DestRect;

        //For internal use, do not set this
        public int DrawOrder; //unused?
        public bool FlipHorizontally;
        public bool FlipVertically;

        public Rectangle AbsoluteDestRect;
        public Vector3 AbsoluteWorldPosition; //used for z buffer calculation

        public void Repurpose()
        {
            Pixel = null;
            Depth = null;
            Mask = null;
            WorldPosition = new Vector3();
            ObjectID = 0;
            Room = 0xFFFF;

            //rects are written always by sprite drawer
            FlipHorizontally = false;
            FlipVertically = false;
        }
    }

    public class _2DSpriteGroup
    {
        public IntersectRectTree SprRectangles;
        public Dictionary<_2DBatchRenderMode, List<_2DSprite>> Sprites;

        public _2DSpriteGroup(bool nonIntersect)
        {
            if (nonIntersect) SprRectangles = new IntersectRectTree();
            Sprites = new Dictionary<_2DBatchRenderMode, List<_2DSprite>>();
            Sprites.Add(_2DBatchRenderMode.NO_DEPTH, new List<_2DSprite>());
            Sprites.Add(_2DBatchRenderMode.RESTORE_DEPTH, new List<_2DSprite>());
            Sprites.Add(_2DBatchRenderMode.WALL, new List<_2DSprite>());
            Sprites.Add(_2DBatchRenderMode.Z_BUFFER, new List<_2DSprite>());
            Sprites.Add(_2DBatchRenderMode.FLOOR, new List<_2DSprite>());
        }
    }
}
