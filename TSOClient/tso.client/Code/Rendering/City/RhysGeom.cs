using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.Code.Utils;
using Microsoft.Xna.Framework;

namespace TSOClient.Code.Rendering.City
{
    public class RhysGeom : ICityGeom
    {
        public TerrainVertex[] Vertices { get; internal set; }
        public int[] Indexes { get; internal set; }
        public IndexBuffer IndexBuffer { get; internal set; }
        public VertexBuffer VertexBuffer { get; internal set; }
        public int PrimitiveCount { get; internal set; }



        #region ICityGeom Members


        public void Process(CityData city)
        {
            /**var verts = []
		        var texUV = []
		        var texUV2 = []
		        var texUV3 = []
		        var texUVB = []
		        var data = getDataForImage(images["elevation.bmp"])
		        elevData = data;
		        fDensityData = getDataForImage(images["forestdensity.bmp"])
		        fTypeData = new Uint32Array(getDataForImage(images["foresttype.bmp"]).buffer)
		        var tData = new Uint32Array(getDataForImage(images["terraintype.bmp"]).buffer)
		        typeData = tData;
		        for (i=0; i<512; i++) {
			        if (i<306) var xStart = 306-i
			        else var xStart = (i-306)
			        if (i<205) var xEnd = 307+i
			        else var xEnd = 512-(i-205)
			        for (var j=xStart; j<xEnd; j++) { //where the magic happens
            **/


            var mesh = new ThreeDMesh<TerrainVertex>();
            var elevation = city.RawElevationPixels;
            var vertexColors = city.VertexColorPixels;
            var terrainTypes = city.RawTerrainTypePixels;
            var terrainSpread = 4.0f;
            //We have a sprite sheet that contains 5 terrain types
            var terrainSheetSpread = (1.0f/5.0f) / terrainSpread;

            for (var y = 0; y < 512; y++)
            {
                var xStart = y < 306 ? (306 - y) : (y - 306);
                var xEnd = y < 205 ? (307 + y) : (512-(y-205));

                for (var x = xStart; x < xEnd; x++)
                {
                    var pixelOffset = (y * 512) + x;
                    var terrainType = terrainTypes[pixelOffset];
                    var vertexColor = vertexColors[pixelOffset];

                    var terrainTextureUV = new Vector2(city.GetTerrainType(terrainType) / 5.0f, 0.0f);
                    terrainTextureUV += new Vector2(terrainSheetSpread * (x % terrainSpread), (terrainSheetSpread / 2.0f) * (y % terrainSpread));


                    var tl = new TerrainVertex(
                        new Vector3(x, elevation[(y*512)+x].R/12.0f, y),
                        terrainTextureUV,
                        vertexColor,
                        Vector2.Zero,
                        Vector2.Zero
                    );

                    var tr = new TerrainVertex(
                        new Vector3(x + 1, elevation[(y * 512) + Math.Min(511, x + 1)].R / 12.0f, y),
                        terrainTextureUV + new Vector2(terrainSheetSpread, 0.0f),
                        vertexColor,
                        Vector2.Zero,
                        Vector2.Zero
                    );

                    var br = new TerrainVertex(
                        new Vector3(x + 1, elevation[((Math.Min(511, y+1)*512)+Math.Min(511, x+1))].R / 12.0f, y + 1),
                        terrainTextureUV + new Vector2(terrainSheetSpread, terrainSheetSpread),
                        vertexColor,
                        Vector2.Zero,
                        Vector2.Zero
                    );

                    var bl = new TerrainVertex(
                        new Vector3(x, elevation[(Math.Min(511, y + 1) * 512) + x].R / 12.0f, y + 1),
                        terrainTextureUV + new Vector2(0.0f, terrainSheetSpread),
                        vertexColor,
                        Vector2.Zero,
                        Vector2.Zero
                    );

                    mesh.AddQuad(tl, tr, br, bl);
                }
            }

            Vertices = mesh.GetVertexes();
            Indexes = mesh.GetIndexes();
            PrimitiveCount = mesh.PrimitiveCount;
        }



        public void CreateBuffer(Microsoft.Xna.Framework.Graphics.GraphicsDevice gd)
        {
            VertexBuffer = new VertexBuffer(gd, TerrainVertex.SizeInBytes * Vertices.Length, BufferUsage.WriteOnly);
            VertexBuffer.SetData(Vertices);

            IndexBuffer = new IndexBuffer(gd, typeof(int), Indexes.Length, BufferUsage.WriteOnly);
            IndexBuffer.SetData(Indexes);
        }

        public void Draw(Microsoft.Xna.Framework.Graphics.GraphicsDevice gd)
        {

            gd.Vertices[0].SetSource(VertexBuffer, 0, TerrainVertex.SizeInBytes);
            gd.VertexDeclaration = new VertexDeclaration(gd, TerrainVertex.VertexElements);
            gd.Indices = IndexBuffer;
            gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, Vertices.Length, 0, PrimitiveCount);
        }

        #endregion














        public float CellWidth
        {
            get
            {
                return 1;
            }
            set
            {
            }
        }

        public float CellHeight
        {
            get
            {
                return 1;
            }
            set
            {
            }
        }

        public float CellYScale
        {
            get
            {
                return 1;
            }
            set
            {
            }
        }
    }
}
