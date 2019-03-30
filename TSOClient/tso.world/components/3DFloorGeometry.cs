using FSO.Common;
using FSO.Common.Utils;
using FSO.LotView.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.LotView.Components
{
    /// <summary>
    /// Handles indices and groups floor geometry by texture for a each floor.
    /// 
    /// </summary>
    /// 

    // these are future plans, really.
    // indices for individual tiles
    // w is ((width+1) * 2) - 1. this is a point for each tile
    //  0________2
    //   |\    /|
    //   | \  / |
    //   |  \/1 |
    //   |  /\  |
    //   | /  \ |
    // +w|/____\|+w+2

    public class _3DFloorGeometry : IDisposable
    {
        public Blueprint Bp;
        public FloorLevel[] Floors;

        public static bool af2019;
        static Dictionary<int, Texture2D> PoolReplace; //af2019
        static Dictionary<int, Texture2D> PoolReplaceParallax;

        public _3DFloorGeometry(Blueprint bp)
        {
            Bp = bp;
            Floors = new FloorLevel[bp.Stories];
            for (int i=0; i<bp.Stories; i++)
            {
                Floors[i] = new FloorLevel();
            }
        }

        private void LoadPoolReplace(GraphicsDevice gd, string folder)
        {
            //don't worry this will be removed when i do graphics refactor
            //probably
            PoolReplace = new Dictionary<int, Texture2D>();
            PoolReplaceParallax = new Dictionary<int, Texture2D>();

            for (int i=0; i<16; i++)
            {
                using (var file = System.IO.File.Open($"Content/Textures/{folder}/diffuse/pool{i}.png", System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read)) {
                    PoolReplace[i] = Files.ImageLoader.FromStream(gd, file);
                }
                using (var file = System.IO.File.Open($"Content/Textures/{folder}/parallax/pool{i}.png", System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
                {
                    PoolReplaceParallax[i] = Files.ImageLoader.FromStream(gd, file);
                }
            }
        }

        public void FullReset(GraphicsDevice gd, bool buildMode)
        {
            if (af2019 && PoolReplace == null) LoadPoolReplace(gd, "af19");
            for (int i=0; i<Floors.Length; i++)
            {
                var lvl = Floors[i];
                var data = Bp.Floors[i];
                var walls = Bp.Walls[i];
                lvl.Clear();
                var width = Bp.Width;
                var end = data.Length - width;
                for (int j=width; j<end; j++)
                {
                    var pat = data[j].Pattern;
                    if ((j + 1) % Bp.Width < 2) continue;
                    var wall = walls[j];
                    if ((wall.Segments & WallSegments.AnyDiag) > 0)
                    {
                        var vertical = (wall.Segments & WallSegments.VerticalDiag) > 0;

                        if (wall.TopLeftPattern != 0 || i == 0) lvl.AddTileDiag(wall.TopLeftPattern, (ushort)j, true, vertical);
                        if (wall.TopLeftStyle != 0 || i == 0) lvl.AddTileDiag(wall.TopLeftStyle, (ushort)j, false, vertical);
                    }
                    else
                    {
                        var convert = PatternConvert(pat, (ushort)j, (sbyte)(i + 1), buildMode);
                        if (convert == 0 && i > 0) continue;
                        lvl.AddTile(convert, (ushort)j);
                    }
                }
                lvl.RegenAll(gd);
            }
        }

        public void SliceReset(GraphicsDevice gd, Rectangle slice)
        {
            for (int i = 0; i < Floors.Length; i++)
            {
                var lvl = Floors[i];
                var data = Bp.Floors[i];
                lvl.Clear();

                for (int x=slice.X; x<slice.Right; x++)
                {
                    for (int y = slice.Y; y < slice.Bottom; y++)
                    {
                        var j = y * Bp.Width + x;
                        var pat = data[j].Pattern;
                        if ((j + 1) % Bp.Width < 2) continue;
                        var convert = PatternConvert(pat, (ushort)j, (sbyte)(i + 1), false);
                        if (convert == 0 && i > 0) continue;
                        lvl.AddTile(convert, (ushort)j);
                    }
                }
                lvl.RegenAll(gd);
            }
        }

        public void BuildableReset(GraphicsDevice gd, bool[] buildable)
        {
            for (int i = 0; i < Floors.Length; i++)
            {
                var lvl = Floors[i];
                var data = Bp.Floors[i];
                lvl.Clear();

                for (int j = 0; j < data.Length; j++)
                {
                    if (!buildable[j]) continue;
                    var pat = data[j].Pattern;
                    if ((j + 1) % Bp.Width < 2) continue;
                    var convert = PatternConvert(pat, (ushort)j, (sbyte)(i + 1), false);
                    if (convert == 0 && i > 0) continue;
                    lvl.AddTile(convert, (ushort)j);
                }
                lvl.RegenAll(gd);
            }
        }

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

        private static Dictionary<WorldRotation, Vector4> TexMat = new Dictionary<WorldRotation, Vector4>()
        {
            {WorldRotation.TopLeft, new Vector4(1,0,0,1) },
            {WorldRotation.TopRight, new Vector4(0,1,1,0) },
            {WorldRotation.BottomRight, new Vector4(1,0,0,1) },
            {WorldRotation.BottomLeft, new Vector4(0,1,1,0) }
        };

        private static Dictionary<WorldRotation, Vector4> CounterTexMat = new Dictionary<WorldRotation, Vector4>()
        {
            {WorldRotation.TopLeft, new Vector4(1,0,0,1) },
            {WorldRotation.TopRight, new Vector4(0,1,-1,0) },
            {WorldRotation.BottomRight, new Vector4(-1,0,0,-1) },
            {WorldRotation.BottomLeft, new Vector4(0,-1,1,0) }
        };

        private static Dictionary<WorldZoom, Vector2> TexOffset = new Dictionary<WorldZoom, Vector2>()
        {
            {WorldZoom.Near, new Vector2(0, -0.5f/64) },
            {WorldZoom.Medium, new Vector2(0, -0.5f/32) },
            {WorldZoom.Far, new Vector2(0, -0.5f/16) },
        };

        public ushort PatternConvert(ushort pattern, ushort index, sbyte level, bool buildMode)
        {
            //65520 - 65535 (inclusive): pool tiles
            //65504 - 65519 (inclusive): water tiles
            //65503: air tile
            //corners to come later...

            if (buildMode && pattern == 0 && level > 1)
            {
                var x = index % Bp.Width;
                var y = index / Bp.Width;
                if (Bp.Supported[level - 2][y * Bp.Height + x])
                {
                    return 65503;
                }
                else return 0;
            }
            else if (pattern < 65534) return pattern;
            else
            {
                //pool tile... check adjacent tiles
                var x = index % Bp.Width;
                var y = index / Bp.Width;

                int poolAdj = 0;
                for (int i = 0; i < PoolDirections.Length; i++)
                {
                    var testTile = new Point(x, y) + PoolDirections[i];
                    if ((testTile.X <= 0 || testTile.X >= Bp.Width - 1) || (testTile.Y <= 0 || testTile.Y >= Bp.Height - 1)
                        || Bp.GetFloor((short)testTile.X, (short)testTile.Y, level).Pattern == pattern) poolAdj |= 1 << i;
                }

                var adj = (PoolSegments)poolAdj;
                ushort spriteNum = 0;
                if ((adj & PoolSegments.TopRight) > 0) spriteNum |= 1;
                if ((adj & PoolSegments.TopLeft) > 0) spriteNum |= 2;
                if ((adj & PoolSegments.BottomLeft) > 0) spriteNum |= 4;
                if ((adj & PoolSegments.BottomRight) > 0) spriteNum |= 8;

                if (pattern == 65535) spriteNum += 65520;
                else spriteNum += 65504;

                return spriteNum;
            }
        }

        public int SetGrassIndices(GraphicsDevice gd, Effect e, WorldState state)
        {
            var floor = Floors[0];
            FloorTileGroup grp = null;
            if (!floor.GroupForTileType.TryGetValue(0, out grp)) return 0;
            var dat = grp.GPUData;
            if (dat == null) return 0;
            gd.Indices = dat;
            return dat.IndexCount/3;
        }

        public SamplerState CustomWrap = new SamplerState()
        {
            Filter = TextureFilter.Linear,
            AddressU = TextureAddressMode.Wrap,
            AddressV = TextureAddressMode.Wrap,
            AddressW = TextureAddressMode.Wrap,
        };

        public DepthStencilState NoDS = new DepthStencilState()
        {
            DepthBufferWriteEnable = false
        };

        private bool Alt;

        public void DrawFloor(GraphicsDevice gd, Effect e, WorldZoom zoom, WorldRotation rot, List<Texture2D> roommaps, HashSet<sbyte> floors, EffectPass pass, 
            Matrix? lightWorld = null, WorldState state = null, int minFloor = 0)
        {
            var parallax = WorldConfig.Current.Complex;
            //assumes the effect and all its parameters have been set up already
            //we just need to get the right texture and offset
            var flrContent = Content.Content.Get().WorldFloors;

            e.Parameters["TexOffset"].SetValue(new Vector2());// TexOffset[zoom]*-1f);
            var tmat = TexMat[rot];
            e.Parameters["TexMatrix"].SetValue(tmat);

            var f = 0;
            foreach (var floor in Floors)
            {
                if (!floors.Contains((sbyte)(f++))) continue;

                Matrix worldmat;
                if (lightWorld == null)
                    worldmat = Matrix.CreateTranslation(0, 2.95f * (f - 1) * 3 - Bp.BaseAlt * Bp.TerrainFactor * 3, 0);
                else
                {
                    worldmat = Matrix.CreateScale(1, 0, 1) * Matrix.CreateTranslation(0, 1f * (f - (1 + minFloor)), 0) * lightWorld.Value;
                    e.Parameters["DiffuseColor"].SetValue(new Vector4(1, 1, 1, 1) * (float)(6 - (f - (minFloor)))/5f);
                }
                
                e.Parameters["World"].SetValue(worldmat);
                e.Parameters["Level"].SetValue((float)(f-((lightWorld == null)?0.999f:1f)));
                if (roommaps != null) e.Parameters["RoomMap"].SetValue(roommaps[f-1]);
                foreach (var type in floor.GroupForTileType)
                {
                    bool water = false;
                    var dat = type.Value.GPUData;
                    if (dat == null) continue;
                    gd.Indices = dat;

                    var id = type.Key;
                    var doubleDraw = false;
                    Texture2D SPR = null;
                    Texture2D pSPR = null;

                    if (id == 0)
                    {
                        e.Parameters["UseTexture"].SetValue(false);
                        e.Parameters["IgnoreColor"].SetValue(false);
                        e.Parameters["GrassShininess"].SetValue(0.02f);// (float)0.25);
                    }
                    else
                    {
                        e.Parameters["GrassShininess"].SetValue((id >= 65503)?0.02f:0f);
                        if (id >= 65503)
                        {
                            if (id == 65503)
                            {
                                water = true;
                                var airTiles = TextureGenerator.GetAirTiles(gd);
                                switch (zoom)
                                {
                                    case WorldZoom.Far:
                                        SPR = airTiles[2];
                                        break;
                                    case WorldZoom.Medium:
                                        SPR = airTiles[1];
                                        break;
                                    case WorldZoom.Near:
                                        SPR = airTiles[0];
                                        break;
                                }
                            }
                            else
                            {
                                e.Parameters["Water"].SetValue(true);
                                var pool = id >= 65520;
                                water = true;
                                if (!pool)
                                {
                                    e.Parameters["UseTexture"].SetValue(false);
                                    e.Parameters["IgnoreColor"].SetValue(false);

                                    //quickly draw under the water
                                    pass.Apply();
                                    gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, type.Value.GeomForOffset.Count * 2);

                                    e.Parameters["UseTexture"].SetValue(true);
                                    e.Parameters["IgnoreColor"].SetValue(true);
                                    if (lightWorld == null) e.Parameters["World"].SetValue(worldmat * Matrix.CreateTranslation(0, 0.05f, 0));
                                    id -= 65504;
                                }
                                else
                                {
                                    id -= 65520;
                                }

                                e.Parameters["TexMatrix"].SetValue(CounterTexMat[rot]);

                                var roti = (int)rot;
                                roti = (4 - roti) % 4;
                                id = (ushort)(((id << roti) & 15) | (id >> (4 - roti)));
                                //pools & water are drawn with special logic, and may also be drawn slightly above the ground.

                                int baseSPR;
                                int frameNum = 0;
                                if (state != null)
                                {
                                    if (PoolReplace != null && pool)
                                    {
                                        SPR = PoolReplace[id];
                                        if (parallax) pSPR = PoolReplaceParallax[id];
                                    }
                                    else
                                    {
                                        switch (zoom)
                                        {
                                            case WorldZoom.Far:
                                                baseSPR = (pool) ? 0x400 : 0x800;
                                                frameNum = (pool) ? 0 : 2;
                                                SPR = state._2D.GetTexture(flrContent.GetGlobalSPR((ushort)(baseSPR + id)).Frames[frameNum]);
                                                break;
                                            case WorldZoom.Medium:
                                                baseSPR = (pool) ? 0x410 : 0x800;
                                                frameNum = (pool) ? 0 : 1;
                                                SPR = state._2D.GetTexture(flrContent.GetGlobalSPR((ushort)(baseSPR + id)).Frames[frameNum]);
                                                break;
                                            default:
                                                baseSPR = (pool) ? 0x420 : 0x800;
                                                SPR = state._2D.GetTexture(flrContent.GetGlobalSPR((ushort)(baseSPR + id)).Frames[frameNum]);
                                                break;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            var flr = flrContent.Get(id);

                            if (flr == null) continue;

                            if (state != null)
                            {
                                switch (zoom)
                                {
                                    case WorldZoom.Far:
                                        SPR = state._2D.GetTexture(flr.Far.Frames[0]);
                                        break;
                                    case WorldZoom.Medium:
                                        SPR = state._2D.GetTexture(flr.Medium.Frames[0]);
                                        break;
                                    default:
                                        SPR = state._2D.GetTexture(flr.Near.Frames[0]);
                                        break;
                                }
                            }
                        }

                        //e.Parameters["UseTexture"].SetValue(SPR != null);

                    }

                    e.Parameters["BaseTex"].SetValue(SPR);
                    if (SPR != null && SPR.Name == null)
                    {
                        doubleDraw = true;
                        SPR.Name = Alt.ToString();
                    }
                    if (pSPR != null)
                    {
                        var parallaxPass = e.CurrentTechnique.Passes[4];
                        e.Parameters["ParallaxTex"].SetValue(pSPR);
                        e.Parameters["ParallaxUVTexMat"].SetValue(new Vector4(0.7071f, -0.7071f, 0.7071f, 0.7071f));
                        e.Parameters["ParallaxHeight"].SetValue(0.1f);
                        parallaxPass.Apply();
                    }
                    else
                    {
                        pass.Apply();
                    }
                    if (Alt && !FSOEnvironment.DirectX)
                    {
                        //opengl bug workaround. For some reason, the texture is set to clamp mode by some outside force on first draw. 
                        //Monogame then thinks the texture is wrapping.
                        gd.SamplerStates[1] = CustomWrap;
                    }
                    gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, type.Value.GeomForOffset.Count * 2);
                    //gd.SamplerStates[1] = SamplerState.LinearWrap;

                    if (id == 0)
                    {
                        e.Parameters["UseTexture"].SetValue(true);
                        e.Parameters["IgnoreColor"].SetValue(true);
                    }
                    if (water)
                    {
                        e.Parameters["World"].SetValue(worldmat);
                        e.Parameters["TexMatrix"].SetValue(tmat);
                        e.Parameters["Water"].SetValue(false);
                    }
                }
            }
            e.Parameters["Water"].SetValue(false);
            Alt = !Alt;
        }

        /*
        public void Regenerate(Rectangle bounds)
        {
            //find all meshes that are dirty:
            // - all floor textures with bounds intersecting these bounds (old)
            // - all floor textures present now (new, union)

            var floors = Bp.Floors[FloorNum];
            //get all 
            var set = new HashSet<ushort>();
            for (int y = bounds.Top; y < bounds.Bottom; y++)
            {
                var idx = y * Bp.Width + bounds.Left;
                for (int x = bounds.Left; x < bounds.Right; x++)
                {
                    set.Add(floors[idx++].Pattern);
                }
            }

            foreach (var geom in IDToGeom.Values)
            {
                if (bounds.Intersects(geom.Bounds) || set.Contains(geom.FloorID))
                {
                    set.Add(geom.FloorID);
                    bounds = Rectangle.Union(geom.Bounds, bounds);
                }
            }

            var idxwidth = (Bp.Width * 2) + 1;
            //build indices for all the geometry
            for (int y=bounds.Top; y<bounds.Bottom; y++)
            {
                var by = idxwidth * y;
                for (int x=bounds.Bottom; x<bounds.Right; x++)
                {
                    //

                    var idxs = new List<int>();
                    var basei = x*2 + by;

                    //top tri
                    idxs.Add(basei); //tl
                    idxs.Add(basei + 2); //tr
                    idxs.Add(basei + 1); //ctr
                    
                    //right tri
                    idxs.Add(basei + 2); //tr
                    idxs.Add(basei + idxwidth + 2); //br
                    idxs.Add(basei + 1); //ctr

                    //left tri
                    idxs.Add(basei); //tl
                    idxs.Add(basei + 1); //ctr
                    idxs.Add(basei + idxwidth); //bl

                    //bottom tri
                    idxs.Add(basei + 1); //ctr
                    idxs.Add(basei + idxwidth + 2); //br
                    idxs.Add(basei + idxwidth); //bl

                }
            }
        }
        */

        public void Dispose()
        {
            foreach (var floor in Floors)
            {
                floor.Dispose();
            }
        }
    }

    public class FloorLevel : IDisposable
    {
        public Dictionary<ushort, FloorTileGroup> GroupForTileType = new Dictionary<ushort, FloorTileGroup>();
        public ushort[] Tiles;

        public void AddTile(ushort tileID, ushort offset)
        {
            FloorTileGroup group;
            if (!GroupForTileType.TryGetValue(tileID, out group))
            {
                group = new FloorTileGroup();
                GroupForTileType[tileID] = group;
            }
            group.AddIndex(offset);
        }

        public void AddTileDiag(ushort tileID, ushort offset, bool side, bool vertical)
        {
            FloorTileGroup group;
            if (!GroupForTileType.TryGetValue(tileID, out group))
            {
                group = new FloorTileGroup();
                GroupForTileType[tileID] = group;
            }
            group.AddDiagIndex(offset, side, vertical);
        }

        public void Regen(GraphicsDevice gd, HashSet<ushort> floors)
        {

        }

        public void RegenAll(GraphicsDevice gd)
        {
            foreach (var group in GroupForTileType)
            {
                group.Value.PrepareGPU(gd);
            }
        }

        public void Clear()
        {
            Dispose();
            GroupForTileType.Clear();
        }

        public void Dispose()
        {
            foreach (var group in GroupForTileType)
            {
                group.Value.Dispose();
            }
        }
    }

    public class FloorTileGroup : IDisposable
    {
        public Dictionary<ushort, List<int>> GeomForOffset = new Dictionary<ushort, List<int>>();
        public IndexBuffer GPUData;

        public void PrepareGPU(GraphicsDevice gd)
        {
            if (GPUData != null) GPUData.Dispose();
            var dat = BuildIndexData();
            GPUData = new IndexBuffer(gd, IndexElementSize.ThirtyTwoBits, dat.Length, BufferUsage.None);
            GPUData.SetData(dat);
        }

        public int[] BuildIndexData()
        {
            var result = new int[GeomForOffset.Count * 6];
            int i = 0;
            foreach (var geom in GeomForOffset.Values)
            {
                foreach (var elem in geom)
                {
                    result[i++] = elem;
                }
            }
            return result;
        }

        public void AddIndex(ushort offset)
        {
            //
            var o2 = offset * 4;
            var result = new List<int> { o2, o2 + 1, o2 + 2, o2 + 2, o2 + 3, o2 };
            GeomForOffset[offset] = result;
        }

        public void AddDiagIndex(ushort offset, bool side, bool vertical)
        {
            //
            var o2 = offset * 4;
            List<int> result;
            if (vertical)
            {
                if (side) result = new List<int> { o2, o2 + 1, o2 + 2 };
                else result = new List<int> { o2 + 2, o2 + 3, o2 };
            } else
            {
                if (side) result = new List<int> { o2+1, o2 + 2, o2 + 3 };
                else result = new List<int> { o2, o2 + 1, o2 + 3 };
            }
            GeomForOffset[(ushort)(offset + (side?32768:0))] = result;
        }


        public bool RemoveIndex(ushort offset)
        {
            GeomForOffset.Remove(offset);
            return GeomForOffset.Count > 0;
        }

        public void Dispose()
        {
            GPUData?.Dispose();
        }
    }

    public class FloorTextureGroup
    {
        public Rectangle Bounds;
        public ushort FloorID;
    }
}
