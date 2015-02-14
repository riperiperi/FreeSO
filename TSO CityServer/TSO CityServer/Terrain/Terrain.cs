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

		private MG.Color[] m_TerrainTypeColorData;
		private byte[] m_ElevationData, m_ForestDensityData;

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

		public void Initialize(String CityName/*, CityDataRetriever cityData*/)
		{
			m_CityNumber = GetCityNumber(CityName);
		}

		public void LoadContent(MGfx.GraphicsDevice GfxDevice)
        {
            m_GraphicsDevice = GfxDevice;

            string CityStr = "cities\\" + ((m_CityNumber >= 10) ? "city_00" + m_CityNumber.ToString() : "city_000" + m_CityNumber.ToString());
            m_Elevation = LoadTex(CityStr + "\\elevation.bmp");

            m_Width = m_Elevation.Width;
            m_Height = m_Elevation.Height;
        }

		public void GenerateCityMesh()
		{
			MG.Color[] ColorData = new MG.Color[m_Width * m_Height];
			m_TerrainTypeColorData = new MG.Color[m_TerrainType.Width * m_TerrainType.Height];

			m_ElevationData = ConvertToBinaryArray(ColorData);
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

		public bool IsLandBuildable(int x, int y)
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
