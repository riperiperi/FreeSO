using FSO.Content.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Client.Rendering.City
{
    public static class CityMapExtensions
    {
        public static void Save(this CityMap map, string baseDir)
        {
            SaveTex(Path.Combine(baseDir, "roadmap.png"), map.RoadData.Select(x => new Color(x, x, x, (byte)255)).ToArray());
            SaveTex(Path.Combine(baseDir, "elevation.png"), map.ElevationData.Select(x => new Color(x, x, x, (byte)255)).ToArray());
            SaveTex(Path.Combine(baseDir, "forestdensity.png"), map.ForestDensityData.Select(x => new Color(x, x, x, (byte)255)).ToArray());
            SaveTex(Path.Combine(baseDir, "foresttype.png"), map.ForestTypeColorData.ToArray());
            SaveTex(Path.Combine(baseDir, "terraintype.png"), map.TerrainTypeColorData.ToArray());
        }

        public static void SaveTex(string filename, Color[] data)
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
