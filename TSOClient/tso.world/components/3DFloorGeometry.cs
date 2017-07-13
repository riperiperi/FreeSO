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

        public _3DFloorGeometry(Blueprint bp)
        {
            Bp = bp;
            Floors = new FloorLevel[bp.Stories];
            for (int i=0; i<bp.Stories; i++)
            {
                Floors[i] = new FloorLevel();
            }
        }

        public void FullReset(GraphicsDevice gd)
        {
            for (int i=0; i<Floors.Length; i++)
            {
                var lvl = Floors[i];
                var data = Bp.Floors[i];
                lvl.Clear();
                var width = Bp.Width;
                var end = data.Length - width;
                for (int j=width; j<end; j++)
                {
                    var pat = data[j].Pattern;
                    if ((j + 1) % Bp.Width < 2 || (pat == 0 && i > 0)) continue;
                    lvl.AddTile(PatternConvert(pat, (ushort)j, (sbyte)(i+1)), (ushort)j);
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

        public ushort PatternConvert(ushort pattern, ushort index, sbyte level)
        {
            //65520 - 65535 (inclusive): pool tiles
            //65504 - 65519 (inclusive): water tiles
            //corners to come later...

            if (pattern < 65534) return pattern;
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

        public bool SetGrassIndices(GraphicsDevice gd, Effect e, WorldState state)
        {
            var floor = Floors[0];
            FloorTileGroup grp = null;
            if (!floor.GroupForTileType.TryGetValue(0, out grp)) return false;
            var dat = grp.GPUData;
            if (dat == null) return false;
            gd.Indices = dat;
            return true;
        }

        public void DrawFloor(GraphicsDevice gd, Effect e, WorldState state)
        {
            //assumes the effect and all its parameters have been set up already
            //we just need to get the right texture and offset
            var flrContent = Content.Content.Get().WorldFloors;

            e.Parameters["TexOffset"].SetValue(TexOffset[state.Zoom]*-1f);
            var tmat = TexMat[state.Rotation];
            e.Parameters["TexMatrix"].SetValue(tmat);

            var f = 0;
            foreach (var floor in Floors)
            {
                if (f++ >= state.Level) continue;

                var worldmat = Matrix.Identity * Matrix.CreateTranslation(0, 2.95f*(f-1)* 3 - Bp.BaseAlt * Bp.TerrainFactor * 3, 0);
                e.Parameters["World"].SetValue(worldmat);
                e.Parameters["Level"].SetValue((float)(f-1));
                foreach (var type in floor.GroupForTileType)
                {
                    bool pointFilter = false;
                    var dat = type.Value.GPUData;
                    if (dat == null) continue;
                    gd.Indices = dat;

                    var id = type.Key;

                    if (id == 0)
                    {
                        e.Parameters["UseTexture"].SetValue(false);
                        e.Parameters["IgnoreColor"].SetValue(false);
                    }
                    else
                    {

                        Texture2D SPR = null;
                        if (id >= 65504)
                        {
                            var pool = id >= 65520;
                            pointFilter = true;
                            if (!pool)
                            {
                                e.Parameters["UseTexture"].SetValue(false);
                                e.Parameters["IgnoreColor"].SetValue(false);

                                //quickly draw under the water
                                var pass2 = e.CurrentTechnique.Passes[WorldConfig.Current.PassOffset];
                                pass2.Apply();
                                gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, type.Value.GeomForOffset.Count * 2);

                                e.Parameters["UseTexture"].SetValue(true);
                                e.Parameters["IgnoreColor"].SetValue(true);
                                e.Parameters["World"].SetValue(worldmat * Matrix.CreateTranslation(0, 0.05f, 0));
                                id -= 65504;
                            } else
                            {
                                id -= 65520;
                            }

                            e.Parameters["TexMatrix"].SetValue(CounterTexMat[state.Rotation]);
                            
                            var rot = (int)state.Rotation;
                            rot = (4 - rot) % 4;
                            id = (ushort)(((id << rot) & 15) | (id >> (4 - rot)));
                            //pools & water are drawn with special logic, and may also be drawn slightly above the ground.

                            int baseSPR;
                            int frameNum = 0;
                            switch (state.Zoom)
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
                        else
                        {
                            var flr = flrContent.Get(id);

                            if (flr == null) continue;

                            switch (state.Zoom)
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
                        e.Parameters["BaseTex"].SetValue(SPR);
                    }

                    var pass = e.CurrentTechnique.Passes[WorldConfig.Current.PassOffset];
                    pass.Apply();
                    if (pointFilter)
                    {
                        gd.SamplerStates[0] = new SamplerState() { Filter = TextureFilter.Point };
                    }
                    gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, type.Value.GeomForOffset.Count * 2);

                    if (id == 0)
                    {
                        e.Parameters["UseTexture"].SetValue(true);
                        e.Parameters["IgnoreColor"].SetValue(true);
                    }
                    if (pointFilter)
                    {
                        e.Parameters["World"].SetValue(worldmat);
                        e.Parameters["TexMatrix"].SetValue(tmat);
                    }
                }
            }
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
