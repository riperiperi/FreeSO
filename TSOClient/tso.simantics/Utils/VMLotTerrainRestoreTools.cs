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
            var width = tilemap[0];
            var height = tilemap[1];

            for (int i=2; i<tilemap.Length; i++)
            {
                if (tilemap[i] == 0) continue;
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
                for (int yo = 0; yo < ytimes; yo++)
                {
                    arch.SetFloor((short)(x + xo), (short)(y + yo), level, new FloorTile() { Pattern = (ushort)tile }, true);
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

        public static void RestoreTerrain(VM vm)
        {
            //take center of lotstate
            RestoreTerrain(vm, vm.TSOState.Terrain.BlendN[1, 1], vm.TSOState.Terrain.Roads[1, 1]);
        }

        public static void RestoreTerrain(VM vm, TerrainBlend blend, byte roads)
        {
            var arch = vm.Context.Architecture;

            if (blend.Base == TerrainType.WATER)
            {
                //...
                VMArchitectureTools.FloorPatternRect(arch, new Rectangle(1, 1, arch.Width - 2, arch.Height - 2), 0, 65534, 1);
                return;
            }

            arch.Terrain.LightType = blend.Base;
            arch.Terrain.DarkType = (blend.Blend == TerrainType.WATER) ? blend.Base : blend.Blend;
            arch.Terrain.GenerateGrassStates();

            //clear all previous roads/sea
            VMArchitectureTools.FloorPatternRect(arch, new Rectangle(0, 0, arch.Width, 6), 0, 0, 1);
            VMArchitectureTools.FloorPatternRect(arch, new Rectangle(arch.Width - 7, 0, 7, arch.Height), 0, 0, 1);
            VMArchitectureTools.FloorPatternRect(arch, new Rectangle(0, arch.Height - 7, arch.Width, 7), 0, 0, 1);
            VMArchitectureTools.FloorPatternRect(arch, new Rectangle(0, 0, 6, arch.Height), 0, 0, 1);

            //blend flags start at top left, then go clockwise. (top right, bottom right..)
            if ((blend.WaterFlags & 1) > 0) VMArchitectureTools.FloorPatternRect(arch, new Rectangle(1, 1, 4, arch.Height - 2), 0, 65534, 1);
            if ((blend.WaterFlags & 2) > 0) VMArchitectureTools.FloorPatternRect(arch, new Rectangle(1, 1, arch.Width-2, 4), 0, 65534, 1);
            if ((blend.WaterFlags & 4) > 0) VMArchitectureTools.FloorPatternRect(arch, new Rectangle(arch.Width-5, 1, 4, arch.Height - 2), 0, 65534, 1);
            if ((blend.WaterFlags & 8) > 0) VMArchitectureTools.FloorPatternRect(arch, new Rectangle(1, arch.Height-5, arch.Width-2, 4), 0, 65534, 1);

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

            //and finally, hard blends into the next terrain type 
            /*
            FillTerrainRect(arch, new Rectangle(0, 0, 1, arch.Height - 1), (byte)(((blend.AdjFlags & 1) > 0)?255:0));
            FillTerrainRect(arch, new Rectangle(0, 0, arch.Width-1, 1), (byte)(((blend.AdjFlags & 4) > 0) ? 255 : 0));
            FillTerrainRect(arch, new Rectangle(arch.Width - 2, 0, 1, arch.Height - 1), (byte)(((blend.AdjFlags & 16) > 0) ? 255 : 0));
            FillTerrainRect(arch, new Rectangle(0, arch.Height - 2, arch.Width - 1, 1), (byte)(((blend.AdjFlags & 64) > 0) ? 255 : 0));

            FillTerrainRect(arch, new Rectangle(0, 0, 1, 1), (byte)(((blend.AdjFlags & 2) > 0) ? 255 : 0));
            FillTerrainRect(arch, new Rectangle(arch.Width - 2, 0, 1, 1), (byte)(((blend.AdjFlags & 8) > 0) ? 255 : 0));
            FillTerrainRect(arch, new Rectangle(arch.Width - 2, arch.Height - 2, 1, 1), (byte)(((blend.AdjFlags & 32) > 0) ? 255 : 0));
            FillTerrainRect(arch, new Rectangle(0, arch.Height - 2, 1, 1), (byte)(((blend.AdjFlags & 128) > 0) ? 255 : 0));
            */

            RestoreRoad(vm, roads);
            arch.SignalTerrainRedraw();
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
            
            if ((roads & 12) == 12) StampTilemap(arch, TopRoadInnerCorner, 1, 1, 1);
            if ((roads & 6) == 6) StampTilemap(arch, RightRoadInnerCorner, (short)(arch.Width - 9), 1, 1);
            if ((roads & 3) == 3) StampTilemap(arch, BottomRoadInnerCorner, (short)(arch.Width - 9), (short)(arch.Height - 9), 1);
            if ((roads & 9) == 9) StampTilemap(arch, LeftRoadInnerCorner, 1, (short)(arch.Height - 9), 1);


            //corners start bit 1 = citymapTopRight = left,
            //bit2 = citymapBottomRight = top (and so on in clockwise order)

            var corners = (roads >> 4);
            if ((corners & 1) > 0) StampTilemap(arch, LeftRoadCorner, 1, (short)(arch.Height - 7), 1);
            if ((corners & 2) > 0) StampTilemap(arch, TopRoadCorner, 1, 1, 1);
            if ((corners & 4) > 0) StampTilemap(arch, RightRoadCorner, (short)(arch.Width-7), 1, 1);
            if ((corners & 8) > 0) StampTilemap(arch, BottomRoadCorner, (short)(arch.Width-7), (short)(arch.Height - 7), 1);
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
