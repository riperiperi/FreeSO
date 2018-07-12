using FSO.Common.Domain.Shards;
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
    public class CityMapData
    {
        public byte[] RoadData;
        public byte[] ElevationData;
        public byte[] ForestDensityData;
        public Color[] ForestTypeData;
        public Color[] TerrainTypeColorData;
        public byte[] TerrainType;

        private Dictionary<Color, byte> TerrainTypeMap = new Dictionary<Color, byte>()
        {
            { new Color(0, 255, 0), 0 },     //grass
            {new Color(12, 0, 255), 4 },    //water
            {new Color(255, 255, 255), 3 }, //snow
            {new Color(255, 0, 0), 2 },     //rock
            {new Color(255, 255, 0), 1 },   //sand
            {new Color(0, 0, 0), 255 },      //nothing, don't blend into this
        };

        public int Width;
        public int Height;

        public CityMapData()
        {

        }

        public CityMapData(string baseDir, Func<string, Texture2D> texLoader)
        {
            Load(baseDir, texLoader, "bmp");
        }

        public void Load(string baseDir, Func<string, Texture2D> texLoader, string filetype)
        {
            var elevation = texLoader(Path.Combine(baseDir, "elevation."+filetype));
            var terrainType = texLoader(Path.Combine(baseDir, "terraintype." + filetype));
            var forestType = texLoader(Path.Combine(baseDir, "foresttype." + filetype));
            var forestDensity = texLoader(Path.Combine(baseDir, "forestdensity." + filetype));
            var roadMap = texLoader(Path.Combine(baseDir, "roadmap." + filetype));

            Width = elevation.Width;
            Height = elevation.Height;

            var colorData = new Color[elevation.Width * elevation.Height];
            elevation.GetData(colorData);
            ElevationData = Array.ConvertAll(colorData, (col) => col.R);
            ElevationFlood(ElevationData);
            roadMap.GetData(colorData);
            RoadData = Array.ConvertAll(colorData, (col) => col.R);
            forestDensity.GetData(colorData);
            ForestDensityData = Array.ConvertAll(colorData, (col) => col.R);

            ForestTypeData = new Color[forestType.Width * forestType.Height];
            forestType.GetData(ForestTypeData);
            TerrainTypeColorData = new Color[terrainType.Width * terrainType.Height];
            terrainType.GetData(TerrainTypeColorData);
            TerrainType = Array.ConvertAll(TerrainTypeColorData, x =>
            {
                byte result;
                if (TerrainTypeMap.TryGetValue(x, out result))
                {
                    return result;
                }
                return (byte)255;
            });

            elevation.Dispose();
            terrainType.Dispose();
            forestType.Dispose();
            forestDensity.Dispose();
            roadMap.Dispose();
        }


        private Tuple<int, int> InBounds(int x, int y)
        {
            int xStart, xEnd;
            if (y < 306)
                xStart = 306 - y;
            else
                xStart = y - 306;
            if (y < 205)
                xEnd = 307 + y;
            else
                xEnd = 512 - (y - 205);
            int sD = xStart - x;
            int eD = x - xEnd;
            if (sD > eD)
            {
                return new Tuple<int, int>(1, sD);
            } else
            {
                return new Tuple<int, int>(-1, eD);
            }
        }

        public void ElevationFlood(byte[] data)
        {
            return;
            var result = (byte[])data.Clone();
            for (int y=0; y<512; y++)
            {
                for (int x=0; x<512; x++)
                {
                    var dist = InBounds(x,y);
                    if (dist.Item2 > 0)
                    {
                        int avg = 0;

                        //int destX = 
                    }
                }
            }
        }

        public void Save(string baseDir)
        {
            SaveTex(Path.Combine(baseDir, "roadmap.png"), RoadData.Select(x => new Color(x, x, x, (byte)255)).ToArray());
            SaveTex(Path.Combine(baseDir, "elevation.png"), ElevationData.Select(x => new Color(x, x, x, (byte)255)).ToArray());
            SaveTex(Path.Combine(baseDir, "forestdensity.png"), ForestDensityData.Select(x => new Color(x, x, x, (byte)255)).ToArray());
            SaveTex(Path.Combine(baseDir, "foresttype.png"), ForestTypeData.ToArray());
            SaveTex(Path.Combine(baseDir, "terraintype.png"), TerrainTypeColorData.ToArray());
        }

        public void SaveTex(string filename, Color[] data)
        {
            var tex = new Texture2D(GameFacade.GraphicsDevice, 512, 512);
            tex.SetData(data);
            Directory.CreateDirectory(Path.GetDirectoryName(filename));
            Common.Utils.GameThread.NextUpdate(y =>
            {
                var strm = File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.None);
                tex.SaveAsPng(strm, 512, 512);
                Common.Utils.GameThread.SetTimeout(() => strm.Close(), 500);
                tex.Dispose();
            });
        }
    }
}
