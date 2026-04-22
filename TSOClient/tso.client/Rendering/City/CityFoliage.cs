using FSO.Common.Utils;
using FSO.Content.Model;
using FSO.Files.RC;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.CompilerServices;

namespace FSO.Client.Rendering.City
{
    /// <summary>
    /// In 3D, foliage in the city view is not handled by per tile, but per 16x16 chunk.
    /// In each 16x16 chunk, appropriate trees are randomly placed for each tile, within the same output model.
    /// The trees are positioned using the same hermite interpolation that near-city geometry uses.
    /// </summary>
    public class CityFoliage : IDisposable
    {
        private struct SimpleRandom
        {
            private ulong RandomSeed;

            public SimpleRandom(ulong seed)
            {
                RandomSeed = seed;
            }

            /// <summary>
            /// Returns a random number between 0 and less than the specified maximum.
            /// </summary>
            /// <param name="max">The upper bound of the random number.</param>
            /// <returns></returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ulong Next(ulong max)
            {
                if (max == 0) return 0;
                RandomSeed ^= RandomSeed >> 12;
                RandomSeed ^= RandomSeed << 25;
                RandomSeed ^= RandomSeed >> 27;
                return (RandomSeed * (ulong)(2685821657736338717)) % max;
            }
        }

        private readonly struct TreeGroup(string name, int index, int count)
        {
            public readonly string Name = name;
            public readonly int Index = index;
            public readonly int Count = count;
        }

        public int ChunkSize = 16;
        public CityMap MapData;
        public Dictionary<int, CityFoliageChunk> Chunks = new Dictionary<int, CityFoliageChunk>();

        public DGRP3DVert[][] TreeVerts;
        public int[][] TreeInds;
        public readonly Matrix[] RotationMatrices;

        private readonly TreeGroup[] TreeGroups =
        [
            new("pine", 0, 4), //4 models
            new("tree", 4, 4), //4 models
            new("cactus", 8, 3), //3 models
            new("palm", 11, 4), //4 models
            new("snow", 15, 3) //3 models
        ];

        public CityFoliage()
        {
            TreeVerts = new DGRP3DVert[18][];
            TreeInds = new int[18][];

            foreach (var group in TreeGroups)
            {
                for (int i = 0; i < group.Count; i++)
                {
                    var model = LoadModel(group.Name + (i + 1) + ".obj");

                    TreeVerts[group.Index + i] = model.Item1;
                    TreeInds[group.Index + i] = model.Item2;
                }
            }

            RotationMatrices = new Matrix[16];
            int length = RotationMatrices.Length;
            for (int i = 0; i < length; i++)
            {
                RotationMatrices[i] = Matrix.CreateRotationY((MathF.PI * 2 * i) / length);
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

        private static readonly int[] TreeCounts = [1, 4, 7, 15];

        private static int O(int x, int y)
        {
            return (Math.Max(0, Math.Min(511, y)) * 512 + Math.Max(0, Math.Min(511, x)));
        }

        public void InvalidateChunks(Rectangle rect)
        {
            foreach (var chunkPair in Chunks)
            {
                int i = chunkPair.Key;
                var chunk = chunkPair.Value;

                var x = i % 32;
                var y = i / 32;

                var chunkRect = new Rectangle(x * 16, y * 16, 16, 16);

                if (rect.Intersects(chunkRect))
                {
                    chunk.Dirty = true;
                }
            }
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
            gd.BlendState = BlendState.NonPremultiplied;

            VertexShader.CurrentTechnique = VertexShader.Techniques[1];
            VertexShader.CurrentTechnique.Passes[5].Apply();

            var copy = terrain.OccupiedTiles;

            for (int y = Math.Max(0, cy-size); y<= Math.Min(31, cy + size); y++)
            {
                for (int x = Math.Max(0, cx - size); x<= Math.Min(31, cx + size); x++)
                {
                    var ind = y * 32 + x;
                    CityFoliageChunk chunk;
                    if (!Chunks.TryGetValue(ind, out chunk))
                    {
                        chunk = GenerateChunk(gd, x, y, copy);
                        Chunks.Add(chunk.Ind, chunk);
                    }
                    else if (chunk.ShouldRegenerate())
                    {
                        RegenerateChunk(chunk, gd, x, y, copy);
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

        private (DGRP3DVert[], int[]) GetChunkData(int x, int y, HashSet<int> noTrees)
        {
            var verts = new List<DGRP3DVert>();
            var inds = new List<int>();
            var md = MapData.ElevationData;
            var baseMat = Matrix.CreateScale(1 / 75f);

            var startx = x * ChunkSize;
            var endx = startx + ChunkSize;
            var starty = y * ChunkSize;
            var endy = starty + ChunkSize;

            var forestTypeData = MapData.ForestTypeData;
            var terrainTypeData = MapData.TerrainType;
            var forestDensityData = MapData.ForestDensityData;
            var roadData = MapData.RoadData;

            for (int oy = starty; oy < endy; oy++)
            {
                for (int ox = startx; ox < endx; ox++)
                {
                    var ind = oy * 512 + ox;
                    var forestType = forestTypeData[ind];
                    if (forestType != ForestType.NULL && !noTrees.Contains(ind))
                    {
                        if (forestType == 0 && terrainTypeData[ind] == TerrainType.SNOW) forestType = ForestType.SNOW;
                        var densityN = ((forestDensityData[ind] * 4) / 255);
                        if (densityN == 0) continue;
                        var density = TreeCounts[densityN - 1];
                        var rand = new SimpleRandom((ulong)(ind * 231458721));// new Random(ind);

                        var road = roadData[ind] & 15;
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

                        var group = TreeGroups[(int)forestType];

                        var fBase = group.Index;

                        for (int i = 0; i < density; i++)
                        {
                            var subtype = (int)rand.Next((ulong)group.Count);
                            var sx = (rand.Next(256) / 256f) * rangex + rangesx;
                            var sy = (rand.Next(256) / 256f) * rangey + rangesy;

                            //get tree height
                            float y1 = CityGeometry.Cubic(md[O(ox - 1, oy - 1)], md[O(ox - 1, oy)], md[O(ox - 1, oy + 1)], md[O(ox - 1, oy + 2)], sy, 0);
                            float y2 = CityGeometry.Cubic(md[O(ox, oy - 1)], md[O(ox, oy)], md[O(ox, oy + 1)], md[O(ox, oy + 2)], sy, 0);
                            float y3 = CityGeometry.Cubic(md[O(ox + 1, oy - 1)], md[O(ox + 1, oy)], md[O(ox + 1, oy + 1)], md[O(ox + 1, oy + 2)], sy, 0);
                            float y4 = CityGeometry.Cubic(md[O(ox + 2, oy - 1)], md[O(ox + 2, oy)], md[O(ox + 2, oy + 1)], md[O(ox + 2, oy + 2)], sy, 0);

                            var h = CityGeometry.Cubic(y1, y2, y3, y4, sx, 0);

                            //add the tree

                            var mat = baseMat * RotationMatrices[rand.Next((ulong)RotationMatrices.Length)];
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

            return ([..verts], [..inds]);
        }

        private void RegenerateChunk(CityFoliageChunk chunk, GraphicsDevice gd, int x, int y, HashSet<int> noTrees)
        {
            if (chunk.Dead)
            {
                return;
            }

            chunk.Regenerating = true;

            Task.Run(() =>
            {
                var (verts, inds) = GetChunkData(x, y, noTrees);
                GameThread.NextUpdate(state =>
                {
                    if (verts.Length > 0 && !chunk.Dead)
                    {
                        VertexBuffer vbuf = chunk.Vertices;
                        if (vbuf == null || vbuf.VertexCount != verts.Length)
                        {
                            vbuf?.Dispose();
                            vbuf = new VertexBuffer(gd, typeof(DGRP3DVert), verts.Length, BufferUsage.None);
                        }
                        vbuf.SetData(verts);

                        IndexBuffer ibuf = chunk.Indices;

                        if (ibuf == null || ibuf.IndexCount != inds.Length)
                        {
                            ibuf?.Dispose();
                            ibuf = new IndexBuffer(gd, IndexElementSize.ThirtyTwoBits, inds.Length, BufferUsage.None);
                        }
                        ibuf.SetData(inds);

                        chunk.Vertices = vbuf;
                        chunk.Indices = ibuf;
                    }
                    else
                    {
                        chunk.Vertices?.Dispose();
                        chunk.Indices?.Dispose();

                        chunk.Vertices = null;
                        chunk.Indices = null;
                    }

                    chunk.Regenerating = false;
                });
            });
        }

        public CityFoliageChunk GenerateChunk(GraphicsDevice gd, int x, int y, HashSet<int> noTrees)
        {
            var chunk = new CityFoliageChunk
            {
                Bounds = new BoundingBox(new Vector3(x * ChunkSize, 0, y * ChunkSize), new Vector3((x + 1) * 32, 255 / 12f, (y + 1) * 32))
            };

            RegenerateChunk(chunk, gd, x, y, noTrees);

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

        public bool Dirty;
        public bool Regenerating;

        public bool Dead;

        public bool ShouldRegenerate()
        {
            if (Dirty && !Regenerating)
            {
                Dirty = false;

                return true;
            }

            return false;
        }

        public void Dispose()
        {
            Vertices?.Dispose();
            Indices?.Dispose();
            Dead = true;
        }
    }
}
