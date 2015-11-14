/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using FSO.Common.Utils;
using FSO.Files.Formats.IFF.Chunks;
using FSO.LotView.Utils;
using FSO.Content.Model;
using Microsoft.Xna.Framework;
using FSO.LotView.Model;
using FSO.Content;

namespace FSO.LotView.Components
{
    public class NewFloorComponent : WorldComponent
    {

        private Texture2D[] ArchZBuffers;
        private static Rectangle FLOORDEST_NEAR = new Rectangle(5, 316, 127, 64);
        private static Rectangle FLOORDEST_MED = new Rectangle(3, 158, 63, 32);
        private static Rectangle FLOORDEST_FAR = new Rectangle(2, 79, 31, 16);
        public Blueprint blueprint;

        public override float PreferredDrawOrder
        {
            get
            {
                return 801.0f;
            }
        }

        public override void Draw(GraphicsDevice device, WorldState world)
        {
            if (ArchZBuffers == null) ArchZBuffers = TextureGenerator.GetWallZBuffer(device);

            var pxOffset = world.WorldSpace.GetScreenOffset();
            var floorContent = Content.Content.Get().WorldFloors;

            for (sbyte level = 1; level <= world.Level; level++)
            {
                for (short y = 0; y < blueprint.Height; y++)
                { //ill decide on a reasonable system for components when it's finished ok pls :(
                    for (short x = 0; x < blueprint.Height; x++)
                    {
                        var comp = blueprint.GetFloor(x, y, level);
                        if (comp.Pattern != 0)
                        {
                            var tilePosition = new Vector3(x, y, (level-1)*2.95f);
                            world._2D.OffsetPixel(world.WorldSpace.GetScreenFromTile(tilePosition) + pxOffset);
                            world._2D.OffsetTile(tilePosition);

                            var floor = GetFloorSprite(floorContent.Get(comp.Pattern), 0, world);
                            floor.Room = (ushort)blueprint.RoomMap[level-1][x + y * blueprint.Width];
                            if (floor.Pixel != null) world._2D.Draw(floor);
                        }
                        else if (world.BuildMode && level > 1 && blueprint.Supported[level-2][y*blueprint.Height+x])
                        {
                            var tilePosition = new Vector3(x, y, (level - 1) * 2.95f);
                            world._2D.OffsetPixel(world.WorldSpace.GetScreenFromTile(tilePosition) + pxOffset);
                            world._2D.OffsetTile(tilePosition);

                            var floor = GetAirSprite(world);
                            floor.Room = 65535;
                            if (floor.Pixel != null) world._2D.Draw(floor);
                        }
                    }
                }
            }

        }

        private _2DSprite GetAirSprite(WorldState world)
        {
            var _Sprite = new _2DSprite()
            {
                RenderMode = _2DBatchRenderMode.Z_BUFFER
            };
            var airTiles = TextureGenerator.GetAirTiles(world.Device);
            Texture2D sprite = null;
            switch (world.Zoom)
            {
                case WorldZoom.Far:
                    sprite = airTiles[2];
                    _Sprite.DestRect = FLOORDEST_FAR;
                    _Sprite.Depth = ArchZBuffers[14];
                    break;
                case WorldZoom.Medium:
                    sprite = airTiles[1];
                    _Sprite.DestRect = FLOORDEST_MED;
                    _Sprite.Depth = ArchZBuffers[13];
                    break;
                case WorldZoom.Near:
                    sprite = airTiles[0];
                    _Sprite.DestRect = FLOORDEST_NEAR;
                    _Sprite.Depth = ArchZBuffers[12];
                    break;
            }

            _Sprite.Pixel = sprite;
            _Sprite.SrcRect = new Microsoft.Xna.Framework.Rectangle(0, 0, _Sprite.Pixel.Width, _Sprite.Pixel.Height);

            return _Sprite;

        }

        private _2DSprite GetFloorSprite(Floor pattern, int rotation, WorldState world)
        {
            var _Sprite = new _2DSprite()
            {
                RenderMode = _2DBatchRenderMode.Z_BUFFER
            };
            if (pattern == null) return _Sprite;
            SPR2 sprite = null;
            bool vertFlip = world.Rotation == WorldRotation.TopRight || world.Rotation == WorldRotation.BottomRight;
            int bufOff = (vertFlip) ? 3 : 0;
            switch (world.Zoom)
            {
                case WorldZoom.Far:
                    sprite = pattern.Far;
                    _Sprite.DestRect = FLOORDEST_FAR;
                    _Sprite.Depth = ArchZBuffers[14+bufOff];
                    break;
                case WorldZoom.Medium:
                    sprite = pattern.Medium;
                    _Sprite.DestRect = FLOORDEST_MED;
                    _Sprite.Depth = ArchZBuffers[13 + bufOff];
                    break;
                case WorldZoom.Near:
                    sprite = pattern.Near;
                    _Sprite.DestRect = FLOORDEST_NEAR;
                    _Sprite.Depth = ArchZBuffers[12 + bufOff];
                    break;
            }
            if (sprite != null)
            {
                _Sprite.Pixel = world._2D.GetTexture(sprite.Frames[rotation]);
                _Sprite.SrcRect = new Microsoft.Xna.Framework.Rectangle(0, 0, _Sprite.Pixel.Width, _Sprite.Pixel.Height);

                if (vertFlip) _Sprite.FlipVertically = true;
                if ((int)world.Rotation > 1) _Sprite.FlipHorizontally = true;
            }

            return _Sprite;
        }
    }
}
