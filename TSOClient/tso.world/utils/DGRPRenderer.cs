/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using FSO.Files.Formats.IFF.Chunks;
using FSO.LotView.Model;

namespace FSO.LotView.Utils
{
    public class DGRPRendererItem 
    {
        public _2DSprite Sprite;
        public DGRPSprite DGRPSprite;
    }

    /// <summary>
    /// Used for rendering DGRPs in the world.
    /// </summary>
    public class DGRPRenderer
    {
        private DGRP DrawGroup;
        private List<DGRPRendererItem> Items = new List<DGRPRendererItem>();
        public uint DynamicSpriteFlags = 0x00000000;
        public ushort DynamicSpriteBaseID;
        public ushort NumDynamicSprites;

        public DGRPRenderer(DGRP group)
        {
            this.DrawGroup = group;
        }

        /// <summary>
        /// Gets or sets the DGRP used by this DGRPRenderer instance.
        /// </summary>
        public DGRP DGRP
        {
            get
            {
                return DrawGroup;
            }
            set
            {
                DrawGroup = value;
                _TextureDirty = true;
                _Dirty = true;
            }
        }

        private bool _Dirty = true;
        private bool _TextureDirty = true;

        private Direction _Direction;

        /// <summary>
        /// Gets or sets the direction that the DGRP is facing.
        /// </summary>
        public Direction Direction
        {
            get{
                return _Direction;
            }
            set{
                _Direction = value;
                _TextureDirty = true;
                _Dirty = true;
            }
        }

        private float RadianDirection
        {
            get
            {
                switch (_Direction)
                {
                    case Direction.NORTH:
                        return 0;
                    case Direction.EAST:
                        return (float)Math.PI / 2;
                    case Direction.SOUTH:
                        return (float)Math.PI;
                    case Direction.WEST:
                        return (float)Math.PI * 1.5f;
                    default:
                        return 0;
                }
            }
        }

        public void InvalidateRotation()
        {
            _TextureDirty = true;
            _Dirty = true;
        }

        public void InvalidateZoom()
        {
            _TextureDirty = true;
            _Dirty = true;
        }

        public void InvalidateScroll()
        {
            _Dirty = true;
        }

        public void Draw(WorldState world)
        {
            if (_Dirty)
            {
                if (_TextureDirty)
                {
                    Items.Clear();
                    var direction = (uint)_Direction;

                    /** Compute the direction **/
                    var image = DrawGroup.GetImage(direction, (uint)world.Zoom, (uint)world.Rotation);
                    if (image != null)
                    {
                        foreach (var sprite in image.Sprites)
                        {
                            var texture = world._2D.GetWorldTexture(sprite);
                            if (texture == null || texture.ZBuffer == null) { continue; }

                            var isDynamic = sprite.SpriteID >= DynamicSpriteBaseID && sprite.SpriteID < (DynamicSpriteBaseID + NumDynamicSprites);
                            if (isDynamic){
                                var dynamicIndex = (ushort)(sprite.SpriteID - DynamicSpriteBaseID);

                                var isVisible = (DynamicSpriteFlags & (0x1 << dynamicIndex)) > 0;
                                if (!isVisible){
                                    continue;
                                }
                            }
                            var item = new _2DSprite();
                            item.Pixel = texture.Pixel;
                            item.Depth = texture.ZBuffer;
                            if (texture.ZBuffer != null){
                                item.RenderMode = _2DBatchRenderMode.Z_BUFFER;
                                item.WorldPosition = sprite.ObjectOffset;
                            }else{
                                item.RenderMode = _2DBatchRenderMode.NO_DEPTH;
                            }

                            item.SrcRect = new Rectangle(0, 0, item.Pixel.Width, item.Pixel.Height);
                            item.DestRect = new Rectangle(0, 0, item.Pixel.Width, item.Pixel.Height);
                            item.FlipHorizontally = sprite.Flip;

                            Items.Add(new DGRPRendererItem { Sprite = item, DGRPSprite = sprite });
                        }
                    }

                    _TextureDirty = false;
                }

                foreach (var item in Items)
                {
                    var sprite = item.Sprite;
                    var dgrpSprite = item.DGRPSprite;
                    
                    var pxX = (world.WorldSpace.CadgeWidth / 2.0f) + dgrpSprite.SpriteOffset.X;
                    var pxY = (world.WorldSpace.CadgeBaseLine - sprite.Pixel.Height) + dgrpSprite.SpriteOffset.Y;

                    var centerRelative = dgrpSprite.ObjectOffset * new Vector3(1f/16f, 1f/16f, 1f/5f);
                    centerRelative = Vector3.Transform(centerRelative, Matrix.CreateRotationZ(RadianDirection));

                    var pxOff = world.WorldSpace.GetScreenFromTile(centerRelative);

                    sprite.DestRect.X = (int)(pxX+pxOff.X);
                    sprite.DestRect.Y = (int)(pxY+pxOff.Y);

                    sprite.WorldPosition = centerRelative * 3f;
                }

                _Dirty = false;
            }

            foreach (var item in Items)
            {
                world._2D.Draw(item.Sprite);
            }
        }
    }
}
