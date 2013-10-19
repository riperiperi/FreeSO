using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CityRenderer
{
    public class Terrain
    {
        GraphicsDevice m_GraphicsDevice;
        private Texture2D m_Elevation, m_TerrainType, m_ForestType, m_ForestDensity;

        public Texture2D Atlas, TransAtlas;
        private Texture2D[] m_TransA = new Texture2D[30], TransB = new Texture2D[30];
        private Texture2D m_Ground, Rock, Snow, Water, Sand, Forest;

        private Dictionary<Color, int> m_ToBlendPrio = new Dictionary<Color, int>();
        private Dictionary<int, double> m_Prio2Map = new Dictionary<int, double>();
        private Dictionary<string, double[]> m_EdgeBLookup = new Dictionary<string, double[]>();
        private Dictionary<double, double[]> m_AtlasOffPrio = new Dictionary<double, double[]>();

        private int m_Width, m_Height;

        public Terrain(GraphicsDevice GfxDevice, int CityNumber)
        {
            string CityStr = (CityNumber > 10) ? "city_00" + CityNumber.ToString() : "city_000" + CityNumber.ToString();

            m_GraphicsDevice = GfxDevice;
            m_Elevation = Texture2D.FromFile(GfxDevice, CityStr + "\\elevation.bmp");
            m_TerrainType = Texture2D.FromFile(GfxDevice, CityStr + "\\terraintype.bmp");
            m_ForestType = Texture2D.FromFile(GfxDevice, CityStr + "\\foresttype.bmp");
            m_ForestDensity = Texture2D.FromFile(GfxDevice, CityStr + "\\forestdensity.bmp");

            m_Ground = Texture2D.FromFile(GfxDevice, "gamedata\\terrain\\newformat\\gr.tga");
            Rock = Texture2D.FromFile(GfxDevice, "gamedata\\terrain\\newformat\\rk.tga");
            Water = Texture2D.FromFile(GfxDevice, "gamedata\\terrain\\newformat\\wt.tga");
            Sand = Texture2D.FromFile(GfxDevice, "gamedata\\terrain\\newformat\\sd.tga");
            Snow = Texture2D.FromFile(GfxDevice, "gamedata\\terrain\\newformat\\sn.tga");
            Forest = Texture2D.FromFile(GfxDevice, "gamedata\\farzoom\\forest00a.tga");

            string Num;

            for (int x = 0; x < 30; x = x + 2)
            {
                Num = ZeroPad((x / 2).ToString(), 2);
                m_TransA[x] = Texture2D.FromFile(GfxDevice, "gamedata\\terrain\\newformat\\transa" + Num + "a.tga");
                m_TransA[x + 1] = Texture2D.FromFile(GfxDevice, "gamedata\\terrain\\newformat\\transa" + Num + "b.tga");
                //Debug.Write(x / 2 + "\r\n");
            }

            for (int x = 0; x < 30; x = x + 2)
            {
                Num = ZeroPad((x / 2).ToString(), 2);
                TransB[x] = Texture2D.FromFile(GfxDevice, "gamedata\\terrain\\newformat\\transb" + Num + "a.tga");
                TransB[x + 1] = Texture2D.FromFile(GfxDevice, "gamedata\\terrain\\newformat\\transb" + Num + "b.tga");
            }

            m_Width = m_Elevation.Width;
            m_Height = m_Elevation.Height;
        }

        private string ZeroPad(string Str, int NumZeroes)
        {
            while (Str.Length < NumZeroes)
                Str = "0" + Str;

            return Str;
        }

        public void Initialize()
        {
            m_ToBlendPrio.Add(new Color(0, 255, 0), 0);     //grass
            m_ToBlendPrio.Add(new Color(255, 0, 0), 4);     //rock
            m_ToBlendPrio.Add(new Color(12, 0, 255), 3);    //water
            m_ToBlendPrio.Add(new Color(255, 255, 255), 2); //snow
            m_ToBlendPrio.Add(new Color(255, 255, 0), 1);   //sand
            m_ToBlendPrio.Add(new Color(0, 0, 0), -1);      //nothing, don't blend into this

            m_Prio2Map.Add(0, 0);
            m_Prio2Map.Add(1, 0.5);
            m_Prio2Map.Add(2, 0.75);
            m_Prio2Map.Add(3, 0.25);
            m_Prio2Map.Add(4, 0);

            m_EdgeBLookup.Add("0000", new double[] { 15 / 16, 0 }); //none

            m_EdgeBLookup.Add("1000", new double[] { 13 / 16, 0 }); //top
            m_EdgeBLookup.Add("0100", new double[] { 14 / 16, 0 }); //right
            m_EdgeBLookup.Add("0010", new double[] { 7 / 16, 0 }); //bottom
            m_EdgeBLookup.Add("0001", new double[] { 11 / 16, 0 }); //left

            m_EdgeBLookup.Add("1010", new double[] { 5 / 16, 0 }); //top + bottom
            m_EdgeBLookup.Add("0101", new double[] { 10 / 16, 0 }); //left + right

            m_EdgeBLookup.Add("1001", new double[] { 9 / 16, 0 }); //left + top
            m_EdgeBLookup.Add("1100", new double[] { 12 / 16, 0 }); //right + top
            m_EdgeBLookup.Add("0110", new double[] { 6 / 16, 0 }); //right + bottom
            m_EdgeBLookup.Add("0011", new double[] { 3 / 16, 0 }); //left + bottom

            m_EdgeBLookup.Add("1101", new double[] { 8 / 16, 0 }); //all but bottom
            m_EdgeBLookup.Add("1110", new double[] { 4 / 16, 0 }); //all but left
            m_EdgeBLookup.Add("0111", new double[] { 2 / 16, 0 }); //all but top
            m_EdgeBLookup.Add("1011", new double[] { 1 / 16, 0 }); //all but right

            m_EdgeBLookup.Add("1111", new double[] { 0 / 15, 0 }); //all 

            m_AtlasOffPrio.Add(0, new double[] {0, 0});
            m_AtlasOffPrio.Add(1, new double[] {0, 0.5});
            m_AtlasOffPrio.Add(2, new double[] {0, 0.25});
            m_AtlasOffPrio.Add(3, new double[] {0.5, 0.25});
            m_AtlasOffPrio.Add(4, new double[] {0.5, 0});
        }

        private Blend GetBlend(uint[] TerrainTypeData, int i, int j)
        {
            int[] edges;
            int sample;
            int t;

            edges = new int[] { -1, -1, -1, -1 };
            sample = m_ToBlendPrio[new Color() { PackedValue = TerrainTypeData[i * 512 + j] }];
            t = m_ToBlendPrio[new Color() { PackedValue = TerrainTypeData[Positivize((i - 1) * 512 + j)] }];

            if ((i - 1 >= 0) && (t > sample)) edges[0] = t;
            t = m_ToBlendPrio[new Color() { PackedValue = TerrainTypeData[i * 512 + j + 1] }];
            if ((j + 1 < 512) && (t > sample)) edges[1] = t;
            t = m_ToBlendPrio[new Color() { PackedValue = TerrainTypeData[Math.Min((i + 1), 511) * 512 + j] }];
            if ((i + 1 < 512) && (t > sample)) edges[2] = t;
            t = m_ToBlendPrio[new Color() { PackedValue = TerrainTypeData[i * 512 + j - 1] }];
            if ((j - 1 >= 0) && (t > sample)) edges[3] = t;

            var binary = new int[] {
		    (edges[0]>-1) ? 1:0,
		    (edges[1]>-1) ? 1:0,
		    (edges[2]>-1) ? 1:0,
		    (edges[3]>-1) ? 1:0};
            //Construct a string of the format "1011" to look up an edge.
            double[] atlasPos = m_EdgeBLookup[ToBinaryString(binary)];

            int maxEdge = 4;

            for (int x = 0; x < 4; x++)
                if (edges[x] < maxEdge && edges[x] != -1) maxEdge = edges[x];

            atlasPos[1] = m_Prio2Map[maxEdge];

            Blend ReturnBlend = new Blend();
            ReturnBlend.AtlasPosition = atlasPos;
            ReturnBlend.MaxEdge = maxEdge;

            return ReturnBlend;
        }

        /// <summary>
        /// Creates a texture atlas with which to texture the terrain.
        /// </summary>
        /// <param name="spriteBatch">A spritebatch to draw with.</param>
        public void CreateTextureAtlas(SpriteBatch spriteBatch)
        {
            DepthStencilBuffer OldDSBuffer = m_GraphicsDevice.DepthStencilBuffer;

            RenderTarget2D RTarget = new RenderTarget2D(m_GraphicsDevice, 256 * 5, 256 * 5, 0, SurfaceFormat.Color,
                RenderTargetUsage.PreserveContents);
            //For some reason, we have to create a new depth stencil buffer with the same size
            //as the rendertarget in order to set the rendertarget on the GraphicsDevice.
            DepthStencilBuffer DSBuffer = new DepthStencilBuffer(m_GraphicsDevice, 256 * 5, 256 * 5,
                OldDSBuffer.Format);

            m_GraphicsDevice.DepthStencilBuffer = DSBuffer;
            m_GraphicsDevice.SetRenderTarget(0, RTarget);

            m_GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();
            spriteBatch.Draw(m_Ground, new Rectangle(0, 0, m_Ground.Width, m_Ground.Height), Color.White);
            spriteBatch.Draw(Water, new Rectangle(256, 0, Water.Width, Water.Height), Color.White);
            spriteBatch.Draw(Rock, new Rectangle(0, 256, Rock.Width, Rock.Height), Color.White);
            spriteBatch.Draw(Snow, new Rectangle(256, 256, Snow.Width, Snow.Height), Color.White);
            spriteBatch.Draw(Sand, new Rectangle(0, 512, Sand.Width, Sand.Height), Color.White);
            spriteBatch.End();

            m_GraphicsDevice.SetRenderTarget(0, null);
            m_GraphicsDevice.DepthStencilBuffer = OldDSBuffer;

            Atlas = RTarget.GetTexture();
        }

        /// <summary>
        /// Creates a transparency atlas with which to texture the terrain.
        /// </summary>
        /// <param name="spriteBatch">A spritebatch to draw with.</param>
        public void CreateTransparencyAtlas(SpriteBatch spriteBatch)
        {
            DepthStencilBuffer OldDSBuffer = m_GraphicsDevice.DepthStencilBuffer;

            RenderTarget2D RTarget = new RenderTarget2D(m_GraphicsDevice, 1024, 256, 0, SurfaceFormat.Color,
                RenderTargetUsage.PreserveContents);
            //For some reason, we have to create a new depth stencil buffer with the same size
            //as the rendertarget in order to set the rendertarget on the GraphicsDevice.
            DepthStencilBuffer DSBuffer = new DepthStencilBuffer(m_GraphicsDevice, 1024, 256,
                OldDSBuffer.Format);

            m_GraphicsDevice.DepthStencilBuffer = DSBuffer;
            m_GraphicsDevice.SetRenderTarget(0, RTarget);

            m_GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();

            for (int i = 0; i < 30; i = i + 2)
            {
                spriteBatch.Draw(m_TransA[i], new Rectangle(i * 64, 0, m_TransA[i].Width, m_TransA[i].Height), Color.White);
                spriteBatch.Draw(m_TransA[i + 1], new Rectangle((i + 1) * 64, 64, m_TransA[i + 1].Width, m_TransA[i + 1].Height), Color.White);
            }

            for (int i = 0; i < 30; i = i + 2)
            {
                spriteBatch.Draw(TransB[i], new Rectangle(i * 64, 128, m_TransA[i].Width, m_TransA[i].Height), Color.White);
                spriteBatch.Draw(TransB[i + 1], new Rectangle((i + 1) * 64, 192, m_TransA[i + 1].Width, m_TransA[i + 1].Height), Color.White);
            }

            spriteBatch.End();

            m_GraphicsDevice.SetRenderTarget(0, null);
            m_GraphicsDevice.DepthStencilBuffer = OldDSBuffer;

            TransAtlas = RTarget.GetTexture();
        }

        private string ToBinaryString(int[] Array)
        {
            StringBuilder StrBuilder = new StringBuilder();

            for (int i = 0; i < Array.Length; i++)
                StrBuilder.Append(Array[i].ToString());

            return StrBuilder.ToString();
        }

        /// <summary>
        /// Takes a number that might be negative, and returns the positive.
        /// </summary>
        /// <param name="PossibleNegative">Number that might be negative.</param>
        /// <returns>A positive, or the number passed in if it was positive.</returns>
        private int Positivize(int PossibleNegative)
        {
            if (PossibleNegative < 0)
                return PossibleNegative * -1;
            else
                return PossibleNegative;
        }

        private double[] GetAtlasOffset(uint Clr)
        {
            switch (Clr)
            {
                case 0xFF00FF00:
                    return new double[] { 0, 0 };
                case 0xFFFF000C:
                    return new double[] { 0.5, 0 };
                case 0xFF0000FF:
                    return new double[] { 0, 0.25 };
                case 0xFFFFFFFF:
                    return new double[] { 0.5, 0.25 };
                case 0xFF00FFFF:
                    return new double[] { 0, 0.5 };
                default:
                    return new double[] { 0, 0 };
            }
        }

        public void GenerateCityMesh(GraphicsDevice GfxDevice)
        {
            int xStart, xEnd;

            MeshVertex[] Verts = new MeshVertex[m_Width * m_Height];

            Color[] ColorData = new Color[m_Width * m_Height];
            Color[] TerrainTypeColorData = new Color[m_TerrainType.Width * m_TerrainType.Height];
            Color[] ForestDensityData = new Color[m_ForestDensity.Width * m_ForestDensity.Height];
            Color[] ForestTypeData = new Color[m_ForestType.Width * m_ForestType.Height];

            m_Elevation.GetData(ColorData);
            m_TerrainType.GetData(TerrainTypeColorData);
            m_ForestDensity.GetData(ForestDensityData);
            m_ForestType.GetData(ForestTypeData);

            byte[] Data = ConvertToBinaryArray(ColorData);
            uint[] TerrainTypeData = ConvertToPackedArray(TerrainTypeColorData);

            for (int i = 0; i < 512; i++)
            {
                if (i < 306)
                    xStart = 306 - i;
                else
                    xStart = i - 306;
                if (i < 205)
                    xEnd = 307 + i;
                else
                    xEnd = 512 - (i - 205);
                for (var j = xStart; j < xEnd; j++)
                { //where the magic happens
                    var blendData = GetBlend(TerrainTypeData, i, j);

                    var bOff = blendData.AtlasPosition;
                    var off = GetAtlasOffset(TerrainTypeData[i * 512 + j]);
                    off[0] += 0.125 * (j % 4);
                    off[1] += (0.125 / 2) * (i % 4); //vertically 2 times as large
                    var off2 = m_AtlasOffPrio[blendData.AtlasPosition[0]];
                    off2[0] += 0.125 * (j % 4);
                    off2[1] += (0.125 / 2) * (i % 4);

                    var toX = 0; //vertex colour offset
				    var toY = 0;

                    Verts[i].Coord.X = j;
                    Verts[i].Coord.Y = Data[(i * 512 + j) * 4] / 12; //elevation
                    Verts[i].Coord.Z = i;
                    Verts[i].TextureCoord.X = (j + toX) / 512;
                    Verts[i].TextureCoord.Y = (i + toY) / 512;
                    Verts[i].Texture2Coord.X = (float)off[0];
                    Verts[i].Texture2Coord.Y = (float)off[1];
                    Verts[i].Texture3Coord.X = (float)off2[0];
                    Verts[i].Texture3Coord.Y = (float)off2[1];
                    Verts[i].UVBCoord.X = (float)bOff[0];
                    Verts[i].UVBCoord.Y = (float)bOff[1];

                    Verts[i + 1].Coord.X = j + 1;
                    Verts[i + 1].Coord.Y = Data[(i * 512 + Math.Min(511, j + 1)) * 4] / 12; //elevation
                    Verts[i + 1].Coord.Z = i;
                    Verts[i + 1].TextureCoord.X = (j + toX + 1) / 512;
                    Verts[i + 1].TextureCoord.Y = (i + toY) / 512;
                    Verts[i + 1].Texture2Coord.X = (float)(off[0] + 0.125);
                    Verts[i + 1].Texture2Coord.Y = (float)(off[1]);
                    Verts[i + 1].Texture3Coord.X = (float)(off2[0] + 0.125);
                    Verts[i + 1].Texture3Coord.Y = (float)off2[1];
                    Verts[i + 1].UVBCoord.X = (float)(bOff[0] + 0.0625);
                    Verts[i + 1].UVBCoord.Y = (float)bOff[1];

                    Verts[i + 2].Coord.X = j + 1;
                    Verts[i + 2].Coord.Y = Data[(Math.Min(511, i + 1) * 512 + Math.Min(511, j + 1)) * 4] / 12; //elevation
                    Verts[i + 2].Coord.Z = i + 1;
                    Verts[i + 2].TextureCoord.X = (j + toX + 1) / 512;
                    Verts[i + 2].TextureCoord.Y = (i + toY + 1) / 512;
                    Verts[i + 2].Texture2Coord.X = (float)(off[0] + 0.125);
                    Verts[i + 2].Texture2Coord.Y = (float)(off[1] + 0.125 / 2);
                    Verts[i + 2].Texture3Coord.X = (float)(off2[0] + 0.125);
                    Verts[i + 2].Texture3Coord.Y = (float)(off2[1] + 0.125 / 2);
                    Verts[i + 2].UVBCoord.X = (float)(bOff[0] + 0.0625);
                    Verts[i + 2].UVBCoord.Y = (float)(bOff[1] + 0.25);

                    //tri 2

                    Verts[i + 3].Coord.X = j;
                    Verts[i + 3].Coord.Y = Data[(i * 512 + j) * 4] / 12; //elevation
                    Verts[i + 3].Coord.Z = i;
                    Verts[i + 3].TextureCoord.X = (j + toX) / 512;
                    Verts[i + 3].TextureCoord.Y = (i + toY) / 512;
                    Verts[i + 3].Texture2Coord.X = (float)(off[0]);
                    Verts[i + 3].Texture2Coord.Y = (float)(off[1]);
                    Verts[i + 3].Texture3Coord.X = (float)off2[0];
                    Verts[i + 3].Texture3Coord.Y = (float)off2[1];
                    Verts[i + 3].UVBCoord.X = (float)bOff[0];
                    Verts[i + 3].UVBCoord.Y = (float)bOff[1];

                    Verts[i + 4].Coord.X = j + 1;
                    Verts[i + 4].Coord.Y = Data[(Math.Min(511, i + 1) * 512 + Math.Min(511, j + 1)) * 4] / 12; //elevation
                    Verts[i + 4].Coord.Z = i + 1;
                    Verts[i + 4].TextureCoord.X = (j + toX + 1) / 512;
                    Verts[i + 4].TextureCoord.Y = (i + toY + 1) / 512;
                    Verts[i + 4].Texture2Coord.X = (float)(off[0] + 0.125);
                    Verts[i + 4].Texture2Coord.Y = (float)(off[1] + 0.125 / 2);
                    Verts[i + 4].Texture3Coord.X = (float)(off2[0] + 0.125);
                    Verts[i + 4].Texture3Coord.Y = (float)(off2[1] + 0.125 / 2);
                    Verts[i + 4].UVBCoord.X = (float)(bOff[0] + 0.0625);
                    Verts[i + 4].UVBCoord.Y = (float)(bOff[1] + 0.25);

                    Verts[i + 5].Coord.X = j;
                    Verts[i + 5].Coord.Y = Data[(Math.Min(511, i + 1) * 512 + j) * 4] / 12; //elevation
                    Verts[i + 5].Coord.Z = i + 1;
                    Verts[i + 5].TextureCoord.X = (j + toX) / 512;
                    Verts[i + 5].TextureCoord.Y = (i + toY + 1) / 512;
                    Verts[i + 5].Texture2Coord.X = (float)(off[0]);
                    Verts[i + 5].Texture2Coord.Y = (float)(off[1] + 0.125 / 2);
                    Verts[i + 5].Texture3Coord.X = (float)off2[0];
                    Verts[i + 5].Texture3Coord.Y = (float)(off2[1] + 0.125 / 2);
                    Verts[i + 5].UVBCoord.X = (float)bOff[0];
                    Verts[i + 5].UVBCoord.Y = (float)(bOff[1] + 0.25);
                }
            }
        }

        private byte[] ConvertToBinaryArray(Color[] ColorArray)
        {
            byte[] BinArray = new byte[ColorArray.Length * 4];

            for(int i = 0; i < ColorArray.Length; i++)
            {
                BinArray[i] = ColorArray[i].R;
                BinArray[i + 1] = ColorArray[i].G;
                BinArray[i + 2] = ColorArray[i].B;
                BinArray[i + 3] = ColorArray[i].A;
            }

            return BinArray;
        }

        private uint[] ConvertToPackedArray(Color[] ColorArray)
        {
            uint[] PackedArray = new uint[ColorArray.Length];

            for (int i = 0; i < ColorArray.Length; i++)
            {
                PackedArray[i] = ColorArray[i].PackedValue;
            }

            return PackedArray;
        }

        private bool IsInsidePoly(double[] Poly, double[] Pos)
        {
            if (Poly.Length % 2 != 0) return false; //invalid polygon
		    var n = Poly.Length / 2;
		    var result = false;
		    
            for (var i=0; i<n; i++)
            {
			    var x1 = Poly[i*2];
			    var y1 = Poly[i*2+1];
			    var x2 = Poly[((i+1)*2)%Poly.Length];
			    var y2 = Poly[((i+1)*2+1)%Poly.Length];
			    var slope = (y2-y1)/(x2-x1);
			    var c = y1-(slope*x1);
                if ((Pos[1] < (slope * Pos[0]) + c) && (Pos[0] >= Math.Min(x1, x2)) && (Pos[0] < Math.Max(x1, x2))) 
                    result = !(result);
		    }

		    return result;
        }
    }
}
