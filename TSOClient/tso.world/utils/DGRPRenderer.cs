/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using TSO.Files.formats.iff.chunks;
using tso.world.model;

namespace tso.world.utils
{
    public class DGRPRendererItem {
        public _2DSprite Sprite;
        public DGRPSprite DGRPSprite;
    }

    public class DGRPRenderer
    {
        private DGRP DrawGroup;
        private List<DGRPRendererItem> Items = new List<DGRPRendererItem>();
        public uint DynamicSpriteFlags = 0x00000000;
        public ushort DynamicSpriteBaseID;
        public ushort NumDynamicSprites;

        public DGRPRenderer(DGRP group){
            this.DrawGroup = group;
        }

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

        public void InvalidateRotation(){
            _TextureDirty = true;
            _Dirty = true;
        }

        public void InvalidateZoom(){
            _TextureDirty = true;
            _Dirty = true;
        }

        public void InvalidateScroll(){
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

                    sprite.DestRect.X = (int)(pxX);
                    sprite.DestRect.Y = (int)(pxY);
                    sprite.WorldPosition.X = dgrpSprite.ObjectOffset.X;
                    sprite.WorldPosition.Y = dgrpSprite.ObjectOffset.Y;
                    sprite.WorldPosition.Z = dgrpSprite.ObjectOffset.Z;
                }

                _Dirty = false;
            }

            foreach (var item in Items){
                world._2D.Draw(item.Sprite);
            }
        }

        /*
        private WorldRotation _Direction;
        public WorldRotation Direction
        {
            get{
                return _Direction;
            }
            set{
                _Direction = value;
            }
        }*/
    }
}
