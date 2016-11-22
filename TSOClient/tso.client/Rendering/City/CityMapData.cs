using FSO.Common.Domain.Shards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Rendering.City
{
    public class CityMapData
    {
        public byte[] RoadData;
        public byte[] ElevationData;
        public byte[] ForestDensityData;
        public Color[] ForestTypeData;
        public Color[] TerrainTypeColorData;

        public int Width;
        public int Height;

        public CityMapData(string baseDir, Func<string, Texture2D> texLoader)
        {
            var elevation = texLoader(baseDir + "/elevation.bmp");
            var terrainType = texLoader(baseDir + "/terraintype.bmp");
            var forestType = texLoader(baseDir + "/foresttype.bmp");
            var forestDensity = texLoader(baseDir + "/forestdensity.bmp");
            var roadMap = texLoader(baseDir + "/roadmap.bmp");

            Width = elevation.Width;
            Height = elevation.Height;

            var colorData = new Color[elevation.Width*elevation.Height];
            elevation.GetData(colorData);
            ElevationData = Array.ConvertAll(colorData, (col) => col.R);
            roadMap.GetData(colorData);
            RoadData = Array.ConvertAll(colorData, (col) => col.R);
            forestDensity.GetData(colorData);
            ForestDensityData = Array.ConvertAll(colorData, (col) => col.R);

            ForestTypeData = new Color[forestType.Width * forestType.Height];
            forestType.GetData(ForestTypeData);
            TerrainTypeColorData = new Color[terrainType.Width * terrainType.Height];
            terrainType.GetData(TerrainTypeColorData);

            elevation.Dispose();
            terrainType.Dispose();
            forestType.Dispose();
            forestDensity.Dispose();
            roadMap.Dispose();
        }
    }
}
