using FSO.Common.Utils;
using FSO.Files.RC;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Rendering.City
{
    /// <summary>
    /// In 3D, foliage in the city view is not handled by per tile, but per 16x16 chunk.
    /// In each 16x16 chunk, appropriate trees are randomly placed for each tile, within the same output model.
    /// The trees are positioned using the same hermite interpolation that near-city geometry uses.
    /// </summary>
    public class CityFoliage : IDisposable
    {
        public int ChunkSize = 16;
        public CityMapData MapData;
        public Dictionary<int, CityFoliageChunk> Chunks = new Dictionary<int, CityFoliageChunk>();

        public DGRP3DVert[][] TreeVerts;
        public int[][] TreeInds;

        public string[] TreeGroups = new string[]
        {
            "pine",
            "tree",
            "palm",
            "cactus", //3
            "snow", //3
        };

        public CityFoliage()
        {
            TreeVerts = new DGRP3DVert[18][];
            TreeInds = new int[18][]; 
            for (int i=0; i<18; i++)
            {
                var snow = (i >= 15);
                var model = LoadModel(TreeGroups[(snow)?(4):(i / 4)] + (snow?(i-14):((i % 4) + 1)) + ".obj");
                //var tree = Content.Content.Get().RCMeshes.Get(TreeGroups[i/4]+((i%4)+1)+".fsom");

                //var geom = tree.Geoms[0].ElementAt(0).Value;
                TreeVerts[i] = model.Item1;
                TreeInds[i] = model.Item2;
            }

        }

        public Tuple<DGRP3DVert[], int[]> LoadModel(string model)
        {
            OBJ obj;
            using (var str = File.Open("Content/3D/" + model, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                obj = new OBJ(str);
            }

            var indices = obj.FacesByObjgroup.First(x => x.Value.Count > 0).Value;
            var outVerts = new List<DGRP3DVert>();
            var outInds = new List<int>();
            var dict = new Dictionary<Tuple<int, int>, int>();

            foreach (var ind in indices)
            {
                var tup = new Tuple<int, int>(ind[0], ind[1]);
                int targ;
                if (!dict.TryGetValue(tup, out targ))
                {
                    //add a vertex
                    targ = outVerts.Count;
                    var vert = new DGRP3DVert(obj.Vertices[ind[0] - 1], Vector3.Zero, obj.TextureCoords[ind[1] - 1]);
                    vert.TextureCoordinate.Y = 1 - vert.TextureCoordinate.Y;
                    outVerts.Add(vert);
                    dict[tup] = targ;
                }
                outInds.Add(targ);
            }

            var triBase = new int[outInds.Count / 3][];
            for (int i = 0; i < triBase.Length; i++) triBase[i] = new int[] { outInds[i * 3], outInds[i * 3 + 1], outInds[i * 3 + 2] };

            var ordered = triBase.OrderBy(x => outVerts[x[0]].Position.Y + outVerts[x[1]].Position.Y + outVerts[x[2]].Position.Y);
            outInds.Clear();
            foreach (var item in ordered) outInds.AddRange(item);

            return new Tuple<DGRP3DVert[], int[]>(outVerts.ToArray(), outInds.ToArray());
        }

        private Dictionary<Color, int> ForestTypes = new Dictionary<Color, int>()
        {
            { new Color(0, 0x6A, 0x28), 0 },   //heavy forest
            { new Color(0, 0xEB, 0x42), 1},   //light forest
            { new Color(255, 0xFC, 0), 2 },   //palm
            { new Color(255, 0, 0), 3},   //cacti
            { new Color(0, 0, 0), -1}  //nothing; no forest
        };

        public int[] TreeCounts = new int[] { 1, 4, 7, 15 };

        private int O(int x, int y)
        {
            return (Math.Max(0, Math.Min(511, y)) * 512 + Math.Max(0, Math.Min(511, x)));
        }

        public void Draw(Terrain terrain, GraphicsDevice gd, CityContent content, Effect VertexShader, Effect PixelShader, int passIndex, int size, BoundingFrustum frustrum)
        {
            var camPos = terrain.Camera.CalculateR();

            var cx = (int)Math.Round(camPos.X / 16);
            var cy = (int)Math.Round(camPos.Y / 16);

            var invalid = Chunks.Keys.Where(i =>
            {
                var x = i % 32;
                var y = i / 32;
                return (x < cx - 2) || (x > cx + 2) || (y < cy - 2) || (y > cy + 2);
            }).ToList();

            foreach (var c in invalid)
            {
                var chunk = Chunks[c];
                chunk.Dispose();
                Chunks.Remove(c);
            }

            gd.RasterizerState = RasterizerState.CullNone;
            gd.BlendState = BlendState.NonPremultiplied;
            var genScale = 1/((terrain.Camera.LotSquish - 1)/2 + 1);
            VertexShader.Parameters["ObjModel"].SetValue(Matrix.CreateScale(genScale, genScale*terrain.Camera.LotSquish, genScale));
            VertexShader.Parameters["DepthBias"].SetValue(-0.12f * terrain.Camera.DepthBiasScale);
            VertexShader.Parameters["HeightVScale"].SetValue(1f);// 1f / terrain.Camera.LotSquish);

            PixelShader.CurrentTechnique = PixelShader.Techniques[1];
            PixelShader.Parameters["ObjTex"].SetValue(content.TreeTex);
            PixelShader.CurrentTechnique.Passes[passIndex].Apply();

            gd.SamplerStates[1] = SamplerState.AnisotropicClamp;
            gd.BlendState = BlendState.AlphaBlend;

            VertexShader.CurrentTechnique = VertexShader.Techniques[1];
            VertexShader.CurrentTechnique.Passes[5].Apply();

            var copy = new HashSet<int>(terrain.LotTileLookup.Keys.Select(i => (int)i.Y*512+(int)i.X));

            for (int y = Math.Max(0, cy-size); y<= Math.Min(31, cy + size); y++)
            {
                for (int x = Math.Max(0, cx - size); x<= Math.Min(31, cx + size); x++)
                {
                    var ind = y * 32 + x;
                    CityFoliageChunk chunk;
                    if (!Chunks.TryGetValue(ind, out chunk)) {
                        chunk = GenerateChunk(gd, x, y, copy);
                        Chunks.Add(chunk.Ind, chunk);
                    }

                    if (chunk.Indices != null && chunk.Bounds.Intersects(frustrum))
                    {

                        //var col = (new Vector4(m_TintColor.R / 255.0f, m_TintColor.G / 255.0f, m_TintColor.B / 255.0f, 1) * 1.25f) / fsof.NightLightColor.ToVector4();
                        //PixelShader.Parameters["LightCol"].SetValue(col);


                        gd.SetVertexBuffer(chunk.Vertices);
                        gd.Indices = chunk.Indices;

                        gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, chunk.Indices.IndexCount / 3);
                    }
                }
            }
        }

        public CityFoliageChunk GenerateChunk(GraphicsDevice gd, int x, int y, HashSet<int> noTrees)
        {
            var chunk = new CityFoliageChunk();
            chunk.Bounds = new BoundingBox(new Vector3(x * ChunkSize, 0, y * ChunkSize), new Vector3((x+1) * 32, 255 / 12f, (y+1) * 32));

            Task.Run(() =>
            {
                var verts = new List<DGRP3DVert>();
                var inds = new List<int>();
                var md = MapData.ElevationData;
                var baseMat = Matrix.CreateScale(1 / 75f);

                var startx = x * ChunkSize;
                var endx = startx + ChunkSize;
                var starty = y * ChunkSize;
                var endy = starty + ChunkSize;

                for (int oy = starty; oy < endy; oy++)
                {
                    for (int ox = startx; ox < endx; ox++)
                    {
                        var ind = oy * 512 + ox;
                        var forestType = ForestTypes[MapData.ForestTypeData[ind]];
                        if (forestType != -1 && !noTrees.Contains(ind))
                        {
                            if (forestType == 0 && MapData.TerrainType[ind] == 3) forestType = 4;
                            var densityN = ((MapData.ForestDensityData[ind] * 4) / 255);
                            if (densityN == 0) continue;
                            var density = TreeCounts[densityN - 1];
                            var rand = new Random(ind);

                            var road = MapData.RoadData[ind] & 15;
                            float rangesx = 0;
                            float rangesy = 0;
                            float rangex = 1;
                            float rangey = 1;

                            if ((road & 1) > 0)
                            {
                                rangesx += 0.15f;
                                rangex -= 0.15f;
                            }
                            if ((road & 2) > 0)
                            {
                                rangey -= 0.15f;
                            }
                            if ((road & 4) > 0)
                            {
                                rangex -= 0.15f;
                            }
                            if ((road & 8) > 0)
                            {
                                rangesy += 0.15f;
                                rangey -= 0.15f;
                            }
                            var fBase = Math.Min(15, forestType * 4);

                            for (int i = 0; i < density; i++)
                            {
                                var subtype = rand.Next((forestType >= 3) ? 3 : 4);
                                var sx = (float)rand.NextDouble() * rangex + rangesx;
                                var sy = (float)rand.NextDouble() * rangey + rangesy;

                                //get tree height
                                float y1 = CityGeometry.Cubic(md[O(ox - 1, oy - 1)], md[O(ox - 1, oy)], md[O(ox - 1, oy + 1)], md[O(ox - 1, oy + 2)], sy, 0);
                                float y2 = CityGeometry.Cubic(md[O(ox, oy - 1)], md[O(ox, oy)], md[O(ox, oy + 1)], md[O(ox, oy + 2)], sy, 0);
                                float y3 = CityGeometry.Cubic(md[O(ox + 1, oy - 1)], md[O(ox + 1, oy)], md[O(ox + 1, oy + 1)], md[O(ox + 1, oy + 2)], sy, 0);
                                float y4 = CityGeometry.Cubic(md[O(ox + 2, oy - 1)], md[O(ox + 2, oy)], md[O(ox + 2, oy + 1)], md[O(ox + 2, oy + 2)], sy, 0);

                                var h = CityGeometry.Cubic(y1, y2, y3, y4, sx, 0);

                                //add the tree

                                var mat = baseMat * Matrix.CreateRotationY((float)(Math.PI * 2 * rand.NextDouble()));
                                var pos = new Vector3(ox + sx, h / 12f, oy + sy);

                                var model = fBase + subtype;
                                var baseV = verts.Count;
                                foreach (var vert in TreeVerts[model])
                                {
                                    var vCopy = vert;
                                    vCopy.Position = Vector3.Transform(vCopy.Position, mat);
                                    vCopy.Normal = pos;
                                    verts.Add(vCopy);
                                }

                                foreach (var tind in TreeInds[model]) inds.Add(tind + baseV);
                            }
                        }
                    }
                }
                GameThread.NextUpdate(state =>
                {
                    if (verts.Count > 0 && !chunk.Dead)
                    {
                        var vbuf = new VertexBuffer(gd, typeof(DGRP3DVert), verts.Count, BufferUsage.None);
                        vbuf.SetData(verts.ToArray());
                        var ibuf = new IndexBuffer(gd, IndexElementSize.ThirtyTwoBits, inds.Count, BufferUsage.None);
                        ibuf.SetData(inds.ToArray());

                        chunk.Vertices = vbuf;
                        chunk.Indices = ibuf;
                    }
                });
            });
            chunk.X = x;
            chunk.Y = y;
            chunk.Ind = y * 32 + x;

            return chunk;
        }

        public void Dispose()
        {
            foreach (var chunk in Chunks)
            {
                chunk.Value.Dispose();
            }
            Chunks.Clear();
        }
    }

    public class CityFoliageChunk
    {
        public int Ind;
        public int X;
        public int Y;
        public VertexBuffer Vertices;
        public IndexBuffer Indices;
        public BoundingBox Bounds;

        public bool Dead;

        public void Dispose()
        {
            Vertices?.Dispose();
            Indices?.Dispose();
            Dead = true;
        }
    }
}
