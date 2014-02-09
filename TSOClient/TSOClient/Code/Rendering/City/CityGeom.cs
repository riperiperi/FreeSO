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
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace TSOClient.Code.Rendering.City
{
    public class CityGeom : IDisposable, ICityGeom
    {
        /// <summary>
        /// Vertices for the map
        /// </summary>

        public TerrainVertex[] Vertices { get; internal set; }
        public int[] Indexes { get; internal set; }
        public IndexBuffer IndexBuffer { get; internal set; }
        public VertexBuffer VertexBuffer { get; internal set; }
        public int PrimitiveCount { get; internal set; }
        public int VertexPerTile = 12;

        public float CellWidth { get; set; }
        public float CellHeight { get; set; }
        public float BorderWidth { get; set; }
        public float BorderHeight { get; set; }

        public float CellYScale { get; set; }
        public float TerrainSpread = 0.05f;

        /// <summary>
        /// How many textures are in the terain sheet, aka how many terrain types
        /// </summary>
        public float TerrainSheetSize = 5.0f;

        public int Width { get; internal set; }
        public int Height { get; internal set; }


        public void GetTileVertices(int x, int y, TerrainVertex[] target)
        {
            var offset = ((y * Width + x) * VertexPerTile);
            for (var i = 0; i < VertexPerTile; i++)
            {
                target[i] = Vertices[offset + i];
            }
        }


        protected Vector2[] CalculateTexCoord(int x, int y, byte terrainType)
        {
            var terrainXO = (terrainType / TerrainSheetSize);
            var terrainSize = (1.0f / TerrainSheetSize);

            var txOrigin = terrainXO + ((x * (TerrainSpread * terrainSize)) % terrainSize);
            var txMid = txOrigin + ((TerrainSpread * (terrainSize / 2)) % terrainSize);
            var txEnd = txOrigin + ((TerrainSpread * terrainSize) % terrainSize);

            var tyOrigin = y * TerrainSpread;
            var tyMid = (y + 0.5f) * TerrainSpread;
            var tyEnd = (y + 1) * TerrainSpread;

            var textureP0 = new Vector2(txMid, tyMid);
            var textureP1 = new Vector2(txOrigin, tyOrigin);
            var textureP2 = new Vector2(txEnd, tyOrigin);
            var textureP3 = new Vector2(txEnd, tyEnd);
            var textureP4 = new Vector2(txOrigin, tyEnd);

            return new Vector2[] {
                textureP0,
                textureP1,
                textureP2,
                textureP3,
                textureP4
            };
        }

        /// <summary>
        /// Do the work of generating the city geom
        /// </summary>
        /// <param name="city"></param>
        public void Process(CityData city)
        {
            //Cleanup if someone is trying to reuse this object
            Dispose();
            var now = DateTime.Now.Ticks;

            Width = city.Width;
            Height = city.Height;




            var vertexList = new List<TerrainVertex>();
            var indexList = new List<int>();




            BorderWidth = (CellWidth / 4) / 2;
            BorderHeight = (CellHeight / 4) / 2;

            var spanX = CellWidth + (BorderWidth * 2);
            var spanY = CellHeight + (BorderHeight * 2);



            var textureMap = new TextureMapper();
            textureMap.TerrainSheetSize = 5.0f;

            /** Build vertex & index structures **/
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var offset = (y * city.Width) + x;

                    /** Settings **/
                    var elevation = city.Elevation[offset];
                    var vertexColor = city.VertexColor[offset];
                    var terrainType = city.Terrain[offset];
                    var blendIndex = city.BlendMap[offset];
                    var backTerrainType = city.BackTerrain[offset];

                    textureMap.TerrainType = terrainType;



                    //Main points
                    var mainElevation = city.GetElevation(x, y, CellYScale);
                    var northElevation = city.GetElevation(x, y, NeighbourDir.North, mainElevation, CellYScale);
                    var eastElevation = city.GetElevation(x, y, NeighbourDir.East, mainElevation, CellYScale);
                    var southElevation = city.GetElevation(x, y, NeighbourDir.South, mainElevation, CellYScale);
                    var westElevation = city.GetElevation(x, y, NeighbourDir.West, mainElevation, CellYScale);
                    var northWestElevation = city.GetElevation(x, y, NeighbourDir.NorthWest, mainElevation, CellYScale);
                    var northEastElevation = city.GetElevation(x, y, NeighbourDir.NorthEast, mainElevation, CellYScale);
                    var southEastElevation = city.GetElevation(x, y, NeighbourDir.SouthEast, mainElevation, CellYScale);
                    var southWestElevation = city.GetElevation(x, y, NeighbourDir.SouthWest, mainElevation, CellYScale);



                    var startIndex = vertexList.Count;
                    var tex = new Vector2(0.5f, 0.5f);

                    if (mainElevation == northElevation &&
                        mainElevation == eastElevation &&
                        mainElevation == southElevation &&
                        mainElevation == westElevation &&
                        mainElevation == northWestElevation &&
                        mainElevation == northEastElevation &&
                        mainElevation == southEastElevation &&
                        mainElevation == southWestElevation)
                    {
                        /** We can just use 1 quad for this tile **/
                        var fullTL = new Vector3((x * spanX) - BorderWidth, -(y * spanY) - BorderHeight, mainElevation);
                        var fullTR = new Vector3(fullTL.X + spanX, fullTL.Y, mainElevation);
                        var fullBL = new Vector3(fullTL.X, fullTL.Y - spanY, mainElevation);
                        var fullBR = new Vector3(fullTL.X + spanX, fullTL.Y - spanY, mainElevation);

                        textureMap.Position(x, y, fullTL, fullBR);

                        vertexList.Add(new TerrainVertex(fullTL, textureMap.MapTerrain(ref fullTL), vertexColor, tex, tex)); //0
                        vertexList.Add(new TerrainVertex(fullTR, textureMap.MapTerrain(ref fullTR), vertexColor, tex, tex)); //1
                        vertexList.Add(new TerrainVertex(fullBR, textureMap.MapTerrain(ref fullBR), vertexColor, tex, tex)); //2
                        vertexList.Add(new TerrainVertex(fullBL, textureMap.MapTerrain(ref fullBL), vertexColor, tex, tex)); //3

                        indexList.Add(startIndex);
                        indexList.Add(startIndex + 1);
                        indexList.Add(startIndex + 2);

                        indexList.Add(startIndex + 2);
                        indexList.Add(startIndex + 3);
                        indexList.Add(startIndex);

                        continue;
                    }




                    var mainTL = new Vector3(x * spanX, -(y * spanY), mainElevation);
                    var mainTR = new Vector3(mainTL.X + CellWidth, mainTL.Y, mainElevation);
                    var mainBL = new Vector3(mainTL.X, mainTL.Y - CellHeight, mainElevation);
                    var mainBR = new Vector3(mainTL.X + CellWidth, mainTL.Y - CellHeight, mainElevation);




                    /** West elevation **/
                    var westElevationMid = (westElevation + mainElevation) / 2;
                    var borderTL_BL = new Vector3(mainTL.X - BorderWidth, mainTL.Y, westElevationMid);
                    var borderBL_TL = new Vector3(mainTL.X - BorderWidth, mainBL.Y, westElevationMid);

                    /** East elevation **/
                    var eastElevationMid = (eastElevation + mainElevation) / 2;
                    var borderTR_BR = new Vector3(mainTR.X + BorderWidth, mainTR.Y, eastElevationMid);
                    var borderBR_TR = new Vector3(mainBR.X + BorderWidth, mainBR.Y, eastElevationMid);

                    /** North elevation **/
                    var northElevationMid = (northElevation + mainElevation) / 2;
                    var borderTL_TR = new Vector3(mainTL.X, mainTL.Y + BorderHeight, northElevationMid);
                    var borderTR_TL = new Vector3(mainTR.X, mainTR.Y + BorderHeight, northElevationMid);

                    /** South elevation **/
                    var southElevationMid = (southElevation + mainElevation) / 2;
                    var borderBL_BR = new Vector3(mainBL.X, mainBL.Y - BorderHeight, southElevationMid);
                    var borderBR_BL = new Vector3(mainBR.X, mainBR.Y - BorderHeight, southElevationMid);

                    var northWestElevationMid = (northWestElevation + northElevation + westElevation + mainElevation) / 4;
                    var borderTL_TL = new Vector3(mainTL.X - BorderWidth, mainTL.Y + BorderHeight, northWestElevationMid);

                    var northEastElevationMid = (northEastElevation + northElevation + eastElevation + mainElevation) / 4;
                    var borderTR_TR = new Vector3(mainTR.X + BorderWidth, mainTR.Y + BorderHeight, northEastElevationMid);

                    var southEastElevationMid = (southEastElevation + eastElevation + southElevation + mainElevation) / 4;
                    var borderBR_BR = new Vector3(mainBR.X + BorderWidth, mainBR.Y - BorderHeight, southEastElevationMid);

                    var southWestElevationMid = (southWestElevation + southElevation + westElevation + mainElevation) / 4;
                    var borderBL_BL = new Vector3(mainBL.X - BorderWidth, mainBL.Y - BorderHeight, southWestElevationMid);



                    textureMap.Position(x, y, borderTL_TL, borderBR_BR);


                    vertexList.Add(new TerrainVertex(mainTL, textureMap.MapTerrain(ref mainTL), vertexColor, tex, tex)); //0
                    vertexList.Add(new TerrainVertex(mainTR, textureMap.MapTerrain(ref mainTR), vertexColor, tex, tex)); //1
                    vertexList.Add(new TerrainVertex(mainBR, textureMap.MapTerrain(ref mainBR), vertexColor, tex, tex)); //2
                    vertexList.Add(new TerrainVertex(mainBL, textureMap.MapTerrain(ref mainBL), vertexColor, tex, tex)); //3
                    vertexList.Add(new TerrainVertex(borderTL_BL, textureMap.MapTerrain(ref borderTL_BL), vertexColor, tex, tex)); //4
                    vertexList.Add(new TerrainVertex(borderBL_TL, textureMap.MapTerrain(ref borderBL_TL), vertexColor, tex, tex)); //5
                    vertexList.Add(new TerrainVertex(borderTR_BR, textureMap.MapTerrain(ref borderTR_BR), vertexColor, tex, tex)); //6
                    vertexList.Add(new TerrainVertex(borderBR_TR, textureMap.MapTerrain(ref borderBR_TR), vertexColor, tex, tex)); //7
                    vertexList.Add(new TerrainVertex(borderTL_TR, textureMap.MapTerrain(ref borderTL_TR), vertexColor, tex, tex)); //8
                    vertexList.Add(new TerrainVertex(borderTR_TL, textureMap.MapTerrain(ref borderTR_TL), vertexColor, tex, tex)); //9
                    vertexList.Add(new TerrainVertex(borderBL_BR, textureMap.MapTerrain(ref borderBL_BR), vertexColor, tex, tex)); //10
                    vertexList.Add(new TerrainVertex(borderBR_BL, textureMap.MapTerrain(ref borderBR_BL), vertexColor, tex, tex)); //11
                    vertexList.Add(new TerrainVertex(borderTL_TL, textureMap.MapTerrain(ref borderTL_TL), vertexColor, tex, tex)); //12
                    vertexList.Add(new TerrainVertex(borderTR_TR, textureMap.MapTerrain(ref borderTR_TR), vertexColor, tex, tex)); //13
                    vertexList.Add(new TerrainVertex(borderBR_BR, textureMap.MapTerrain(ref borderBR_BR), vertexColor, tex, tex)); //14
                    vertexList.Add(new TerrainVertex(borderBL_BL, textureMap.MapTerrain(ref borderBL_BL), vertexColor, tex, tex)); //15


                    /** Main tile **/
                    indexList.Add(startIndex);
                    indexList.Add(startIndex + 1);
                    indexList.Add(startIndex + 2);
                    indexList.Add(startIndex + 2);
                    indexList.Add(startIndex + 3);
                    indexList.Add(startIndex);

                    if (y > 0)
                    {
                        /** Top flap **/
                        indexList.Add(startIndex + 8);
                        indexList.Add(startIndex + 9);
                        indexList.Add(startIndex + 1);

                        indexList.Add(startIndex + 1);
                        indexList.Add(startIndex + 0);
                        indexList.Add(startIndex + 8);
                    }

                    if (y < Height - 1)
                    {
                        /** Bottom flap **/
                        indexList.Add(startIndex + 3);
                        indexList.Add(startIndex + 2);
                        indexList.Add(startIndex + 11);

                        indexList.Add(startIndex + 11);
                        indexList.Add(startIndex + 10);
                        indexList.Add(startIndex + 3);

                        if (x > 0)
                        {
                            /** Bottom left corner **/
                            indexList.Add(startIndex + 5);
                            indexList.Add(startIndex + 3);
                            indexList.Add(startIndex + 10);

                            indexList.Add(startIndex + 10);
                            indexList.Add(startIndex + 15);
                            indexList.Add(startIndex + 5);
                        }
                        if (x < Width - 1)
                        {
                            /** Bottom right corner **/
                            indexList.Add(startIndex + 2);
                            indexList.Add(startIndex + 7);
                            indexList.Add(startIndex + 14);

                            indexList.Add(startIndex + 14);
                            indexList.Add(startIndex + 11);
                            indexList.Add(startIndex + 2);
                        }
                    }

                    if (x > 0)
                    {
                        /** Left flap **/
                        indexList.Add(startIndex + 4);
                        indexList.Add(startIndex);
                        indexList.Add(startIndex + 3);

                        indexList.Add(startIndex + 3);
                        indexList.Add(startIndex + 5);
                        indexList.Add(startIndex + 4);

                        if (y > 0)
                        {
                            /** Top left corner **/
                            indexList.Add(startIndex + 12);
                            indexList.Add(startIndex + 8); //tl_bl
                            indexList.Add(startIndex); //tl_tr

                            indexList.Add(startIndex);
                            indexList.Add(startIndex + 4);
                            indexList.Add(startIndex + 12);
                        }
                    }
                    if (x < Width - 1)
                    {
                        /** Right flap **/
                        indexList.Add(startIndex + 1);
                        indexList.Add(startIndex + 6);
                        indexList.Add(startIndex + 7);

                        indexList.Add(startIndex + 7);
                        indexList.Add(startIndex + 2);
                        indexList.Add(startIndex + 1);

                        if (y > 0)
                        {
                            /** Top right corner **/
                            indexList.Add(startIndex + 9);
                            indexList.Add(startIndex + 13);
                            indexList.Add(startIndex + 6);

                            indexList.Add(startIndex + 6);
                            indexList.Add(startIndex + 1);
                            indexList.Add(startIndex + 9);
                        }
                    }

                }
            }


            Vertices = vertexList.ToArray();
            Indexes = indexList.ToArray();
            PrimitiveCount = Indexes.Length / 3;

            System.Diagnostics.Debug.WriteLine("Took : " + (DateTime.Now.Ticks - now) + " ticks");
        }

        /// <summary>
        /// Store the vertices in a vertex buffer
        /// </summary>
        /// <param name="gd"></param>
        public void CreateBuffer(GraphicsDevice gd)
        {
            VertexBuffer = new VertexBuffer(gd, TerrainVertex.SizeInBytes * Vertices.Length, BufferUsage.WriteOnly);
            VertexBuffer.SetData(Vertices);

            IndexBuffer = new IndexBuffer(gd, typeof(int), Indexes.Length, BufferUsage.WriteOnly);
            IndexBuffer.SetData(Indexes);
        }


        public void Draw(GraphicsDevice gd)
        {
            gd.Vertices[0].SetSource(VertexBuffer, 0, TerrainVertex.SizeInBytes);
            gd.VertexDeclaration = new VertexDeclaration(gd, TerrainVertex.VertexElements);
            gd.Indices = IndexBuffer;
            gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, Vertices.Length, 0, PrimitiveCount);
        }

        #region IDisposable Members

        /// <summary>
        /// Cleans up the various objects used by the geom object
        /// </summary>
        public void Dispose()
        {
        }

        #endregion
    }



    public class TextureMapper
    {
        private float minX;
        private float minY;
        private float ratioX;
        private float ratioY;
        private int X;
        private int Y;

        public float TerrainSpread = 0.05f;
        public byte TerrainType;
        private float _TerrainSheetSize;
        public float TerrainSheetSize
        {
            get
            {
                return _TerrainSheetSize;
            }
            set
            {
                _TerrainSheetSize = value;
                TerrainSheetCellSize = 1 / value;
            }
        }
        private float TerrainSheetCellSize;


        public TextureMapper()
        {
        }

        public void Position(int x, int y, Vector3 TL, Vector3 BR)
        {
            X = x;
            Y = y;

            minX = TL.X;
            minY = TL.Y;

            ratioX = BR.X - TL.X;
            ratioY = BR.Y - TL.Y;
        }

        public void Position(int x, int y, Vector2 TL, Vector2 BR)
        {
            X = x;
            Y = y;

            minX = TL.X;
            minY = TL.Y;

            ratioX = BR.X - TL.X;
            ratioY = BR.Y - TL.Y;
        }

        public Vector2 MapTerrain(ref Vector3 point)
        {
            var xPosition = (point.X - minX) / ratioX;
            var yPosition = (point.Y - minY) / ratioY;


            /**
            var terrainXO = (terrainType / TerrainSheetSize);
            var terrainSize = (1.0f / TerrainSheetSize);

            var txOrigin = terrainXO + ((x * (TerrainSpread * terrainSize)) % terrainSize);
            var txMid = txOrigin + ((TerrainSpread * (terrainSize / 2)) % terrainSize);
            var txEnd = txOrigin + ((TerrainSpread * terrainSize) % terrainSize);

            var tyOrigin = y * TerrainSpread;
            var tyMid = (y + 0.5f) * TerrainSpread;
            var tyEnd = (y + 1) * TerrainSpread;

            var textureP0 = new Vector2(txMid, tyMid);
            var textureP1 = new Vector2(txOrigin, tyOrigin);
            var textureP2 = new Vector2(txEnd, tyOrigin);
            var textureP3 = new Vector2(txEnd, tyEnd);
            var textureP4 = new Vector2(txOrigin, tyEnd);
**/

            var xTerrainStart = (TerrainSheetCellSize * TerrainType);
            var xCellOffset = X * (TerrainSpread * TerrainSheetCellSize);
            var xVertexOffset = (TerrainSpread * xPosition);

            xPosition = (xCellOffset + xVertexOffset) % TerrainSheetCellSize;
            xPosition += xTerrainStart;


            yPosition = (Y * TerrainSpread) + (yPosition * TerrainSpread);

            return new Vector2(xPosition, yPosition);
        }
    }
}
