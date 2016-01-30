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
    public class FloorComponent : WorldComponent
    {

        private Texture2D[] ArchZBuffers;
        private static Rectangle FLOORDEST_NEAR = new Rectangle(5, 316, 127, 64);
        private static Rectangle FLOORDEST_MED = new Rectangle(3, 158, 63, 32);
        private static Rectangle FLOORDEST_FAR = new Rectangle(2, 79, 31, 16);
        public Blueprint blueprint;

        private static Point[] PoolDirections =
        {
            new Point(0, -1),
            new Point(1, -1),
            new Point(1, 0),
            new Point(1, 1),
            new Point(0, 1),
            new Point(-1, 1),
            new Point(-1, 0),
            new Point(-1, -1),
        };

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
                    for (short x = 0; x < blueprint.Width; x++)
                    {
                        var comp = blueprint.GetFloor(x, y, level);
                        if (comp.Pattern != 0)
                        {
                            ushort room = (ushort)blueprint.RoomMap[level - 1][x + y * blueprint.Width];
                            var tilePosition = new Vector3(x, y, (level-1)*2.95f);
                            world._2D.OffsetPixel(world.WorldSpace.GetScreenFromTile(tilePosition) + pxOffset);
                            world._2D.OffsetTile(tilePosition);

                            if (comp.Pattern > 65534)
                            {
                                //determine adjacent pool tiles
                                int poolAdj = 0;
                                for (int i=0; i<PoolDirections.Length; i++)
                                {
                                    var testTile = new Point(x, y) + PoolDirections[i];
                                    if ((testTile.X < 0 || testTile.X >= blueprint.Width) || (testTile.Y < 0 || testTile.Y >= blueprint.Height)) continue;
                                    if (blueprint.GetFloor((short)testTile.X, (short)testTile.Y, level).Pattern == comp.Pattern) poolAdj |= 1 << i;
                                }

                                var adj = RotatePoolSegs((PoolSegments)poolAdj, (int)world.Rotation);

                                //get and draw the base tile

                                int spriteNum = 0;
                                if ((adj & PoolSegments.TopRight) > 0) spriteNum |= 1;
                                if ((adj & PoolSegments.TopLeft) > 0) spriteNum |= 2;
                                if ((adj & PoolSegments.BottomLeft) > 0) spriteNum |= 4;
                                if ((adj & PoolSegments.BottomRight) > 0) spriteNum |= 8;

                                var _Sprite = new _2DSprite() { RenderMode = _2DBatchRenderMode.Z_BUFFER };
                                SPR2 sprite = null;
                                switch (world.Zoom)
                                {
                                    case WorldZoom.Far:
                                        sprite = floorContent.GetGlobalSPR((ushort)(0x400+spriteNum));
                                        _Sprite.DestRect = FLOORDEST_FAR;
                                        _Sprite.Depth = ArchZBuffers[14];
                                        break;
                                    case WorldZoom.Medium:
                                        sprite = floorContent.GetGlobalSPR((ushort)(0x410 + spriteNum));
                                        _Sprite.DestRect = FLOORDEST_MED;
                                        _Sprite.Depth = ArchZBuffers[13];
                                        break;
                                    case WorldZoom.Near:
                                        sprite = floorContent.GetGlobalSPR((ushort)(0x420 + spriteNum));
                                        _Sprite.DestRect = FLOORDEST_NEAR;
                                        _Sprite.Depth = ArchZBuffers[12];
                                        break;
                                }
                                _Sprite.Pixel = world._2D.GetTexture(sprite.Frames[0]);
                                _Sprite.SrcRect = new Microsoft.Xna.Framework.Rectangle(0, 0, _Sprite.Pixel.Width, _Sprite.Pixel.Height);
                                _Sprite.Room = room;
                                world._2D.Draw(_Sprite);

                                //draw any corners on top

                                PoolSegments[] CornerChecks =
                                {
                                    (PoolSegments.TopLeft | PoolSegments.TopRight),
                                    (PoolSegments.TopLeft | PoolSegments.BottomLeft),
                                    (PoolSegments.BottomLeft | PoolSegments.BottomRight),
                                    (PoolSegments.TopRight | PoolSegments.BottomRight)
                                };


                                if ((adj & CornerChecks[0]) == CornerChecks[0] && (adj & PoolSegments.Top) == 0)
                                {
                                    //top corner
                                    var tcS = new _2DSprite() { RenderMode = _2DBatchRenderMode.Z_BUFFER };
                                    //base sprite position on base tile
                                    sprite = floorContent.GetGlobalSPR((ushort)(0x430 + ((int)world.Zoom-1)*4));
                                    tcS.Pixel = world._2D.GetTexture(sprite.Frames[0]);
                                    tcS.DestRect = new Rectangle(_Sprite.DestRect.Center.X - tcS.Pixel.Width / 2, _Sprite.DestRect.Y, tcS.Pixel.Width, tcS.Pixel.Height);
                                    tcS.SrcRect = new Microsoft.Xna.Framework.Rectangle(0, 0, tcS.Pixel.Width, tcS.Pixel.Height);
                                    tcS.Depth = ArchZBuffers[21 + (3 - (int)world.Zoom)];
                                    tcS.Room = room;
                                    world._2D.Draw(tcS);
                                }

                                if ((adj & CornerChecks[1]) == CornerChecks[1] && (adj & PoolSegments.Left) == 0)
                                {
                                    //left corner
                                    var tcS = new _2DSprite() { RenderMode = _2DBatchRenderMode.Z_BUFFER };
                                    //base sprite position on base tile
                                    sprite = floorContent.GetGlobalSPR((ushort)(0x431 + ((int)world.Zoom - 1) * 4));
                                    tcS.Pixel = world._2D.GetTexture(sprite.Frames[0]);
                                    tcS.DestRect = new Rectangle(_Sprite.DestRect.X, _Sprite.DestRect.Center.Y+1 - tcS.Pixel.Height/2, tcS.Pixel.Width, tcS.Pixel.Height);
                                    tcS.SrcRect = new Microsoft.Xna.Framework.Rectangle(0, 0, tcS.Pixel.Width, tcS.Pixel.Height);
                                    tcS.Depth = ArchZBuffers[24 + (3 - (int)world.Zoom)];
                                    tcS.Room = room;
                                    world._2D.Draw(tcS);
                                }

                                if ((adj & CornerChecks[2]) == CornerChecks[2] && (adj & PoolSegments.Bottom) == 0)
                                {
                                    //bottom corner
                                    var tcS = new _2DSprite() { RenderMode = _2DBatchRenderMode.Z_BUFFER };
                                    //base sprite position on base tile
                                    sprite = floorContent.GetGlobalSPR((ushort)(0x432 + ((int)world.Zoom - 1) * 4));
                                    tcS.Pixel = world._2D.GetTexture(sprite.Frames[0]);
                                    tcS.DestRect = new Rectangle(_Sprite.DestRect.Center.X - tcS.Pixel.Width / 2, _Sprite.DestRect.Bottom-tcS.Pixel.Height, tcS.Pixel.Width, tcS.Pixel.Height);
                                    tcS.SrcRect = new Microsoft.Xna.Framework.Rectangle(0, 0, tcS.Pixel.Width, tcS.Pixel.Height);
                                    tcS.Depth = ArchZBuffers[27 + (3 - (int)world.Zoom)];
                                    tcS.Room = room;
                                    world._2D.Draw(tcS);
                                }

                                if ((adj & CornerChecks[3]) == CornerChecks[3] && (adj & PoolSegments.Right) == 0)
                                {
                                    //right corner
                                    var tcS = new _2DSprite() { RenderMode = _2DBatchRenderMode.Z_BUFFER };
                                    //base sprite position on base tile
                                    sprite = floorContent.GetGlobalSPR((ushort)(0x433 + ((int)world.Zoom - 1) * 4));
                                    tcS.Pixel = world._2D.GetTexture(sprite.Frames[0]);
                                    tcS.DestRect = new Rectangle(_Sprite.DestRect.Right-tcS.Pixel.Width, _Sprite.DestRect.Center.Y+1 - tcS.Pixel.Height / 2, tcS.Pixel.Width, tcS.Pixel.Height);
                                    tcS.SrcRect = new Microsoft.Xna.Framework.Rectangle(0, 0, tcS.Pixel.Width, tcS.Pixel.Height);
                                    tcS.Depth = ArchZBuffers[24 + (3 - (int)world.Zoom)];
                                    tcS.Room = room;
                                    world._2D.Draw(tcS);
                                }

                            }
                            else
                            {
                                var floor = GetFloorSprite(floorContent.Get(comp.Pattern), 0, world);
                                floor.Room = room;
                                if (floor.Pixel != null) world._2D.Draw(floor);
                            }
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

        internal PoolSegments RotatePoolSegs(PoolSegments ps, int rotate)
        {
            int poolSides = (int)ps;
            int rotPart = ((poolSides << (rotate*2)%8) & 255) | ((poolSides & 255) >> (8 - rotate *2)%8);
            return (PoolSegments)rotPart;
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

    [Flags]
    public enum PoolSegments
    {
        TopRight = 1,
        Right = 2,
        BottomRight = 4,
        Bottom = 8,
        BottomLeft = 16,
        Left = 32,
        TopLeft = 64,
        Top = 128
    }
}
