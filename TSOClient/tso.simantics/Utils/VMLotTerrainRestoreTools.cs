using FSO.Content.Model;
using FSO.LotView.Components;
using FSO.LotView.Model;
using FSO.SimAntics.Engine.TSOTransaction;
using FSO.SimAntics.Marshals.Hollow;
using FSO.SimAntics.Model;
using FSO.SimAntics.NetPlay.Drivers;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.Utils
{
    public static class VMLotTerrainRestoreTools
    {

        #region Road Surroundings Map Data
        //    /\
        //   /  \
        //  /    \
        // /      \
        //+y      +x

        //key: 9 = black tarmac, 10 = vert line, 11 = horiz line, 12 = tarmac

        public static short[] TopLeftRoadCrossing = //7x7
        {
            5, 5,

            9,  9,  9,  0,  12,
            10, 10, 10, 0,  12,
            11, 11, 11, 12, 12, 
            10, 10, 10, 0,  12, 
            9,  9,  9,  0,  12, 
        };

        public static short[] TopRightRoadCrossing = //7x7
        {
            5, 5,

            9,  11, 10, 11, 9,
            9,  11, 10, 11, 9,
            9,  11, 10, 11, 9,
            0,  0,  12, 0,  0,
            12, 12, 12, 12, 12,
        };

        public static short[] BottomRightRoadCrossing =
        {
            6, 5,

            12, 0,  9,  9,  9,  9,
            12, 0,  10, 10, 10, 10,
            12, 12, 11, 11, 11, 11,
            12, 0,  10, 10, 10, 10,
            12, 0,  9,  9,  9,  9,
        };

        public static short[] BottomLeftRoadCrossing =
        {
            5, 6,

            12, 12, 12, 12, 12, 
            0,  0,  12, 0,  0,
            9,  11, 10, 11, 9,
            9,  11, 10, 11, 9,
            9,  11, 10, 11, 9, 
            9,  11, 10, 11, 9, 
        };

        public static short[] TopRoadInnerCorner = //7x7
        {
            7, 7,

            9,  9,  9,  11, 10, 11, 9,
            9,  9,  9,  11, 10, 11, 9,
            9,  9,  9,  11, 10, 11, 9,
            10, 10, 10, 0,  12, 0,  0,
            11, 11, 11, 12, 12, 12, 12,
            10, 10, 10, 0,  12, 0,  0,
            9,  9,  9,  0,  12, 0,  0
        };

        public static short[] RightRoadInnerCorner =
        {
            8, 7,

            9,  11, 10, 11, 9,  9,  9,  9,
            9,  11, 10, 11, 9,  9,  9,  9,
            9,  11, 10, 11, 9,  9,  9,  9,
            0,  0,  12, 0,  10, 10, 10, 10,
            12, 12, 12, 12, 11, 11, 11, 11,
            0,  0,  12, 0,  10, 10, 10, 10,
            0,  0,  12, 0,  9,  9,  9,  9,
        };

        public static short[] BottomRoadInnerCorner =
        {
            8, 8,

            0,  0,  12, 0,  9,  9,  9,  9, 
            0,  0,  12, 0,  10, 10, 10, 10,
            12, 12, 12, 12, 11, 11, 11, 11,
            0,  0,  12, 0,  10, 10, 10, 10,
            9,  11, 10, 11, 9,  9,  9,  9,
            9,  11, 10, 11, 9,  9,  9,  9,
            9,  11, 10, 11, 9,  9,  9,  9,
            9,  11, 10, 11, 9,  9,  9,  9,
        };

        public static short[] LeftRoadInnerCorner =
        {
            7, 8,

            9,  9,  9,  0,  12, 0,  0,
            10, 10, 10, 0,  12, 0,  0,
            11, 11, 11, 12, 12, 12, 12,
            10, 10, 10, 0,  12, 0,  0,
            9,  9,  9,  11, 10, 11, 9,
            9,  9,  9,  11, 10, 11, 9,
            9,  9,  9,  11, 10, 11, 9,
            9,  9,  9,  11, 10, 11, 9,
        };

        public static short[] TopLeftRoadTile =
        {
            5, 4,

            9, 9, 9, 0, 12,
            9, 9, 9, 0, 12,
            9, 9, 9, 0, 12,
            9, 9, 9, 0, 12,
        };

        public static short[] TopRightRoadTile =
        {
            4, 5,

            9, 9, 9, 9,
            9, 9, 9, 9,
            9, 9, 9, 9,
            0, 0, 0, 0,
            12,12,12,12,
        };

        public static short[] BottomRightRoadTile =
        {
            6, 4,

            12, 0,  9,  9,  9,  11,
            12, 0,  9,  9,  9,  11,
            12, 0,  9,  9,  9,  11,
            12, 0,  9,  9,  9,  9,
        };

        public static short[] BottomLeftRoadTile =
        {
            4, 6,

            12, 12, 12, 12,
            0,  0,  0,  0,
            9,  9,  9,  9,
            9,  9,  9,  9,
            9,  9,  9,  9,
            10, 10, 10, 9,
        };

        public static short[] TopRoadCorner =
        {
            5, 5,

            9,  9,  9,  0,  12,
            9,  9,  9,  0,  12,
            9,  9,  9,  0,  12,
            0,  0,  0,  0,  12,
            12, 12, 12, 12, 12,
        };

        public static short[] RightRoadCorner =
        {
            6, 5,

            12, 0,  9,  9,  9,  9,
            12, 0,  9,  9,  9,  9,
            12, 0,  9,  9,  9,  9,
            12, 0,  0,  0,  0,  0,
            12, 12, 12, 12, 12, 12,
        };

        public static short[] BottomRoadCorner =
        {
            6, 6,

            12, 12, 12, 12, 12, 12,
            12, 0,  0,  0,  0,  0,
            12, 0,  9,  9,  9,  9,
            12, 0,  9,  9,  9,  9,
            12, 0,  9,  9,  9,  9,
            12, 0,  9,  9,  9,  9,
        };

        public static short[] LeftRoadCorner =
        {
            5, 6,

            12, 12, 12, 12, 12,
            0,  0,  0,  0,  12,
            9,  9,  9,  0,  12,
            9,  9,  9,  0,  12,
            9,  9,  9,  0,  12,
            9,  9,  9,  0,  12,
        };

        //terrain pattern to apply beneath car portals (when present)
        public static byte[] CarDirtRoad =
        {
            21, 3,

            2,   4,   6,   8,   12,  16,  32,  64,  128, 196, 225, 196, 128, 64,  32,  16,  12,  8,   6,   4,   2,
            0,   2,   4,   6,   8,   12,  16,  32,  64,  96,  64,  96,  64,  32,  16,  12,  8,   6,   4,   2,   0,
            2,   4,   6,   8,   12,  16,  32,  64,  128, 196, 225, 196, 128, 64,  32,  16,  12,  8,   6,   4,   2,
        };

        #endregion

        #region Water Surroundings Map Data

        private const short W = -2;

        public static short[] TopWaterCorner =
        {
            6, 6,
            W, W, W, W, W, W,
            W, W, W, W, W, W,
            W, W, W, W, 0, 0,
            W, W, W, 0, 0, 0,
            W, W, 0, 0, 0, 0,
            W, W, 0, 0, 0, 0,
        };

        public static short[] RightWaterCorner =
        {
            6, 6,
            W, W, W, W, W, W,
            W, W, W, W, W, W,
            0, 0, W, W, W, W,
            0, 0, 0, W, W, W,
            0, 0, 0, 0, W, W,
            0, 0, 0, 0, W, W,
        };

        public static short[] BottomWaterCorner =
        {
            6, 6,
            0, 0, 0, 0, W, W,
            0, 0, 0, 0, W, W,
            0, 0, 0, W, W, W,
            0, 0, W, W, W, W,
            W, W, W, W, W, W,
            W, W, W, W, W, W,
        };

        public static short[] LeftWaterCorner =
        {
            6, 6,
            W, W, 0, 0, 0, 0,
            W, W, 0, 0, 0, 0,
            W, W, W, 0, 0, 0,
            W, W, W, W, 0, 0,
            W, W, W, W, W, W,
            W, W, W, W, W, W,
        };

        public static short[] WaterLineSegments =
        { //length, thickness
        //6, 0,
        //7, 1,
        13, 1,
        10, 2,
        31, 3,
        10, 2,
        13, 1
        //7, 1,
        //6, 0
        }; //adds up to 77

        #endregion

        public static uint[][] BlankTerrainObjects =
        new uint[][]{
            //grass:
            new uint[]{
                0x51474702, //off-white spruce
                0x5F421524, //trees - pine
                0x8C7F5607, //trees - mulberry
                0x8BA56284, //trees - fir
                0x374B44F3, //trees - birch
                0x658E9089, //trees - crape myrtle
                0x2B83B971, //trees - apple
                0x154522AF, //shrub - juniper bush
                0xD5CA5425, //shrub - rose bush
                0x15460FCA, //shrub - sword fern
                0x3659DCA2, //flamingo (RARE)
            },

            //beach:
            new uint[]{
                0xD9BD21CD, //cabana palm
                0x65627AE4, //small palm
                0x65627AE4,
                0x65627AE4,
                0x6779ED37, //palm 12 foot
                0x6779ED37,
                0x6779ED37,
                0x263FAA5F, //century plant
                0x28EAE0F9, //beach bench (RARE)
            },

            //desert:
            new uint[]{
                0x70E7B9A5, //prickly pear plant
                0x70E7B9A5,
                0x70E7B9A5,
                0xD297087F, //agave bloom
                0x2FF38F53, //cactus
                0x2FF38F53,
                0x2FF38F53,
                0x2163F6F7, //tumbleweed
                0xD8F649FF, //joshua tree
                0x21E65BBF, //trash can barrel (RARE)
            },

            //snow:
            new uint[]{
                0x56040A7B, //pine with snow
                0x563504AC, //fir with snow
                0x56D56301, //spruce with snow
                0x378AB380, //large fir
                0x37A2A8CA, //large pine
                0x365ACF98, //large spruce
                0x10080233, //holiday snowman (RARE)
            },

            //water:
            new uint[]{
                0x319DE003, //koi boat (RARE)
            }
        };

        public static void StampTilemap(VMArchitecture arch, short[] tilemap, short x, short y, sbyte level)
        {
            StampTilemap(arch, tilemap, x, y, level, false);
        }

        public static void StampTilemap(VMArchitecture arch, short[] tilemap, short x, short y, sbyte level, bool skipZero)
        {
            var width = tilemap[0];
            var height = tilemap[1];

            for (int i=2; i<tilemap.Length; i++)
            {
                if (skipZero && tilemap[i] == 0) continue;
                var xo = (i - 2) % width;
                var yo = (i - 2) / width;
                if (x + xo >= arch.Width - 1 || y + yo >= arch.Height - 1) continue;
                arch.SetFloor((short)(x + xo), (short)(y + yo), level, new LotView.Model.FloorTile() { Pattern = (ushort)tilemap[i] }, true);
            }
        }

        public static void FillTiles(VMArchitecture arch, short tile, short x, short y, sbyte level, int xtimes, int ytimes)
        {
            for (int xo = 0; xo < xtimes; xo++)
            {
                var x2 = (short)(x + xo);
                if (x2 < 1 || x2 >= arch.Width-1) continue;
                for (int yo = 0; yo < ytimes; yo++)
                {
                    var y2 = (short)(y + yo);
                    if (y2 < 1 || y2 >= arch.Height-1) continue;
                    arch.SetFloor(x2, y2, level, new FloorTile() { Pattern = (ushort)tile }, true);
                }
            }
        }

        public static void FillTileLine(VMArchitecture arch, short tile, short x, short y, sbyte level, short[] line, bool horiz)
        {
            for (int i=0; i<line.Length; i+=2)
            {
                var segLen = line[i];
                var thickness = line[i + 1];
                if (horiz) {
                    FillTiles(arch, tile, x, (short)(y - thickness), level, segLen, thickness * 2 +1);
                    x += segLen;
                } else
                {
                    FillTiles(arch, tile, (short)(x - thickness), y, level, thickness * 2+1, segLen);
                    y += segLen;
                }
            }
        }


        public static void RepeatTilemap(VMArchitecture arch, short[] tilemap, short x, short y, sbyte level, int xtimes, int ytimes)
        {
            for (int xo = 0; xo< xtimes; xo++)
            {
                for (int yo=0; yo< ytimes; yo++)
                {
                    StampTilemap(arch, tilemap, (short)(x + xo * tilemap[0]), (short)(y + yo * tilemap[1]), level);
                }
            }
        }

        public static void StampTerrainmap(VMArchitecture arch, byte[] tilemap, short x, short y, Vector2 xInc, Vector2 yInc)
        {
            var width = tilemap[0];
            var height = tilemap[1];

            for (int i = 2; i < tilemap.Length; i++)
            {
                if (tilemap[i] == 0) continue;
                var src = new Vector2((i - 2) % width, (i - 2) / width);
                var dst = src.X * xInc + src.Y * yInc;
                var xo = (int)(dst.X);
                var yo = (int)(dst.Y);
                if (x + xo >= arch.Width - 1 || y + yo >= arch.Height - 1) continue;

                var mult = tilemap[i] / 255f;

                var archOff = (y + yo) * arch.Width + (x + xo);
                arch.Terrain.GrassState[archOff] = (byte)(((1-mult) * arch.Terrain.GrassState[archOff] + mult * 255));
            }
        }

        public static void RestoreTerrain(VM vm)
        {
            //take center of lotstate
            RestoreTerrain(vm, vm.TSOState.Terrain.BlendN[1, 1], vm.TSOState.Terrain.Roads[1, 1]);
        }

        public static void RestoreTerrain(VM vm, TerrainBlend blend, byte roads)
        {
            var arch = vm.Context.Architecture;
            arch.DisableClip = true;

            var baseB = blend.Base;
            arch.Terrain.LightType = (baseB == TerrainType.WATER) ? TerrainType.SAND : blend.Base;
            arch.Terrain.DarkType = (blend.Blend == TerrainType.WATER) ? blend.Base : blend.Blend;
            arch.Terrain.GenerateGrassStates();

            //clear all previous roads/sea
            VMArchitectureTools.FloorPatternRect(arch, new Rectangle(0, 0, arch.Width, 5), 0, 0, 1);
            VMArchitectureTools.FloorPatternRect(arch, new Rectangle(arch.Width - 7, 0, 7, arch.Height), 0, 0, 1);
            VMArchitectureTools.FloorPatternRect(arch, new Rectangle(0, arch.Height - 7, arch.Width, 7), 0, 0, 1);
            VMArchitectureTools.FloorPatternRect(arch, new Rectangle(0, 0, 5, arch.Height), 0, 0, 1);

            if (baseB == TerrainType.WATER)
            {
                //...
                VMArchitectureTools.FloorPatternRect(arch, new Rectangle(1, 1, arch.Width - 3, arch.Height - 3), 0, 65534, 1);
            }

            //blend flags start at top left, then go clockwise. (top right, bottom right..)

            if ((blend.WaterFlags & 1) > 0) FillTileLine(arch, W, 0, 0, 1, WaterLineSegments, false);
            if ((blend.WaterFlags & 4) > 0) FillTileLine(arch, W, 0, 0, 1, WaterLineSegments, true);
            if ((blend.WaterFlags & 16) > 0) FillTileLine(arch, W, (short)(arch.Width - 1), 0, 1, WaterLineSegments, false);
            if ((blend.WaterFlags & 64) > 0) FillTileLine(arch, W, 0, (short)(arch.Height - 1), 1, WaterLineSegments, true);

            if ((blend.WaterFlags & 2) > 0) FillTiles(arch, W, 1, 1, 1, 1, 1);
            if ((blend.WaterFlags & 8) > 0) FillTiles(arch, W, (short)(arch.Width - 2), 1, 1, 1, 1);
            if ((blend.WaterFlags & 32) > 0) FillTiles(arch, W, (short)(arch.Width - 2), (short)(arch.Height - 2), 1, 1, 1);
            if ((blend.WaterFlags & 128) > 0) FillTiles(arch, W, 1, (short)(arch.Height - 2), 1, 1, 1);

            if ((blend.WaterFlags & 5) == 5) StampTilemap(arch, TopWaterCorner, 1, 1, 1);
            if ((blend.WaterFlags & 20) == 20) StampTilemap(arch, RightWaterCorner, (short)(arch.Width - 7), 1, 1);
            if ((blend.WaterFlags & 80) == 80) StampTilemap(arch, BottomWaterCorner, (short)(arch.Width - 7), (short)(arch.Height - 7), 1);
            if ((blend.WaterFlags & 65) == 65) StampTilemap(arch, LeftWaterCorner, 1, (short)(arch.Height - 7), 1);

            /*

            if ((blend.WaterFlags & 1) > 0) VMArchitectureTools.FloorPatternRect(arch, new Rectangle(1, 1, 4, arch.Height - 2), 0, 65534, 1);
            if ((blend.WaterFlags & 2) > 0) VMArchitectureTools.FloorPatternRect(arch, new Rectangle(1, 1, arch.Width-2, 4), 0, 65534, 1);
            if ((blend.WaterFlags & 4) > 0) VMArchitectureTools.FloorPatternRect(arch, new Rectangle(arch.Width-5, 1, 4, arch.Height - 2), 0, 65534, 1);
            if ((blend.WaterFlags & 8) > 0) VMArchitectureTools.FloorPatternRect(arch, new Rectangle(1, arch.Height-5, arch.Width-2, 4), 0, 65534, 1);

            */

            //hard blends into the next terrain type 

            FillTerrainRect(arch, new Rectangle(0, 0, 1, arch.Height - 1), (byte)(((blend.AdjFlags & 1) > 0) ? 255 : 0));
            FillTerrainRect(arch, new Rectangle(0, 0, arch.Width - 1, 1), (byte)(((blend.AdjFlags & 4) > 0) ? 255 : 0));
            FillTerrainRect(arch, new Rectangle(arch.Width - 2, 0, 1, arch.Height - 1), (byte)(((blend.AdjFlags & 16) > 0) ? 255 : 0));
            FillTerrainRect(arch, new Rectangle(0, arch.Height - 2, arch.Width - 1, 1), (byte)(((blend.AdjFlags & 64) > 0) ? 255 : 0));

            /*
            FillTerrainRect(arch, new Rectangle(0, 0, 1, 1), (byte)(((blend.AdjFlags & 2) > 0) ? 255 : 0));
            FillTerrainRect(arch, new Rectangle(arch.Width - 2, 0, 1, 1), (byte)(((blend.AdjFlags & 8) > 0) ? 255 : 0));
            FillTerrainRect(arch, new Rectangle(arch.Width - 2, arch.Height - 2, 1, 1), (byte)(((blend.AdjFlags & 32) > 0) ? 255 : 0));
            FillTerrainRect(arch, new Rectangle(0, arch.Height - 2, 1, 1), (byte)(((blend.AdjFlags & 128) > 0) ? 255 : 0));
            */

            //smooth blends into the next terrain type

            ApplyTerrainBlend(arch, RotateByte(blend.AdjFlags, 0), new Rectangle(0, 0, arch.Width - 1, 24), 255, 0, -11,
                new Point[] { new Point(0, 0), new Point(arch.Width - 1, 0) },
                new float[] { (135f / 180f) * (float)Math.PI, (225f / 180f) * (float)Math.PI },
                new float[] { (15f / 180f) * (float)Math.PI, (-15f / 180f) * (float)Math.PI });

            ApplyTerrainBlend(arch, RotateByte(blend.AdjFlags, 6), new Rectangle(arch.Width-25, 0, 24, arch.Height-1), 0, 11, 0,
                new Point[] { new Point(arch.Width - 1, 0), new Point(arch.Width - 1, arch.Height - 1) },
                new float[] { (225f / 180f) * (float)Math.PI, (315f / 180f) * (float)Math.PI },
                new float[] { (15f / 180f) * (float)Math.PI, (-15f / 180f) * (float)Math.PI });

            ApplyTerrainBlend(arch, RotateByte(blend.AdjFlags, 4), new Rectangle(0, arch.Height - 25, arch.Width - 1, 24), 0, 0, 11,
                new Point[] { new Point(arch.Width - 1, arch.Height - 1), new Point(0, arch.Height - 1) },
                new float[] { (315f / 180f) * (float)Math.PI, (45f / 180f) * (float)Math.PI },
                new float[] { (15f / 180f) * (float)Math.PI, (-15f / 180f) * (float)Math.PI });

            ApplyTerrainBlend(arch, RotateByte(blend.AdjFlags, 2), new Rectangle(0, 0, 24, arch.Height - 1), 255, -11, 0,
                new Point[] { new Point(0, arch.Height - 1), new Point(0, 0) },
                new float[] { (45f / 180f) * (float)Math.PI, (135f / 180f) * (float)Math.PI},
                new float[] { (15f / 180f) * (float)Math.PI, (-15f / 180f) * (float)Math.PI });

            RestoreRoad(vm, roads);

            //set road dir. should only really do this FIRST EVER time, then road dir changes after are manual and rotate the contents of the lot.
            vm.TSOState.Size &= 0xFFFF;
            vm.TSOState.Size |= PickRoadDir(roads) << 16;

            PositionLandmarkObjects(vm);

            arch.SignalTerrainRedraw();
            arch.DisableClip = false;
        }
        
        public static byte PickRoadDir(byte roads)
        {
            for (int i=0; i<4; i++)
            {
                if ((roads&(1<<i))>0)
                {
                    return (byte)((4 - i) % 4);
                }
            }
            return 0;
        }

        private class GUIDToPosition
        {
            public uint GUID;
            public short X;
            public short Y;
            public int DirOff; //in 8th directions, like blueprint
            public GUIDToPosition(uint guid, short x, short y, int dirOff)
            {
                GUID = guid; X = x; Y = y; DirOff = dirOff;
            }
        }

        // looking towards the lot
        // (M=mailbox, B=bin, P=phone, 1/2=carportal)
        // (0x39CCF441, 0xA4258067, 0x313D2F9A, (0x865A6812, 0xD564C66B))
        // |===M|====
        // |   M B  P
        // |^^^^^^^^^
        // 2    1

        private static GUIDToPosition[] MovePositions = {
            //center relative (vertical line above)
            new GUIDToPosition(0x39CCF441, -1, 0, 0), //mailbox (2tile)
            new GUIDToPosition(0xA4258067, 1, 1, 0), //bin
            new GUIDToPosition(0x313D2F9A, 4, 1, 0), //phone
            new GUIDToPosition(0x865A6812, 0, 3, 2), //car portal 1
            new GUIDToPosition(0xD564C66B, -5, 3, 2), //car portal 2
        };

        /// <summary>
        /// Positions the Landmark objects depending on the lot direction. (npc/car portals, bin, mailbox, phone)
        /// </summary>
        /// <param name="vm">The VM.</param>
        public static void PositionLandmarkObjects(VM vm)
        {
            var arch = vm.Context.Architecture;
            var lotSInfo = vm.TSOState.Size;
            var lotSize = lotSInfo & 255;
            var lotFloors = ((lotSInfo >> 8) & 255) + 2;
            var lotDir = (lotSInfo >> 16);

            var dim = VMBuildableAreaInfo.BuildableSizes[lotSize];

            //need to rotate the lot dir towards the road. bit weird cos we're rotating a rectangle

            var w = arch.Width;
            var h = arch.Height;
            //little bit different from the array in VMContext. Want the tile positions outside buildable area
            //so min x and y are 5 instead of 6.
            var corners = new Vector2[]
            {
                new Vector2(5, 5), // top, default orientation
                new Vector2(w-7, 5), // right
                new Vector2(w-7, h-7), // bottom
                new Vector2(5, h-7) // left
            };
            var perpIncrease = new Vector2[]
            {
                new Vector2(0, -1), //bottom left road side
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(-1, 0)
            };

            //rotation 0: move perp from closer point to top bottom -> left (90 degree ccw of perp)
            //rotation 1: choose closer pt to top left->top (90 degree ccw of perp)
            //rotation 2: choose closer pt to top top->right (90 degree cw of perp)

            var pt1 = corners[(lotDir + 2) % 4];
            var pt2 = corners[(lotDir + 3) % 4];

            var ctr = (pt1 + pt2) / 2; //ok.

            var xperp = perpIncrease[(lotDir + 1) % 4];
            var yperp = perpIncrease[(lotDir + 2) % 4];
            //move relative position objs
            foreach (var pos in MovePositions)
            {
                var rpos = ctr + (pos.X * xperp) + (pos.Y * yperp);
                var ent = EntityByGUID(vm, pos.GUID);
                if (ent != null)
                {
                    ent.MultitileGroup.BaseObject.SetPosition(LotTilePos.FromBigTile((short)rpos.X, (short)rpos.Y, 1), (Direction)(1 << ((lotDir*2 + pos.DirOff) % 8)), vm.Context);
                }
            }

            // finally, must position npc portals. These are on the sidewalk, but at the far edge of the lot. 
            // ped, npc1, npc2 (0x81E6BEF9, 0x23BC2034, 0x4E57C380)
            // if there is water on the space we can't intersect it :(

            // for now just choose pavement corners. These are safe from being in water.
            var npc1 = EntityByGUID(vm, 0x23BC2034);
            if (npc1 != null) npc1.SetPosition(LotTilePos.FromBigTile((short)pt1.X, (short)pt1.Y, 1), (Direction)(1 << ((lotDir * 2 + 0) % 8)), vm.Context);
            var npc2 = EntityByGUID(vm, 0x4E57C380);
            if (npc2 != null) npc2.SetPosition(LotTilePos.FromBigTile((short)pt2.X, (short)pt2.Y, 1), (Direction)(1 << ((lotDir * 2 + 0) % 8)), vm.Context);
            var ped = EntityByGUID(vm, 0x81E6BEF9);
            if (ped != null) ped.SetPosition(LotTilePos.FromBigTile((short)ctr.X, (short)ctr.Y, 1), (Direction)(1 << ((lotDir * 2 + 0) % 8)), vm.Context);

            var rPos = ctr + (-13 * xperp) + (2 * yperp);
            if (ped != null)
            {
                StampTerrainmap(arch, CarDirtRoad, (short)rPos.X, (short)rPos.Y, xperp, yperp);
            }
        }

        private static VMEntity EntityByGUID(VM vm, uint GUID)
        {
            return vm.Entities.FindAll(x => (x.MasterDefinition?.GUID ?? 0) == GUID || x.Object.GUID == GUID).FirstOrDefault();
        }

        private static byte RotateByte(byte flags, int amount)
        {
            return (byte)((255 & (flags << amount)) | (flags >> (8 - amount)));
        }

        public static void FillTerrainRect(VMArchitecture arch, Rectangle area, byte value)
        {
            for (int x = 0; x < area.Width; x++)
            {
                var ox = x + area.X;
                for (int y = 0; y < area.Height; y++)
                {
                    var oy = y + area.Y;
                    arch.Terrain.GrassState[oy * arch.Width + ox] = value;
                }
            }
        }

        public static void ApplyTerrainBlend(VMArchitecture arch, int flags, Rectangle area, int startVal, int xChange, int yChange, Point[] coneCtr, float[] pivots, float[] ranges)
        {
            //neighbourhood of 5 flags. 1=ccw edge, 2 = ccw corner, 4 = my edge, 8 = cw corner, 16 = cw edge

            bool flipCone = (flags & 4) == 0;
            if (flipCone) return; //flipCone mode was experimental and actually doesn't work with the corner drawing method I was using.

            bool corner1 = ((flags & 4) == 0) ?
                (((flags & 1) == 0 && ((flags & 2) > 0)) ? true : false) //inverted mode. draw part corner if no edge to make up for it
                : ((flags & 2) == 0); // corner fade if no corner present

            bool corner2 = ((flags & 4) == 0) ?
                (((flags & 16) == 0 && ((flags & 8) > 0)) ? true : false) //inverted mode. draw part corner if no edge to make up for it
                : ((flags & 8) == 0); // corner fade if no corner present


            for (int x = 0; x<area.Width; x++)
            {
                var ox = x + area.X;
                for (int y = 0; y < area.Height; y++)
                {
                    var oy = y + area.Y;
                    var val = startVal + xChange * x + yChange * y;
                    int closest = -1;
                    int closestDist = int.MaxValue;
                    for (int i=0; i<coneCtr.Length; i++)
                    {
                        var dx = coneCtr[i].X - ox;
                        var dy = coneCtr[i].Y - oy;
                        var dist = dx * dx + dy * dy;
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            closest = i;
                        }
                    }
                    if (closest != -1)
                    {
                        if ((closest == 0 && corner1) || (closest == 1 && corner2))
                        {
                            val = (int)(val * ((flipCone?1:0)+ConeMult(coneCtr[closest], new Point(ox, oy), pivots[closest], ranges[closest] * (flipCone ? -1 : 1))));
                        } else if (flipCone)
                        {
                            val = 0;
                        }
                    }
                    val += arch.Terrain.GrassState[oy * arch.Width + ox];
                    arch.Terrain.GrassState[oy * arch.Width + ox] = (byte)(Math.Max(0, Math.Min(255,val)));
                }
            }
        }

        public static float ConeMult(Point center, Point dest, float pivotAngle, float range)
        {
            if (dest == center) return 1f;
            var rel = dest - center;
            rel.Y *= -1;
            var angle = Math.Atan2(rel.X, rel.Y);
            if (angle < 0) angle += 2 * Math.PI;
            var relAngle = pivotAngle - angle;

            float mult = 0.5f + (float)relAngle * (0.5f / range);
            mult = Math.Max(0f, Math.Min(1f, mult));
            return mult;
        }

        public static void RestoreRoad (VM vm, byte roads)
        {
            //lo bits: road flags
            //hi bits: road corners flags
            var arch = vm.Context.Architecture;
            //road starts: bit 1 = citymapRight = topLeft
            //bit 2 = citymapBottom = topRight (and so on in clockwise order)

            if ((roads & 8) > 0)
            {
                RepeatTilemap(arch, TopLeftRoadTile, 1, 1, 1, 1, (arch.Height + 3) / 4);
                StampTilemap(arch, TopLeftRoadCrossing, 1, 3, 1); 
                StampTilemap(arch, TopLeftRoadCrossing, 1, (short)(arch.Height-9), 1);
            }
            if ((roads & 4) > 0)
            {
                RepeatTilemap(arch, TopRightRoadTile, 1, 1, 1, (arch.Width + 3) / 4, 1);
                StampTilemap(arch, TopRightRoadCrossing, 3, 1, 1);
                StampTilemap(arch, TopRightRoadCrossing, (short)(arch.Width - 9), 1, 1);
            }
            if ((roads & 2) > 0)
            {
                RepeatTilemap(arch, BottomRightRoadTile, (short)(arch.Width - 7), 1, 1, 1, (arch.Height + 3) / 4);
                StampTilemap(arch, BottomRightRoadCrossing, (short)(arch.Width - 7), 3, 1);
                FillTiles(arch, 9, (short)(arch.Width - 2), 1, 1, 1, 2);
                StampTilemap(arch, BottomRightRoadCrossing, (short)(arch.Width - 7), (short)(arch.Height - 9), 1);
                FillTiles(arch, 9, (short)(arch.Width - 2), (short)(arch.Height - 4), 1, 1, 3);
            }
            if ((roads & 1) > 0)
            {
                RepeatTilemap(arch, BottomLeftRoadTile, 1, (short)(arch.Height - 7), 1, (arch.Width + 3) / 4, 1);
                StampTilemap(arch, BottomLeftRoadCrossing, 3, (short)(arch.Height - 7), 1);
                FillTiles(arch, 9, 1, (short)(arch.Height - 2), 1, 2, 1);
                StampTilemap(arch, BottomLeftRoadCrossing, (short)(arch.Width - 9), (short)(arch.Height - 7), 1);
                FillTiles(arch, 9, (short)(arch.Width - 4), (short)(arch.Height - 2), 1, 3, 1);
            }
            
            if ((roads & 12) == 12) StampTilemap(arch, TopRoadInnerCorner, 1, 1, 1, true);
            if ((roads & 6) == 6) StampTilemap(arch, RightRoadInnerCorner, (short)(arch.Width - 9), 1, 1, true);
            if ((roads & 3) == 3) StampTilemap(arch, BottomRoadInnerCorner, (short)(arch.Width - 9), (short)(arch.Height - 9), 1, true);
            if ((roads & 9) == 9) StampTilemap(arch, LeftRoadInnerCorner, 1, (short)(arch.Height - 9), 1, true);


            //corners start bit 1 = citymapTopRight = left,
            //bit2 = citymapBottomRight = top (and so on in clockwise order)

            var corners = (roads >> 4);
            if ((corners & 1) > 0) StampTilemap(arch, LeftRoadCorner, 1, (short)(arch.Height - 7), 1);
            if ((corners & 2) > 0) StampTilemap(arch, BottomRoadCorner, (short)(arch.Width - 7), (short)(arch.Height - 7), 1);
            if ((corners & 4) > 0) StampTilemap(arch, RightRoadCorner, (short)(arch.Width-7), 1, 1);
            if ((corners & 8) > 0) StampTilemap(arch, TopRoadCorner, 1, 1, 1);
        }

        public static void PopulateBlankTerrain(VM vm)
        {
            var arch = vm.Context.Architecture;
            var objs = BlankTerrainObjects[(int)arch.Terrain.LightType];

            var random = new Random();
            var toPlace = 15 + random.Next(20);

            for (int i=0; i<toPlace; i++)
            {
                vm.Context.CreateObjectInstance(objs[random.Next(objs.Length-1)],
                    LotTilePos.FromBigTile((short)(random.Next(arch.Width - 14) + 7), (short)(random.Next(arch.Height - 14) + 7), 1),
                    (Direction)(1 << (random.Next(4) * 2)));
            }

            if (random.Next(6) == 0)
            {
                vm.Context.CreateObjectInstance(objs[objs.Length - 1], 
                    LotTilePos.FromBigTile((short)(random.Next(arch.Width-14)+7), (short)(random.Next(arch.Height - 14) + 7), 1), 
                    (Direction)(1 << (random.Next(4) * 2)));
            }
        }

        public static void RestoreSurroundings(VM vm, byte[][] hollowAdj)
        {
            var myArch = vm.Context.Architecture;
            var terrain = vm.TSOState.Terrain;
            var size = myArch.Width;
            for (int y=0; y<3; y++)
            {
                for (int x=0; x<3; x++)
                {
                    if (x == 1 & y == 1) continue; //that's us...
                    var gd = vm.Context.World.State.Device;
                    var subworld = new SubWorldComponent(gd);
                    subworld.Initialize(gd);
                    var tempVM = new VM(new VMContext(subworld), new VMServerDriver(new VMTSOGlobalLinkStub()), new VMNullHeadlineProvider());
                    tempVM.Init();

                    var state = (hollowAdj == null)? null : hollowAdj[y * 3 + x];

                    VMHollowMarshal hollow = null;
                    if (state != null)
                    {
                        try
                        {
                            hollow = new VMHollowMarshal();
                            using (var reader = new BinaryReader(new MemoryStream(state))) {
                                hollow.Deserialize(reader);
                            }
                            tempVM.HollowLoad(hollow);
                            RestoreTerrain(tempVM, terrain.BlendN[x, y], terrain.Roads[x, y]);
                            tempVM.Update();
                            tempVM.Update();
                        } catch (Exception)
                        {
                            hollow = null;
                        }
                    }

                    if (hollow == null)
                    {
                        var blueprint = new Blueprint(size, size);
                        tempVM.Context.Blueprint = blueprint;
                        subworld.InitBlueprint(blueprint);
                        tempVM.Context.Architecture = new VMArchitecture(size, size, blueprint, tempVM.Context);

                        tempVM.Context.Architecture.RegenRoomMap();
                        tempVM.Context.RegeneratePortalInfo();

                        var terrainC = new TerrainComponent(new Rectangle(1, 1, size - 2, size - 2), blueprint);
                        terrainC.Initialize(subworld.State.Device, subworld.State);
                        blueprint.Terrain = terrainC;

                        RestoreTerrain(tempVM, terrain.BlendN[x, y], terrain.Roads[x, y]);
                        PopulateBlankTerrain(tempVM);

                        tempVM.Update();
                        tempVM.Update();
                    }

                    subworld.State.Level = 5;
                    subworld.GlobalPosition = new Vector2((1 - y) * (size - 2), (x - 1) * (size - 2));

                    vm.Context.Blueprint.SubWorlds.Add(subworld);
                }
            }
        }
    }
}
