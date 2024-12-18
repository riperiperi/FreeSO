using FSO.Common.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace FSO.Client.Rendering.City
{
    public class CityGeometry
    {
        private static Matrix RotToNormalXY = Matrix.CreateRotationZ((float)(Math.PI / 2));
        private static Matrix RotToNormalZY = Matrix.CreateRotationX(-(float)(Math.PI / 2));

        //draw order:
        //grass, sand, rock, snow, water
        public CityMapData MapData;
        public IndexBuffer[] LayerIndices = new IndexBuffer[5];
        public VertexBuffer[] LayerVertices = new VertexBuffer[5];
        public int[][] LayerSubPrims = new int[5][];
        public int[] LayerPrims = new int[5];
        public IndexBuffer RoadIndices;
        public VertexBuffer RoadVertices;
        public int RoadPrims;
        public int[] RoadSubPrims;
        public int Width;
        public int Height;
        public int Ready = -1;
        public int CurrentSlice = -1;

        private bool MeshRegenInProgress;
        private bool MeshDirty;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetElevationPoint(byte[] elevationData, int x, int y)
        {
            return elevationData[(y * 512 + x)] / 6.0f;
        }

        private Blend GetBlend(byte[] TerrainTypeData, int i, int j)
        {
            int[] edges;
            int sample;
            int t;

            edges = new int[] { -1, -1, -1, -1 };
            sample = TerrainTypeData[i * 512 + j];
            t = TerrainTypeData[Math.Abs((i - 1) * 512 + j)];

            if ((i - 1 >= 0) && (t > sample) && t != 255) edges[0] = t;
            t = TerrainTypeData[i * 512 + j + 1];
            if ((j + 1 < 512) && (t > sample) && t != 255) edges[1] = t;
            t = TerrainTypeData[Math.Min((i + 1), 511) * 512 + j];
            if ((i + 1 < 512) && (t > sample) && t != 255) edges[2] = t;
            t = TerrainTypeData[i * 512 + j - 1];
            if ((j - 1 >= 0) && (t > sample) && t != 255) edges[3] = t;


            int binary =
            ((edges[0] > -1) ? 0 : 2) |
            ((edges[1] > -1) ? 0 : 1) |
            ((edges[2] > -1) ? 0 : 8) |
            ((edges[3] > -1) ? 0 : 4);

            int remap = CityContent.FlagLayout[binary];
            var atlasPos = new Vector2((remap % 7) / 7f, (remap / 7) / 3f);

            int maxEdge = 4;

            for (int x = 0; x < 4; x++)
                if (edges[x] < maxEdge && edges[x] != -1) maxEdge = edges[x];

            Blend ReturnBlend = new Blend();
            ReturnBlend.Binary = binary;
            ReturnBlend.AtlasPosition = atlasPos;
            ReturnBlend.MaxEdge = maxEdge;

            return ReturnBlend;
        }

        private Vector3 GetNormalAt(byte[] elevationData, int x, int y)
        {
            var sum = new Vector3();

            if (x < 511)
            {
                var vec = new Vector3();
                vec.X = 1;
                vec.Y = GetElevationPoint(elevationData, x + 1, y) - GetElevationPoint(elevationData, x, y);
                vec = Vector3.Transform(vec, RotToNormalXY);
                sum += vec;
            }

            if (x > 1)
            {
                var vec = new Vector3();
                vec.X = 1;
                vec.Y = GetElevationPoint(elevationData, x, y) - GetElevationPoint(elevationData, x - 1, y);
                vec = Vector3.Transform(vec, RotToNormalXY);
                sum += vec;
            }

            if (y < 511)
            {
                var vec = new Vector3();
                vec.Z = 1;
                vec.Y = GetElevationPoint(elevationData, x, y + 1) - GetElevationPoint(elevationData, x, y);
                vec = Vector3.Transform(vec, RotToNormalZY);
                sum += vec;
            }

            if (y > 1)
            {
                var vec = new Vector3();
                vec.Z = 1;
                vec.Y = GetElevationPoint(elevationData, x, y) - GetElevationPoint(elevationData, x, y - 1);
                vec = Vector3.Transform(vec, RotToNormalZY);
                sum += vec;
            }
            if (sum != Vector3.Zero) sum.Normalize();
            return sum;
        }

        public static float Cubic(float v0, float v1, float v2, float v3, float fracy, float continuity)
        {
            float mu = fracy;
            float tension = 0.5f + continuity / 2;
            float bias = 0;

            float mu2 = mu * mu;
            float mu3 = mu2 * mu;
            float m0 = (v1 - v0) * (1 + bias) * (1 - tension) * (1 + continuity) / 2;
            m0 += (v2 - v1) * (1 - bias) * (1 - tension) * (1 - continuity) / 2;
            float m1 = (v2 - v1) * (1 + bias) * (1 - tension) * (1 - continuity) / 2;
            m1 += (v3 - v2) * (1 - bias) * (1 - tension) * (1 + continuity) / 2;
            float a0 = 2 * mu3 - 3 * mu2 + 1;
            float a1 = mu3 - 2 * mu2 + mu;
            float a2 = mu3 - mu2;
            float a3 = -2 * mu3 + 3 * mu2;

            return (a0 * v1 + a1 * m0 + a2 * m1 + a3 * v2);

            /*
            float A = (v3 - v2) - (v0 - v1);
            float B = (v0 - v1) - A;
            float C = v2 - v0;
            float D = v1;

            float linear = v2 * fracy + v1 * (1 - fracy);
            float cubic = (float)(A * Math.Pow(fracy, 3) + B * Math.Pow(fracy, 2) + C * fracy + D);

            float mix = Math.Abs(fracy*2 - 1f);

            return cubic * mix + linear * (1-mix);
            */
        }

        public void RegenMeshVerts(GraphicsDevice gd, bool async)
        {
            var indices = new List<int>[5];
            var vertices = new List<TLayerVertex>[5];

            var bDelta = new Vector2(1 / 7f, 1 / 3f);
            var rDelta = new Vector2(1f / CityContent.RoadWidth, 1f / CityContent.RoadHeight);

            for (int i = 0; i < 5; i++)
            {
                indices[i] = new List<int>(400000);
                vertices[i] = new List<TLayerVertex>(300000);
            }

            var roadIndices = new List<int>(150000);
            var roadVertices = new List<TLayerVertex>(100000);

            int xStart, xEnd;

            int chunkSize = 16;

            int yStart = 0, yEnd = 512;

            var chunkWidth = 512 / chunkSize;
            var chunkCount = chunkWidth * chunkWidth;

            int[][] newLayerSubPrims = new int[LayerSubPrims.Length][];
            for (int i = 0; i < 5; i++)
                newLayerSubPrims[i] = new int[chunkCount];
            int[] newRoadSubPrims = new int[chunkCount];

            Action generate = () =>
            {
                byte[] terrainType = MapData.TerrainType;
                byte[] roadData = MapData.RoadData;
                byte[] elevationData = MapData.ElevationData;

                var ci = 0;
                for (int cy = 0; cy < chunkWidth; cy++)
                {
                    for (int cx = 0; cx < chunkWidth; cx++)
                    {
                        yStart = cy * chunkSize;
                        yEnd = (cy + 1) * chunkSize;
                        var xLim = cx * chunkSize;
                        var xLimEnd = (cx + 1) * chunkSize;

                        for (int i = yStart; i < yEnd; i++)
                        {
                            if (i < 306) xStart = 306 - i;
                            else xStart = i - 306;
                            if (i < 205) xEnd = 307 + i;
                            else xEnd = 512 - (i - 205);
                            var rXE = xEnd;
                            var rXS = xStart;

                            int rXE2, rXS2;
                            int i2 = i + 1;
                            if (i2 < 306) rXS2 = 306 - i2;
                            else rXS2 = i2 - 306;
                            if (i2 < 205) rXE2 = 307 + i2;
                            else rXE2 = 512 - (i2 - 205);

                            var fadeRange = 10;
                            var fR = 1 / 9f;
                            xStart = Math.Max(xStart - fadeRange, xLim);
                            xEnd = Math.Min(xLimEnd, xEnd + fadeRange);

                            if (xEnd <= xStart) continue;

                            for (int j = xStart; j < xEnd; j++)
                            { //where the magic happens
                                var ex = Math.Min(Math.Max(rXS, j), rXE - 1);
                                var blendData = GetBlend(terrainType, i, ex); //gets information on what this tile blends into and what blend image to use for the alpha.
                                var type = terrainType[((i * 512) + ex)];
                                byte roadByte = roadData[(i * 512 + ex)];

                                //huge segment of code for generating triangles incoming
                                var norm1 = GetNormalAt(elevationData, Math.Min(rXE, Math.Max(rXS, j)), i);
                                var norm2 = GetNormalAt(elevationData, Math.Min(rXE, Math.Max(rXS, j + 1)), i);
                                var norm3 = GetNormalAt(elevationData, Math.Min(rXE2, Math.Max(rXS2, j + 1)), Math.Min(511, i + 1));
                                var norm4 = GetNormalAt(elevationData, Math.Min(rXE2, Math.Max(rXS2, j)), Math.Min(511, i + 1));

                                var pos1 = new Vector3(j, elevationData[(i * 512 + Math.Min(rXE, Math.Max(rXS, j)))] / 12.0f, i);
                                var pos2 = new Vector3(j + 1, elevationData[(i * 512 + Math.Min(rXE, Math.Max(rXS, j + 1)))] / 12.0f, i);
                                var pos3 = new Vector3(j + 1, elevationData[(Math.Min(511, i + 1) * 512 + Math.Min(rXE2, Math.Max(rXS2, j + 1)))] / 12.0f, i + 1);
                                var pos4 = new Vector3(j, elevationData[(Math.Min(511, i + 1) * 512 + Math.Min(rXE2, Math.Max(rXS2, j)))] / 12.0f, i + 1);

                                var trans1 = Math.Min(1, Math.Max(0, Math.Max(rXS - j, j - rXE) * fR));
                                var trans2 = Math.Min(1, Math.Max(0, Math.Max(rXS - (j + 1), (j + 1) - rXE) * fR));
                                var trans3 = Math.Min(1, Math.Max(0, Math.Max(rXS2 - (j + 1), (j + 1) - rXE2) * fR));
                                var trans4 = Math.Min(1, Math.Max(0, Math.Max(rXS2 - j, j - rXE2) * fR));

                                var baseInd = vertices[type].Count;
                                vertices[type].Add(new TLayerVertex()
                                {
                                    Position = pos1,
                                    Normal = norm1,
                                    Transparency = trans1,
                                    TextureCoord = new Vector2(j, i) / 4,
                                    MaskTextureCoord = new Vector2(-1, -1)
                                });

                                vertices[type].Add(new TLayerVertex()
                                {
                                    Position = pos2,
                                    Normal = norm2,
                                    Transparency = trans2,
                                    TextureCoord = new Vector2(j + 1, i) / 4,
                                    MaskTextureCoord = new Vector2(-1, -1)
                                });

                                vertices[type].Add(new TLayerVertex()
                                {
                                    Position = pos3,
                                    Normal = norm3,
                                    Transparency = trans3,
                                    TextureCoord = new Vector2(j + 1, i + 1) / 4,
                                    MaskTextureCoord = new Vector2(-1, -1)
                                });

                                vertices[type].Add(new TLayerVertex()
                                {
                                    Position = pos4,
                                    Normal = norm4,
                                    Transparency = trans4,
                                    TextureCoord = new Vector2(j, i + 1) / 4,
                                    MaskTextureCoord = new Vector2(-1, -1)
                                });

                                indices[type].Add(baseInd);
                                indices[type].Add(baseInd + 1);
                                indices[type].Add(baseInd + 2);
                                indices[type].Add(baseInd);
                                indices[type].Add(baseInd + 2);
                                indices[type].Add(baseInd + 3);

                                if (j > rXS && j < rXE)
                                {
                                    if (blendData.Binary < 15)
                                    {
                                        //add a blend face on top of this face (with a higher priority)
                                        var bOff = blendData.AtlasPosition; //texture used for blend alpha
                                        var blendT = blendData.MaxEdge;

                                        baseInd = vertices[blendT].Count;
                                        vertices[blendT].Add(new TLayerVertex()
                                        {
                                            Position = pos1,
                                            Normal = norm1,
                                            TextureCoord = new Vector2(j, i) / 4,
                                            Transparency = trans1,
                                            MaskTextureCoord = bOff
                                        });

                                        vertices[blendT].Add(new TLayerVertex()
                                        {
                                            Position = pos2,
                                            Normal = norm2,
                                            TextureCoord = new Vector2(j + 1, i) / 4,
                                            Transparency = trans2,
                                            MaskTextureCoord = new Vector2(bOff.X + bDelta.X, bOff.Y)
                                        });

                                        vertices[blendT].Add(new TLayerVertex()
                                        {
                                            Position = pos3,
                                            Normal = norm3,
                                            TextureCoord = new Vector2(j + 1, i + 1) / 4,
                                            Transparency = trans3,
                                            MaskTextureCoord = new Vector2(bOff.X + bDelta.X, bOff.Y + bDelta.Y)
                                        });

                                        vertices[blendT].Add(new TLayerVertex()
                                        {
                                            Position = pos4,
                                            Normal = norm4,
                                            Transparency = trans4,
                                            TextureCoord = new Vector2(j, i + 1) / 4,
                                            MaskTextureCoord = new Vector2(bOff.X, bOff.Y + bDelta.Y)
                                        });

                                        indices[blendT].Add(baseInd);
                                        indices[blendT].Add(baseInd + 1);
                                        indices[blendT].Add(baseInd + 2);
                                        indices[blendT].Add(baseInd);
                                        indices[blendT].Add(baseInd + 2);
                                        indices[blendT].Add(baseInd + 3);
                                    }

                                    var normalRoad = roadByte & 15;
                                    var cornerRoad = roadByte >> 4;
                                    if (normalRoad > 0)
                                    {
                                        //add a road face on top of this face
                                        var roadInd = CityContent.RoadLayout[normalRoad];
                                        var roadOff = new Vector2(
                                            (roadInd % CityContent.RoadWidth) / (float)CityContent.RoadWidth,
                                            (roadInd / CityContent.RoadWidth) / (float)CityContent.RoadHeight
                                            );
                                        baseInd = roadVertices.Count;
                                        roadVertices.Add(new TLayerVertex()
                                        {
                                            Position = pos1,
                                            Normal = norm1,
                                            TextureCoord = new Vector2(roadOff.X + rDelta.X, roadOff.Y),
                                            MaskTextureCoord = new Vector2(-1, -1)
                                        });

                                        roadVertices.Add(new TLayerVertex()
                                        {
                                            Position = pos2,
                                            Normal = norm2,
                                            TextureCoord = roadOff,
                                            MaskTextureCoord = new Vector2(-1, -1)
                                        });

                                        roadVertices.Add(new TLayerVertex()
                                        {
                                            Position = pos3,
                                            Normal = norm3,
                                            TextureCoord = new Vector2(roadOff.X, roadOff.Y + rDelta.Y),
                                            MaskTextureCoord = new Vector2(-1, -1)
                                        });

                                        roadVertices.Add(new TLayerVertex()
                                        {
                                            Position = pos4,
                                            Normal = norm4,
                                            TextureCoord = new Vector2(roadOff.X + rDelta.X, roadOff.Y + rDelta.Y),
                                            MaskTextureCoord = new Vector2(-1, -1)
                                        });

                                        roadIndices.Add(baseInd);
                                        roadIndices.Add(baseInd + 1);
                                        roadIndices.Add(baseInd + 2);
                                        roadIndices.Add(baseInd);
                                        roadIndices.Add(baseInd + 2);
                                        roadIndices.Add(baseInd + 3);
                                    }

                                    if (cornerRoad > 0)
                                    {
                                        //add a road face on top of this face
                                        var roadInd = CityContent.RoadCLayout[cornerRoad];
                                        var roadOff = new Vector2(
                                            (roadInd % CityContent.RoadWidth) / (float)CityContent.RoadWidth,
                                            (roadInd / CityContent.RoadWidth) / (float)CityContent.RoadHeight
                                            );
                                        baseInd = roadVertices.Count;
                                        roadVertices.Add(new TLayerVertex()
                                        {
                                            Position = pos1,
                                            Normal = norm1,
                                            TextureCoord = new Vector2(roadOff.X + rDelta.X, roadOff.Y),
                                            MaskTextureCoord = new Vector2(-1, -1)
                                        });

                                        roadVertices.Add(new TLayerVertex()
                                        {
                                            Position = pos2,
                                            Normal = norm2,
                                            TextureCoord = roadOff,
                                            MaskTextureCoord = new Vector2(-1, -1)
                                        });

                                        roadVertices.Add(new TLayerVertex()
                                        {
                                            Position = pos3,
                                            Normal = norm3,
                                            TextureCoord = new Vector2(roadOff.X, roadOff.Y + rDelta.Y),
                                            MaskTextureCoord = new Vector2(-1, -1)
                                        });

                                        roadVertices.Add(new TLayerVertex()
                                        {
                                            Position = pos4,
                                            Normal = norm4,
                                            TextureCoord = new Vector2(roadOff.X + rDelta.X, roadOff.Y + rDelta.Y),
                                            MaskTextureCoord = new Vector2(-1, -1)
                                        });

                                        roadIndices.Add(baseInd);
                                        roadIndices.Add(baseInd + 1);
                                        roadIndices.Add(baseInd + 2);
                                        roadIndices.Add(baseInd);
                                        roadIndices.Add(baseInd + 2);
                                        roadIndices.Add(baseInd + 3);
                                    }
                                }
                            }
                        }

                        for (int i = 0; i < 5; i++)
                        {
                            newLayerSubPrims[i][ci] = indices[i].Count;
                        }
                        newRoadSubPrims[ci] = roadIndices.Count;
                        ci++;
                    }
                }
            };

            Action upload = () =>
            {
                LayerSubPrims = newLayerSubPrims;
                RoadSubPrims = newRoadSubPrims;

                //upload to gpu
                for (int i = 0; i < 5; i++)
                {
                    LayerIndices[i]?.Dispose();
                    LayerVertices[i]?.Dispose();
                    if (vertices[i].Count == 0)
                    {
                        LayerIndices[i] = null;
                        LayerVertices[i] = null;
                    }
                    else
                    {
                        LayerIndices[i] = new IndexBuffer(gd, IndexElementSize.ThirtyTwoBits, indices[i].Count, BufferUsage.None);
                        LayerIndices[i].SetData(indices[i].ToArray());
                        LayerVertices[i] = new VertexBuffer(gd, typeof(TLayerVertex), vertices[i].Count, BufferUsage.None);
                        LayerVertices[i].SetData(vertices[i].ToArray());
                    }
                    LayerPrims[i] = indices[i].Count / 3;
                }

                RoadIndices?.Dispose();
                RoadVertices?.Dispose();
                if (roadVertices.Count > 0)
                {
                    RoadIndices = new IndexBuffer(gd, IndexElementSize.ThirtyTwoBits, roadIndices.Count, BufferUsage.None);
                    RoadIndices.SetData(roadIndices.ToArray());
                    RoadVertices = new VertexBuffer(gd, typeof(TLayerVertex), roadVertices.Count, BufferUsage.None);
                    RoadVertices.SetData(roadVertices.ToArray());
                    RoadPrims = roadIndices.Count / 3;
                }
            };


            if (async)
            {
                if (!MeshRegenInProgress)
                {
                    MeshDirty = false;
                    MeshRegenInProgress = true;
                    Task.Run(() =>
                    {
                        generate();
                    }).ContinueWith((x) =>
                    {
                        GameThread.NextUpdate((state) =>
                        {
                            upload();

                            MeshRegenInProgress = false;

                            if (MeshDirty)
                            {
                                RegenMeshVerts(gd, true);
                            }
                        });
                    });
                }
                else
                {
                    MeshDirty = true;
                }
            }
            else
            {
                generate();
                upload();
            }
        }

        private int O(int x, int y, int minx, int maxx)
        {
            return (Math.Max(0, Math.Min(511, y)) * 512 + Math.Max(minx, Math.Min(maxx, x)));
        }

        public void SubRegenMeshVerts(GraphicsDevice gd, Rectangle? range, int subdiv, int cpos)
        {
            CurrentSlice = cpos;
            var indices = new List<int>[5];
            var vertices = new List<TLayerVertex>[5];

            var bDelta = new Vector2(1 / 7f, 1 / 3f);
            var rDelta = new Vector2(1f / CityContent.RoadWidth, 1f / CityContent.RoadHeight);

            for (int i = 0; i < 5; i++)
            {
                indices[i] = new List<int>(100000);
                vertices[i] = new List<TLayerVertex>(20000);
            }

            var roadIndices = new List<int>(50000);
            var roadVertices = new List<TLayerVertex>(10000);

            int xStart, xEnd;

            int index = 0;
            int yStart = 0, yEnd = 512;
            int subd1 = subdiv + 1;
            float subd1f = 1f / subdiv;
            int vertCount = subd1 * subd1;

            if (range.HasValue)
            {
                yStart = range.Value.Y;
                yEnd = range.Value.Bottom;
            }

            Task.Run(() =>
            {
                byte[] terrainType = MapData.TerrainType;
                byte[] roadData = MapData.RoadData;
                byte[] elevationData = MapData.ElevationData;

                for (int i = yStart; i < yEnd; i++)
                {
                    if (i < 306)
                        xStart = 306 - i;
                    else
                        xStart = i - 306;
                    if (i < 205)
                        xEnd = 307 + i;
                    else
                        xEnd = 512 - (i - 205);

                    var rXE = xEnd;
                    var rXS = xStart;

                    int rXE2, rXS2;
                    int i2 = i + 1;
                    if (i2 < 306) rXS2 = 306 - i2;
                    else rXS2 = i2 - 306;
                    if (i2 < 205) rXE2 = 307 + i2;
                    else rXE2 = 512 - (i2 - 205);

                    var fadeRange = 10;
                    var fR = 1 / 9f;

                    xStart -= fadeRange;
                    xEnd += fadeRange;

                    if (range.HasValue)
                    {
                        xStart = Math.Max(range.Value.X, xStart);
                        xEnd = Math.Min(range.Value.Right, xEnd);
                    }

                    for (int j = xStart; j < xEnd; j++)
                    { //where the magic happens
                        var ex = Math.Min(Math.Max(rXS, j), rXE - 1);
                        var blendData = GetBlend(terrainType, i, ex); //gets information on what this tile blends into and what blend image to use for the alpha.
                        var type = terrainType[((i * 512) + ex)];
                        byte roadByte = roadData[(i * 512 + ex)];

                        //huge segment of code for generating triangles incoming
                        var norm1 = GetNormalAt(elevationData, Math.Min(rXE, Math.Max(rXS, j)), i);
                        var norm2 = GetNormalAt(elevationData, Math.Min(rXE, Math.Max(rXS, j + 1)), i);
                        var norm3 = GetNormalAt(elevationData, Math.Min(rXE2, Math.Max(rXS2, j + 1)), Math.Min(511, i + 1));
                        var norm4 = GetNormalAt(elevationData, Math.Min(rXE2, Math.Max(rXS2, j)), Math.Min(511, i + 1));

                        var trans1 = Math.Min(1, Math.Max(0, Math.Max(rXS - j, j - rXE) * fR));
                        var trans2 = Math.Min(1, Math.Max(0, Math.Max(rXS - (j + 1), (j + 1) - rXE) * fR));
                        var trans3 = Math.Min(1, Math.Max(0, Math.Max(rXS2 - (j + 1), (j + 1) - rXE2) * fR));
                        var trans4 = Math.Min(1, Math.Max(0, Math.Max(rXS2 - j, j - rXE2) * fR));

                        var md = elevationData;

                        var bOff = blendData.AtlasPosition; //texture used for blend alpha
                        var blendT = blendData.MaxEdge;
                        var baseI = indices[type];

                        var blendI = (blendData.Binary < 15) ? indices[blendT] : null;

                        var normalRoad = roadByte & 15;
                        var cornerRoad = roadByte >> 4;

                        var yEdge = (j == range.Value.X) ? -1f : 0f;
                        var yEdge2 = (j == range.Value.Right - 1) ? -1f : 0f;

                        var xEdge = (i == range.Value.Y) ? -1f : 0f;
                        var xEdge2 = (i == range.Value.Bottom - 1) ? -1f : 0f;


                        var d = new float[]
                        {
                            md[O(j - 1, i - 1, rXS, rXE)], md[O(j - 1, i, rXS, rXE)], md[O(j - 1, i + 1, rXS2, rXE2)], md[O(j - 1, i + 2, rXS2, rXE2)],
                            md[O(j, i - 1, rXS, rXE)], md[O(j, i, rXS, rXE)], md[O(j, i + 1, rXS2, rXE2)], md[O(j, i + 2, rXS2, rXE2)],
                            md[O(j + 1, i - 1, rXS, rXE)], md[O(j + 1, i, rXS, rXE)], md[O(j + 1, i + 1, rXS2, rXE2)], md[O(j + 1, i + 2, rXS2, rXE2)],
                            md[O(j + 2, i - 1, rXS, rXE)], md[O(j + 2, i, rXS, rXE)], md[O(j + 2, i + 1, rXS2, rXE2)], md[O(j + 2, i + 2, rXS2, rXE2)],
                        };

                        var normalTile = (j > rXS && j < rXE);

                        var yi = 0f;
                        for (int y = 0; y < subd1; y++)
                        {
                            var lXE = (yi * xEdge2) + ((1 - yi) * xEdge);
                            var xi = 0f;
                            for (int x = 0; x < subd1; x++)
                            {
                                float y1 = Cubic(d[0], d[1], d[2], d[3], yi, yEdge);
                                float y2 = Cubic(d[4], d[5], d[6], d[7], yi, yEdge);
                                float y3 = Cubic(d[8], d[9], d[10], d[11], yi, yEdge2);
                                float y4 = Cubic(d[12], d[13], d[14], d[15], yi, yEdge2);

                                var h = Cubic(y1, y2, y3, y4, xi, lXE);

                                var lerpNX = Vector3.Lerp(norm1, norm2, xi);
                                var lerpNX2 = Vector3.Lerp(norm4, norm3, xi);

                                var lerpN = Vector3.Lerp(lerpNX, lerpNX2, yi);

                                float lerpT = 0;
                                if (!normalTile)
                                {
                                    var lerpTX = trans1 * (1 - xi) + trans2 * xi;
                                    var lerpTX2 = trans4 * (1 - xi) + trans3 * xi;

                                    lerpT = lerpTX * (1 - yi) + lerpTX2 * yi;
                                }

                                var pos = new Vector3(j + xi, h / 12f, i + yi);
                                var baseInd = vertices[type].Count;
                                vertices[type].Add(new TLayerVertex()
                                {
                                    Position = pos,
                                    Normal = lerpN,
                                    TextureCoord = new Vector2(j + xi, i + yi) / 4,
                                    MaskTextureCoord = new Vector2(-1, -1),
                                    Transparency = lerpT
                                });

                                var addIndices = x < subdiv && y < subdiv;
                                if (addIndices)
                                {
                                    baseI.Add(baseInd);
                                    baseI.Add(baseInd + 1);
                                    baseI.Add(baseInd + subd1);

                                    baseI.Add(baseInd + subd1);
                                    baseI.Add(baseInd + 1);
                                    baseI.Add(baseInd + subd1 + 1);
                                }

                                if (normalTile)
                                {
                                    if (blendData.Binary < 15)
                                    {
                                        var blendInd = vertices[blendT].Count;
                                        vertices[blendT].Add(new TLayerVertex()
                                        {
                                            Position = pos,
                                            Normal = lerpN,
                                            TextureCoord = new Vector2(j + xi, i + yi) / 4,
                                            MaskTextureCoord = bOff + bDelta * new Vector2(xi, yi),
                                            Transparency = lerpT
                                        });

                                        if (addIndices)
                                        {
                                            blendI.Add(blendInd);
                                            blendI.Add(blendInd + 1);
                                            blendI.Add(blendInd + subd1);

                                            blendI.Add(blendInd + subd1);
                                            blendI.Add(blendInd + 1);
                                            blendI.Add(blendInd + subd1 + 1);
                                        }
                                    }

                                    var roadInd = roadVertices.Count;
                                    var roadMul = ((normalRoad > 0) ? 1 : 0) + ((cornerRoad > 0) ? 1 : 0);
                                    if (normalRoad > 0)
                                    {
                                        //add a road face on top of this face
                                        var roadIndi = CityContent.RoadLayout[normalRoad];
                                        var roadOff = new Vector2(
                                            (roadIndi % CityContent.RoadWidth) / (float)CityContent.RoadWidth,
                                            (roadIndi / CityContent.RoadWidth) / (float)CityContent.RoadHeight
                                            );

                                        roadVertices.Add(new TLayerVertex()
                                        {
                                            Position = pos,
                                            Normal = lerpN,
                                            TextureCoord = roadOff + rDelta * new Vector2(1 - xi, yi),
                                            MaskTextureCoord = new Vector2(-1, -1)
                                        });

                                        if (addIndices)
                                        {
                                            roadIndices.Add(roadInd);
                                            roadIndices.Add(roadInd + 1 * roadMul);
                                            roadIndices.Add(roadInd + subd1 * roadMul);

                                            roadIndices.Add(roadInd + subd1 * roadMul);
                                            roadIndices.Add(roadInd + 1 * roadMul);

                                            roadIndices.Add(roadInd + (subd1 + 1) * roadMul);
                                        }

                                    }

                                    if (cornerRoad > 0)
                                    {
                                        roadInd = roadVertices.Count;
                                        //add a road face on top of this face
                                        var roadIndi = CityContent.RoadCLayout[cornerRoad];
                                        var roadOff = new Vector2(
                                            (roadIndi % CityContent.RoadWidth) / (float)CityContent.RoadWidth,
                                            (roadIndi / CityContent.RoadWidth) / (float)CityContent.RoadHeight
                                            );

                                        roadVertices.Add(new TLayerVertex()
                                        {
                                            Position = pos,
                                            Normal = lerpN,
                                            TextureCoord = roadOff + rDelta * new Vector2(1 - xi, yi),
                                            MaskTextureCoord = new Vector2(-1, -1)
                                        });

                                        if (addIndices)
                                        {
                                            roadIndices.Add(roadInd);
                                            roadIndices.Add(roadInd + 1 * roadMul);
                                            roadIndices.Add(roadInd + subd1 * roadMul);

                                            roadIndices.Add(roadInd + subd1 * roadMul);
                                            roadIndices.Add(roadInd + 1 * roadMul);

                                            roadIndices.Add(roadInd + (subd1 + 1) * roadMul);
                                        }
                                    }
                                }
                                xi += subd1f;
                            }
                            yi += subd1f;
                        }

                    }
                }
            }).ContinueWith(x =>
            {
                GameThread.NextUpdate(task =>
                {
                    //upload to gpu
                    for (int i = 0; i < 5; i++)
                    {
                        LayerIndices[i]?.Dispose();
                        LayerVertices[i]?.Dispose();
                        LayerIndices[i] = new IndexBuffer(gd, IndexElementSize.ThirtyTwoBits, Math.Max(1, indices[i].Count), BufferUsage.None);
                        if (indices[i].Count > 0) LayerIndices[i].SetData(indices[i].ToArray());
                        LayerVertices[i] = new VertexBuffer(gd, typeof(TLayerVertex), Math.Max(3, vertices[i].Count), BufferUsage.None);
                        if (vertices[i].Count > 0) LayerVertices[i].SetData(vertices[i].ToArray());
                        LayerPrims[i] = indices[i].Count / 3;
                    }

                    RoadIndices?.Dispose();
                    RoadVertices?.Dispose();
                    RoadIndices = new IndexBuffer(gd, IndexElementSize.ThirtyTwoBits, Math.Max(1, roadIndices.Count), BufferUsage.None);
                    if (roadIndices.Count > 0) RoadIndices.SetData(roadIndices.ToArray());
                    RoadVertices = new VertexBuffer(gd, typeof(TLayerVertex), Math.Max(3, roadVertices.Count), BufferUsage.None);
                    if (roadVertices.Count > 0) RoadVertices.SetData(roadVertices.ToArray());
                    RoadPrims = roadIndices.Count / 3;
                    Ready = cpos;
                });
            });
        }

        public SamplerState RoadSampler = new SamplerState()
        {
            AddressU = TextureAddressMode.Clamp,
            AddressV = TextureAddressMode.Clamp,
            Filter = TextureFilter.MinLinearMagPointMipLinear
        };

        public void DrawAll(GraphicsDevice gd, CityContent content, Effect vs, Effect ps, int vsn, int psn)
        {
            //assume a lot of the parameters have already been set
            //we just need to switch the textures and draw all the different buffers
            gd.BlendState = BlendState.AlphaBlend;

            ps.Parameters["VertexColorTex"].SetValue(content.VertexColor);
            ps.Parameters["UseVertexColor"].SetValue(true);
            vs.Parameters["DepthBias"].SetValue(0f);

            for (int i = 0; i < 5; i++)
            {
                ps.Parameters["TextureAtlasTex"].SetValue(content.TerrainTextures[i]);
                var trans = (1 - (i - 1) / 2) * 2 + ((4 - i) % 2);
                ps.Parameters["TransAtlasTex"].SetValue((i == 0) ? null : content.TransAtlas[trans]);

                if (i == 4 && psn != 1)
                {
                    ps.CurrentTechnique = ps.Techniques[3];
                    ps.Parameters["BigWTex"].SetValue(content.BigWNormal);
                    ps.Parameters["SmallWTex"].SetValue(content.SmallWNormal);
                }

                vs.CurrentTechnique.Passes[vsn].Apply();
                ps.CurrentTechnique.Passes[psn].Apply();

                gd.SamplerStates[0] = SamplerState.LinearWrap;
                gd.SamplerStates[1] = SamplerState.LinearClamp;

                gd.SetVertexBuffer(LayerVertices[i]);
                gd.Indices = LayerIndices[i];
                if (LayerPrims[i] > 0) gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, LayerPrims[i]);
                if (i == 4 && psn != 1)
                {
                    //HACK HACK HACK HACK
                    //Monogame OpenGL DOES NOT like these textures staying in samplers 3 and 4.
                    //for some reason they cause textures to randomly black out. null them immediately.
                    ps.Parameters["BigWTex"].SetValue((Texture2D)null);
                    ps.Parameters["SmallWTex"].SetValue((Texture2D)null);
                    ps.CurrentTechnique.Passes[0].Apply();

                    ps.CurrentTechnique = ps.Techniques[2];
                }
            }
            //draw road verts

            ps.Parameters["TextureAtlasTex"].SetValue(content.RoadAtlas);
            vs.Parameters["DepthBias"].SetValue(0f);
            ps.Parameters["UseVertexColor"].SetValue(false);

            vs.CurrentTechnique.Passes[vsn].Apply();
            ps.CurrentTechnique.Passes[psn].Apply();
            gd.SamplerStates[0] = RoadSampler;

            gd.SetVertexBuffer(RoadVertices);
            gd.Indices = RoadIndices;
            if (RoadPrims > 0) gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, RoadPrims);

            var rts = gd.GetRenderTargets();
            gd.SetRenderTargets(rts);
            //gd.SetRenderTarget((RenderTarget2D)rts.FirstOrDefault()?.RenderTarget);
        }

        private void DrawChunk(GraphicsDevice gd, int ind, int chunkWidth, int[] ranges, int primCount)
        {

            //before double chunk size block
            if (ind != 0)
            {
                var end = ranges[ind - 1];
                if (end != 0) gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, end / 3);
            }

            //after row 1 of the chunk block, and before row 2
            var midstart = ranges[ind + 1];
            var midend = ranges[ind + chunkWidth - 1];
            if (midend - midstart > 0)
                gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, midstart, (midend - midstart) / 3);

            //after the chunk block

            var afterstart = ranges[ind + 1 + chunkWidth];
            var afterend = primCount * 3;
            if (afterend - afterstart > 0)
                gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, afterstart, (afterend - afterstart) / 3);
        }

        public void DrawSlice(GraphicsDevice gd, CityContent content, Effect vs, Effect ps, int vsn, int psn, int ind, int chunkSize)
        {
            //assume a lot of the parameters have already been set
            //we just need to switch the textures and draw all the different buffers
            if (ind == -1)
            {
                DrawAll(gd, content, vs, ps, vsn, psn);
                return;
            }
            gd.BlendState = BlendState.AlphaBlend;

            ps.Parameters["VertexColorTex"].SetValue(content.VertexColor);
            ps.Parameters["UseVertexColor"].SetValue(true);
            vs.Parameters["DepthBias"].SetValue(0f);

            var chunkWidth = 512 / chunkSize;

            for (int i = 0; i < 5; i++)
            {
                if (LayerVertices[i] == null) continue;
                ps.Parameters["TextureAtlasTex"].SetValue(content.TerrainTextures[i]);
                var trans = (1 - (i - 1) / 2) * 2 + ((4 - i) % 2);
                ps.Parameters["TransAtlasTex"].SetValue((i == 0) ? null : content.TransAtlas[trans]);

                if (i == 4)
                {
                    ps.CurrentTechnique = ps.Techniques[3];
                    ps.Parameters["BigWTex"].SetValue(content.BigWNormal);
                    ps.Parameters["SmallWTex"].SetValue(content.SmallWNormal);
                }

                ps.CurrentTechnique.Passes[psn].Apply();
                vs.CurrentTechnique.Passes[vsn].Apply();

                gd.SamplerStates[0] = SamplerState.LinearWrap;
                gd.SamplerStates[1] = SamplerState.LinearClamp;

                gd.SetVertexBuffer(LayerVertices[i]);
                gd.Indices = LayerIndices[i];

                var ranges = LayerSubPrims[i];

                DrawChunk(gd, ind, chunkWidth, ranges, LayerPrims[i]);
                if (i == 4)
                {
                    //HACK HACK HACK HACK
                    //Monogame OpenGL DOES NOT like these textures staying in samplers 3 and 4.
                    //for some reason they cause textures to randomly black out. null them immediately.
                    ps.Parameters["BigWTex"].SetValue((Texture2D)null);
                    ps.Parameters["SmallWTex"].SetValue((Texture2D)null);
                    ps.CurrentTechnique.Passes[0].Apply();

                    ps.CurrentTechnique = ps.Techniques[2];
                }
            }

            //draw road verts

            ps.Parameters["TextureAtlasTex"].SetValue(content.RoadAtlas);
            vs.Parameters["DepthBias"].SetValue(0f);
            ps.Parameters["UseVertexColor"].SetValue(false);

            ps.CurrentTechnique.Passes[psn].Apply();
            vs.CurrentTechnique.Passes[vsn].Apply();
            gd.SamplerStates[0] = RoadSampler;

            gd.SetVertexBuffer(RoadVertices);
            gd.Indices = RoadIndices;

            DrawChunk(gd, ind, chunkWidth, RoadSubPrims, RoadPrims);
        }

        public void Dispose()
        {
            foreach (var buf in LayerIndices) buf?.Dispose();
            foreach (var buf in LayerVertices) buf?.Dispose();
            RoadIndices?.Dispose();
            RoadVertices?.Dispose();
        }
    }
}