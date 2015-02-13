/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSO CityServer.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/


//DON'T FUCKING CHANGE THIS - some fucking rocket scientist decided to redefine System.IO.FileMode with the EXACT
//SAME NAMESPACE inside the Monogame lib...
extern alias MonoGame;

using System;
using System.Collections.Generic;
using System.Text;
using Hack = global::System.IO;
using System.Threading.Tasks;
using MG = MonoGame::Microsoft.Xna.Framework;
using MGfx = MonoGame::Microsoft.Xna.Framework.Graphics;

namespace TSO_CityServer.Terrain
{
	public class Terrain
	{
		public int m_CityNumber;
		public int m_Width, m_Height;

		private MGfx.GraphicsDevice m_GraphicsDevice;

		private MGfx.Texture2D m_Elevation, m_VertexColor, m_TerrainType, m_ForestType, m_ForestDensity, m_RoadMap;
		public MGfx.Texture2D Atlas, TransAtlas, RoadAtlas, RoadCAtlas;

		private Dictionary<MG.Color, int> m_ToBlendPrio = new Dictionary<MG.Color, int>();
		private Dictionary<MG.Color, double[]> m_AtlasOff = new Dictionary<MG.Color, double[]>();
		private Dictionary<int, double> m_Prio2Map = new Dictionary<int, double>();
		private Dictionary<string, double[]> m_EdgeBLookup = new Dictionary<string, double[]>();
		private double[][] m_AtlasOffPrio = new double[5][];

		private MeshVertex[] m_Verts;

		private MG.Color[] m_TerrainTypeColorData;
		private byte[] m_ElevationData, m_ForestDensityData;

		private MG.Color[] m_ForestTypeData;

		private MGfx.VertexBuffer vertBuf;
		private int m_MeshTris;

		private MGfx.Texture2D LoadTex(string Path)
        {
			return LoadTex(new Hack.FileStream(Path, Hack.FileMode.Open));
        }

		private MGfx.Texture2D LoadTex(Hack.Stream stream)
        {
			MGfx.Texture2D result = null;
            try
            {
                result = ImageLoader.FromStream(m_GraphicsDevice, stream);
            }
            catch (Exception)
            {
				result = new MGfx.Texture2D(m_GraphicsDevice, 1, 1);
            }
            stream.Close();
            return result;
        }

		/// <summary>
		/// This gets the number of a city when provided with a name.
		/// </summary>
		/// <param name="CityName">Name of the city.</param>
		/// <returns>Number of the city.</returns>
		private int GetCityNumber(string CityName)
		{
			switch (CityName)
			{
				case "Blazing Falls":
					return 1;
				case "Alphaville":
					return 2;
				case "Test Center":
					return 3;
				case "Interhogan":
					return 4;
				case "Ocean's Edge":
					return 5;
				case "East Jerome":
					return 6;
				case "Fancy Fields":
					return 7;
				case "Betaville":
					return 8;
				case "Charvatia":
					return 9;
				case "Dragon's Cove":
					return 10;
				case "Rancho Rizzo":
					return 11;
				case "Zavadaville":
					return 12;
				case "Queen Margaret's":
					return 13;
				case "Shannopolis":
					return 14;
				case "Grantley Grove":
					return 15;
				case "Calvin's Creek":
					return 16;
				case "The Billabong":
					return 17;
				case "Mount Fuji":
					return 18;
				case "Dan's Grove":
					return 19;
				case "Jolly Pines":
					return 20;
				case "Yatesport":
					return 21;
				case "Landry Lakes":
					return 22;
				case "Nichol's Notch":
					return 23;
				case "King Canyons":
					return 24;
				case "Virginia Islands":
					return 25;
				case "Pixie Point":
					return 26;
				case "West Darrington":
					return 27;
				case "Upper Shankelston":
					return 28;
				case "Albertstown":
					return 29;
				case "Terra Tablante":
					return 30;
			}

			return 1;
		}

		public void LoadContent(MGfx.GraphicsDevice GfxDevice)
        {
            m_GraphicsDevice = GfxDevice;
            /*VertexShader = GameFacade.Game.Content.Load<Effect>("Effects\\VerShader");
            PixelShader = GameFacade.Game.Content.Load<Effect>("Effects\\PixShader");
            Shader2D = GameFacade.Game.Content.Load<Effect>("Effects\\colorpoly2d");*/

            string CityStr = "cities\\" + ((m_CityNumber >= 10) ? "city_00" + m_CityNumber.ToString() : "city_000" + m_CityNumber.ToString());
            m_Elevation = LoadTex(CityStr + "\\elevation.bmp");
            m_VertexColor = LoadTex(CityStr + "\\vertexcolor.bmp");
            m_TerrainType = LoadTex(CityStr + "\\terraintype.bmp");
            m_ForestType = LoadTex(CityStr + "\\foresttype.bmp");
            m_ForestDensity = LoadTex(CityStr + "\\forestdensity.bmp");
            m_RoadMap = LoadTex(CityStr + "\\roadmap.bmp");

            /*m_Ground = LoadTex("gamedata\\terrain\\newformat\\gr.tga");
            m_Rock = LoadTex("gamedata\\terrain\\newformat\\rk.tga");
            m_Water = LoadTex("gamedata\\terrain\\newformat\\wt.tga");
            m_Sand = LoadTex("gamedata\\terrain\\newformat\\sd.tga");
            m_Snow = LoadTex("gamedata\\terrain\\newformat\\sn.tga");
            m_Forest = LoadTex("gamedata\\farzoom\\forest00a.tga");
            m_DefaultHouse = LoadTex( "userdata\\houses\\defaulthouse.bmp");//, new TextureCreationParameters(128, 64, 24, 0, SurfaceFormat.Rgba32, TextureUsage.Linear, Color.Black, FilterOptions.None, FilterOptions.None));
            TextureUtils.ManualTextureMaskSingleThreaded(ref m_DefaultHouse, new uint[] { new Color(0x00, 0x00, 0x00, 0xFF).PackedValue });*/

            /*byte[] bytes = ContentManager.GetResourceFromLongID(0x0000032F00000001);
            using (var stream = new MemoryStream(bytes))
            {
                m_LotOnline = LoadTex(stream); //texture creation parameters have been phased out as of xna4! need to manually make magenta transparent
                TextureUtils.ManualTextureMaskSingleThreaded(ref m_LotOnline, MASK_COLORS);
            }

            bytes = ContentManager.GetResourceFromLongID(0x0000033100000001);
            using (var stream = new MemoryStream(bytes))
            {
                m_LotOffline = LoadTex(stream); //, new TextureCreationParameters(4, 3, 24, 0, SurfaceFormat.Rgba32, TextureUsage.Linear, new Color(255, 0, 255, 255), FilterOptions.None, FilterOptions.None));
                TextureUtils.ManualTextureMaskSingleThreaded(ref m_LotOffline, MASK_COLORS);
            }*/

            /*string Num;

            for (int x = 0; x < 30; x = x + 2)
            {
                Num = ZeroPad((x / 2).ToString(), 2);
                m_TransA[x] = LoadTex("gamedata\\terrain\\newformat\\transa" + Num + "a.tga");
                m_TransA[x + 1] = LoadTex("gamedata\\terrain\\newformat\\transa" + Num + "b.tga");
            }

            for (int x = 0; x < 30; x = x + 2)
            {
                Num = ZeroPad((x / 2).ToString(), 2);
                TransB[x] = LoadTex("gamedata\\terrain\\newformat\\transb" + Num + "a.tga");
                TransB[x + 1] = LoadTex("gamedata\\terrain\\newformat\\transb" + Num + "b.tga");
            }

            for (int x = 0; x < 16; x++)
            {
                Num = ZeroPad((x).ToString(), 2);
                m_Roads[x] = LoadTex("gamedata\\terrain\\road" + Num + ".tga");
            }

            for (int x = 0; x < 16; x++)
            {
                Num = ZeroPad((x).ToString(), 2);
                m_RoadCorners[x] = LoadTex("gamedata\\terrain\\roadcorner" + Num + ".tga");
            }*/

            m_Width = m_Elevation.Width;
            m_Height = m_Elevation.Height;
        }

		public void Initialize(String CityName/*, CityDataRetriever cityData*/)
		{
			/*m_CityData = cityData;*/

			m_ToBlendPrio.Add(new MG.Color(0, 255, 0), 0);     //grass
			m_ToBlendPrio.Add(new MG.Color(12, 0, 255), 4);    //water
			m_ToBlendPrio.Add(new MG.Color(255, 255, 255), 3); //snow
			m_ToBlendPrio.Add(new MG.Color(255, 0, 0), 2);     //rock
			m_ToBlendPrio.Add(new MG.Color(255, 255, 0), 1);   //sand
			m_ToBlendPrio.Add(new MG.Color(0, 0, 0), -1);      //nothing, don't blend into this

			m_AtlasOff.Add(new MG.Color(0, 255, 0), new double[] { 0.0, 0.0 });     //grass
			m_AtlasOff.Add(new MG.Color(12, 0, 255), new double[] { 0.5, 0.0 });    //water
			m_AtlasOff.Add(new MG.Color(255, 0, 0), new double[] { 0.0, 0.25 });     //rock
			m_AtlasOff.Add(new MG.Color(255, 255, 255), new double[] { 0.5, 0.25 }); //snow
			m_AtlasOff.Add(new MG.Color(255, 255, 0), new double[] { 0.0, 0.5 });   //sand
			m_AtlasOff.Add(new MG.Color(0, 0, 0), new double[] { 0.0, 0.0 });      //nothing, don't blend into this

			//I rewrote this. Please change back if appropriate/annoying. - Afr0
			m_CityNumber = GetCityNumber(CityName);

			m_Prio2Map.Add(0, 0);
			m_Prio2Map.Add(1, 0.5);
			m_Prio2Map.Add(2, 0.75);
			m_Prio2Map.Add(3, 0.25);
			m_Prio2Map.Add(4, 0);

			m_EdgeBLookup.Add("0000", new double[] { 15.0 / 16.0, 0 }); //none

			m_EdgeBLookup.Add("1000", new double[] { 13.0 / 16.0, 0 }); //top
			m_EdgeBLookup.Add("0100", new double[] { 14.0 / 16.0, 0 }); //right
			m_EdgeBLookup.Add("0010", new double[] { 7.0 / 16.0, 0 }); //bottom
			m_EdgeBLookup.Add("0001", new double[] { 11.0 / 16.0, 0 }); //left

			m_EdgeBLookup.Add("1010", new double[] { 5.0 / 16.0, 0 }); //top + bottom
			m_EdgeBLookup.Add("0101", new double[] { 10.0 / 16.0, 0 }); //left + right

			m_EdgeBLookup.Add("1001", new double[] { 9.0 / 16.0, 0 }); //left + top
			m_EdgeBLookup.Add("1100", new double[] { 12.0 / 16.0, 0 }); //right + top
			m_EdgeBLookup.Add("0110", new double[] { 6.0 / 16.0, 0 }); //right + bottom
			m_EdgeBLookup.Add("0011", new double[] { 3.0 / 16.0, 0 }); //left + bottom

			m_EdgeBLookup.Add("1101", new double[] { 8.0 / 16.0, 0 }); //all but bottom
			m_EdgeBLookup.Add("1110", new double[] { 4.0 / 16.0, 0 }); //all but left
			m_EdgeBLookup.Add("0111", new double[] { 2.0 / 16.0, 0 }); //all but top
			m_EdgeBLookup.Add("1011", new double[] { 1.0 / 16.0, 0 }); //all but right

			m_EdgeBLookup.Add("1111", new double[] { 0.0 / 15.0, 0 }); //all 

			m_AtlasOffPrio[0] = new double[] { 0, 0 };
			m_AtlasOffPrio[1] = new double[] { 0, 0.5 };
			m_AtlasOffPrio[2] = new double[] { 0, 0.25 };
			m_AtlasOffPrio[3] = new double[] { 0.5, 0.25 };
			m_AtlasOffPrio[4] = new double[] { 0.5, 0 };

			/*m_ForestTypes.Add(new Color(0, 0x6A, 0x28), 0);   //heavy forest
			m_ForestTypes.Add(new Color(0, 0xEB, 0x42), 1);   //light forest
			m_ForestTypes.Add(new Color(255, 0, 0), 2);   //cacti
			m_ForestTypes.Add(new Color(255, 0xFC, 0), 3);   //palm
			m_ForestTypes.Add(new Color(0, 0, 0), -1);*/  //nothing; no forest

			//m_HouseGraphics = new Dictionary<int, Texture2D>();
			//populateCityLookup();
		}

		public void GenerateCityMesh(MGfx.GraphicsDevice GfxDevice)
		{
			m_Verts = new MeshVertex[m_Width * m_Height * 3]; //6 verts per pixel, but only half the pixels in the image are used, so multiplier is 3!
			int xStart, xEnd;

			MG.Color[] ColorData = new MG.Color[m_Width * m_Height];
			m_TerrainTypeColorData = new MG.Color[m_TerrainType.Width * m_TerrainType.Height];
			MG.Color[] ForestDensityData = new MG.Color[m_ForestDensity.Width * m_ForestDensity.Height];
			m_ForestTypeData = new MG.Color[m_ForestType.Width * m_ForestType.Height];

			MG.Color[] RoadMapData = new MG.Color[m_RoadMap.Width * m_RoadMap.Height];

			m_Elevation.GetData(ColorData);
			m_TerrainType.GetData(m_TerrainTypeColorData);
			m_ForestDensity.GetData(ForestDensityData);
			m_ForestType.GetData(m_ForestTypeData);
			m_RoadMap.GetData(RoadMapData);

			byte[] RoadData = ConvertToBinaryArray(RoadMapData); //we need binary arrays for these as the values are accessed directly instead of being compared.
			m_ElevationData = ConvertToBinaryArray(ColorData);
			m_ForestDensityData = ConvertToBinaryArray(ForestDensityData);

			int index = 0;

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
				for (int j = xStart; j < xEnd; j++)
				{ //where the magic happens
					var blendData = GetBlend(m_TerrainTypeColorData, i, j); //gets information on what this tile blends into and what blend image to use for the alpha.

					var bOff = blendData.AtlasPosition; //texture used for blend alpha
					double[] temp = m_AtlasOff[m_TerrainTypeColorData[((i * 512) + j)]];
					double[] off = new double[] { temp[0], temp[1] }; //texture for this tile (grass, rock etc)
					off[0] += 0.125 * (j % 4);
					off[1] += (0.125 / 2.0) * (i % 4); //vertically 2 times as large
					double[] temp2 = m_AtlasOffPrio[blendData.MaxEdge];
					double[] off2 = new double[] { temp2[0], temp2[1] }; //texture this tile is blending into (grass, rock etc)
					off2[0] += 0.125 * (j % 4);
					off2[1] += (0.125 / 2.0) * (i % 4);

					float toX = 0; //vertex colour offset, adjust to try and fix vertexcolor offset.
					float toY = 0;

					byte roadByte = RoadData[(i * 512 + j) * 4];
					double[] off3 = new double[] { ((roadByte & 15) % 4) * 0.25, ((int)((roadByte & 15) / 4)) * 0.25 }; //normal road uv selection
					double[] off4 = new double[] { ((roadByte >> 4) % 4) * 0.25, ((int)((roadByte >> 4) / 4)) * 0.25 }; //road corners uv selection

					//huge segment of code for generating triangles incoming

					m_Verts[index].Coord.X = j;
					m_Verts[index].Coord.Y = m_ElevationData[(i * 512 + j) * 4] / 12.0f; //elevation
					m_Verts[index].Coord.Z = i;
					m_Verts[index].TextureCoord.X = (j + toX) / 512.0f;
					m_Verts[index].TextureCoord.Y = (i + toY) / 512.0f;
					m_Verts[index].Texture2Coord.X = (float)off[0];
					m_Verts[index].Texture2Coord.Y = (float)off[1];
					m_Verts[index].Texture3Coord.X = (float)off2[0];
					m_Verts[index].Texture3Coord.Y = (float)off2[1];
					m_Verts[index].UVBCoord.X = (float)bOff[0];
					m_Verts[index].UVBCoord.Y = (float)bOff[1];
					m_Verts[index].RoadCoord.X = (float)(off3[0] + 0.25);
					m_Verts[index].RoadCoord.Y = (float)off3[1];
					m_Verts[index].RoadCCoord.X = (float)(off4[0] + 0.25);
					m_Verts[index].RoadCCoord.Y = (float)off4[1];

					index++;

					m_Verts[index].Coord.X = j + 1;
					m_Verts[index].Coord.Y = m_ElevationData[(i * 512 + Math.Min(511, j + 1)) * 4] / 12.0f; //elevation
					m_Verts[index].Coord.Z = i;
					m_Verts[index].TextureCoord.X = (j + toX + 1) / 512.0f;
					m_Verts[index].TextureCoord.Y = (i + toY) / 512.0f;
					m_Verts[index].Texture2Coord.X = (float)(off[0] + 0.125);
					m_Verts[index].Texture2Coord.Y = (float)(off[1]);
					m_Verts[index].Texture3Coord.X = (float)(off2[0] + 0.125);
					m_Verts[index].Texture3Coord.Y = (float)(off2[1]);
					m_Verts[index].UVBCoord.X = (float)(bOff[0] + 0.0625);
					m_Verts[index].UVBCoord.Y = (float)bOff[1];
					m_Verts[index].RoadCoord.X = (float)off3[0];
					m_Verts[index].RoadCoord.Y = (float)off3[1];
					m_Verts[index].RoadCCoord.X = (float)off4[0];
					m_Verts[index].RoadCCoord.Y = (float)off4[1];

					index++;

					m_Verts[index].Coord.X = j + 1;
					m_Verts[index].Coord.Y = m_ElevationData[(Math.Min(511, i + 1) * 512 + Math.Min(511, j + 1)) * 4] / 12.0f; //elevation
					m_Verts[index].Coord.Z = i + 1;
					m_Verts[index].TextureCoord.X = (j + toX + 1) / 512.0f;
					m_Verts[index].TextureCoord.Y = (i + toY + 1) / 512.0f;
					m_Verts[index].Texture2Coord.X = (float)(off[0] + 0.125);
					m_Verts[index].Texture2Coord.Y = (float)(off[1] + 0.125 / 2.0);
					m_Verts[index].Texture3Coord.X = (float)(off2[0] + 0.125);
					m_Verts[index].Texture3Coord.Y = (float)(off2[1] + 0.125 / 2.0);
					m_Verts[index].UVBCoord.X = (float)(bOff[0] + 0.0625);
					m_Verts[index].UVBCoord.Y = (float)(bOff[1] + 0.25);
					m_Verts[index].RoadCoord.X = (float)(off3[0]);
					m_Verts[index].RoadCoord.Y = (float)(off3[1] + 0.25);
					m_Verts[index].RoadCCoord.X = (float)(off4[0]);
					m_Verts[index].RoadCCoord.Y = (float)(off4[1] + 0.25);

					index++;

					//tri 2

					m_Verts[index].Coord.X = j;
					m_Verts[index].Coord.Y = m_ElevationData[(i * 512 + j) * 4] / 12.0f; //elevation
					m_Verts[index].Coord.Z = i;
					m_Verts[index].TextureCoord.X = (j + toX) / 512.0f;
					m_Verts[index].TextureCoord.Y = (i + toY) / 512.0f;
					m_Verts[index].Texture2Coord.X = (float)(off[0]);
					m_Verts[index].Texture2Coord.Y = (float)(off[1]);
					m_Verts[index].Texture3Coord.X = (float)off2[0];
					m_Verts[index].Texture3Coord.Y = (float)off2[1];
					m_Verts[index].UVBCoord.X = (float)bOff[0];
					m_Verts[index].UVBCoord.Y = (float)bOff[1];
					m_Verts[index].RoadCoord.X = (float)(off3[0] + 0.25);
					m_Verts[index].RoadCoord.Y = (float)off3[1];
					m_Verts[index].RoadCCoord.X = (float)(off4[0] + 0.25);
					m_Verts[index].RoadCCoord.Y = (float)off4[1];

					index++;

					m_Verts[index].Coord.X = j + 1;
					m_Verts[index].Coord.Y = m_ElevationData[(Math.Min(511, i + 1) * 512 + Math.Min(511, j + 1)) * 4] / 12.0f; //elevation
					m_Verts[index].Coord.Z = i + 1;
					m_Verts[index].TextureCoord.X = (j + toX + 1) / 512.0f;
					m_Verts[index].TextureCoord.Y = (i + toY + 1) / 512.0f;
					m_Verts[index].Texture2Coord.X = (float)(off[0] + 0.125);
					m_Verts[index].Texture2Coord.Y = (float)(off[1] + 0.125 / 2.0);
					m_Verts[index].Texture3Coord.X = (float)(off2[0] + 0.125);
					m_Verts[index].Texture3Coord.Y = (float)(off2[1] + 0.125 / 2.0);
					m_Verts[index].UVBCoord.X = (float)(bOff[0] + 0.0625);
					m_Verts[index].UVBCoord.Y = (float)(bOff[1] + 0.25);
					m_Verts[index].RoadCoord.X = (float)(off3[0]);
					m_Verts[index].RoadCoord.Y = (float)(off3[1] + 0.25);
					m_Verts[index].RoadCCoord.X = (float)(off4[0]);
					m_Verts[index].RoadCCoord.Y = (float)(off4[1] + 0.25);

					index++;

					m_Verts[index].Coord.X = j;
					m_Verts[index].Coord.Y = m_ElevationData[(Math.Min(511, i + 1) * 512 + j) * 4] / 12.0f; //elevation
					m_Verts[index].Coord.Z = i + 1;
					m_Verts[index].TextureCoord.X = (j + toX) / 512.0f;
					m_Verts[index].TextureCoord.Y = (i + toY + 1) / 512.0f;
					m_Verts[index].Texture2Coord.X = (float)(off[0]);
					m_Verts[index].Texture2Coord.Y = (float)(off[1] + 0.125 / 2.0);
					m_Verts[index].Texture3Coord.X = (float)off2[0];
					m_Verts[index].Texture3Coord.Y = (float)(off2[1] + 0.125 / 2.0);
					m_Verts[index].UVBCoord.X = (float)bOff[0];
					m_Verts[index].UVBCoord.Y = (float)(bOff[1] + 0.25);
					m_Verts[index].RoadCoord.X = (float)(off3[0] + 0.25);
					m_Verts[index].RoadCoord.Y = (float)(off3[1] + 0.25);
					m_Verts[index].RoadCCoord.X = (float)(off4[0] + 0.25);
					m_Verts[index].RoadCCoord.Y = (float)(off4[1] + 0.25);

					index++;
				}
			}

			vertBuf = new MGfx.VertexBuffer(m_GraphicsDevice, typeof(MeshVertex), m_Verts.Length, MGfx.BufferUsage.WriteOnly);
			vertBuf.SetData(m_Verts); //use vertex buffer to draw mesh as the data is always the same. we only have to set data once.
			m_MeshTris = m_Verts.Length / 3;
			m_Verts = null; //clear m_Verts now that it's copied to save some RAM.
		}

		/// <summary>
		/// Gets information on what this tile blends into and what blend image to use for the alpha.
		/// </summary>
		/// <param name="TerrainTypeData">Terrain type data.</param>
		/// <param name="i"></param>
		/// <param name="j"></param>
		/// <returns>Blend instance.</returns>
		private Blend GetBlend(MG.Color[] TerrainTypeData, int i, int j)
		{
			int[] edges;
			int sample;
			int t;

			edges = new int[] { -1, -1, -1, -1 };
			sample = m_ToBlendPrio[TerrainTypeData[i * 512 + j]];
			t = m_ToBlendPrio[TerrainTypeData[Math.Abs((i - 1) * 512 + j)]];

			if ((i - 1 >= 0) && (t > sample)) edges[0] = t;
			t = m_ToBlendPrio[TerrainTypeData[i * 512 + j + 1]];
			if ((j + 1 < 512) && (t > sample)) edges[1] = t;
			t = m_ToBlendPrio[TerrainTypeData[Math.Min((i + 1), 511) * 512 + j]];
			if ((i + 1 < 512) && (t > sample)) edges[2] = t;
			t = m_ToBlendPrio[TerrainTypeData[i * 512 + j - 1]];
			if ((j - 1 >= 0) && (t > sample)) edges[3] = t;

			int[] binary = new int[] {
		    (edges[0]>-1) ? 1:0,
		    (edges[1]>-1) ? 1:0,
		    (edges[2]>-1) ? 1:0,
		    (edges[3]>-1) ? 1:0};
			//Construct a string of the format "1011" to look up an edge.
			double[] temp = m_EdgeBLookup[ToBinaryString(binary)];
			double[] atlasPos = new double[] { temp[0], temp[1] };

			int maxEdge = 4;

			for (int x = 0; x < 4; x++)
				if (edges[x] < maxEdge && edges[x] != -1) maxEdge = edges[x];

			atlasPos[1] = m_Prio2Map[maxEdge];

			Blend ReturnBlend = new Blend();
			ReturnBlend.AtlasPosition = atlasPos;
			ReturnBlend.MaxEdge = maxEdge;

			return ReturnBlend;
		}

		private byte[] ConvertToBinaryArray(MG.Color[] ColorArray)
		{
			byte[] BinArray = new byte[ColorArray.Length * 4];

			for (int i = 0; i < ColorArray.Length; i++)
			{
				BinArray[i * 4] = ColorArray[i].R;
				BinArray[i * 4 + 1] = ColorArray[i].G;
				BinArray[i * 4 + 2] = ColorArray[i].B;
				BinArray[i * 4 + 3] = ColorArray[i].A;
			}

			return BinArray;
		}

		private string ToBinaryString(int[] Array)
		{
			StringBuilder StrBuilder = new StringBuilder();

			for (int i = 0; i < Array.Length; i++)
				StrBuilder.Append(Array[i].ToString());

			return StrBuilder.ToString();
		}

		private bool isLandBuildable(int x, int y)
		{
			if (x < 0 || x > 510 || y < 0 || y > 510) return false; //because of +1s, use 510 as bound rather than 511. People won't see those tiles at near view anyways.

			if (m_TerrainTypeColorData[y * 512 + x] == new MG.Color(0x0C, 0, 255)) return false; //if on water, not buildable

			//gets max and min elevation of the 4 verts of this tile, and compares them against a threshold. This threshold should be EXACTLY THE SAME ON THE SERVER! 
			//This is so that the game and the server have the same ideas on what is buildable and what is not.

			int max = Math.Max(m_ElevationData[(y * 512 + x) * 4], Math.Max(m_ElevationData[(y * 512 + x + 1) * 4], Math.Max(m_ElevationData[((y + 1) * 512 + x + 1) * 4], m_ElevationData[((y + 1) * 512 + x) * 4])));
			int min = Math.Min(m_ElevationData[(y * 512 + x) * 4], Math.Min(m_ElevationData[(y * 512 + x + 1) * 4], Math.Min(m_ElevationData[((y + 1) * 512 + x + 1) * 4], m_ElevationData[((y + 1) * 512 + x) * 4])));

			return (max - min < 10); //10 is the threshold for now
		}
	}
}
