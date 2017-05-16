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
