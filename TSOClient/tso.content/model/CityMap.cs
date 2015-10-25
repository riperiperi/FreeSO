using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Content.Model
{
    public class CityMap
    {
        private static Color TERRAIN_GRASS = Color.FromArgb(255, 0, 255, 0);
        private static Color TERRAIN_WATER = Color.FromArgb(255, 12, 0, 255);
        private static Color TERRAIN_SNOW = Color.FromArgb(255, 255, 255, 255);
        private static Color TERRAIN_ROCK = Color.FromArgb(255, 255, 0, 0);
        private static Color TERRAIN_SAND = Color.FromArgb(255, 255, 255, 0);
        
        private string _Directory;

        public ITextureRef Elevation { get; internal set; }
        public ITextureRef ForestDensity { get; internal set; }
        public ITextureRef ForestType { get; internal set; }
        public ITextureRef RoadMap { get; internal set; }
        public ITextureRef TerrainType { get; internal set; }
        public ITextureRef VertexColour { get; internal set; }
        public ITextureRef Thumbnail { get; internal set; }

        private TextureValueMap<TerrainType> _TerrainType;
        private TextureValueMap<byte> _ElevationMap;

        public CityMap(string directory)
        {
            _Directory = directory;
            Elevation = new FileTextureRef(Path.Combine(directory, "elevation.bmp"));
            ForestDensity = new FileTextureRef(Path.Combine(directory, "forestdensity.bmp"));
            ForestType = new FileTextureRef(Path.Combine(directory, "foresttype.bmp"));
            RoadMap = new FileTextureRef(Path.Combine(directory, "roadmap.bmp"));
            TerrainType = new FileTextureRef(Path.Combine(directory, "terraintype.bmp"));
            VertexColour = new FileTextureRef(Path.Combine(directory, "vertexcolor.bmp"));
            Thumbnail = new FileTextureRef(Path.Combine(directory, "thumbnail.bmp"));

            _TerrainType = new TextureValueMap<Model.TerrainType>(TerrainType, x =>
            {
                if(x == TERRAIN_GRASS){
                    return Model.TerrainType.GRASS;
                }else if(x == TERRAIN_WATER)
                {
                    return Model.TerrainType.WATER;
                }else if(x == TERRAIN_SNOW)
                {
                    return Model.TerrainType.SNOW;
                }else if(x == TERRAIN_ROCK)
                {
                    return Model.TerrainType.ROCK;
                }else if(x == TERRAIN_SAND)
                {
                    return Model.TerrainType.SAND;
                }
                return default(TerrainType);
            });

            _ElevationMap = new TextureValueMap<byte>(Elevation, x => x.R);
        }

        public TerrainType GetTerrain(int x, int y)
        {
            return _TerrainType.Get(x, y);
        }

        public byte GetElevation(int x, int y)
        {
            return _ElevationMap.Get(x, y);
        }
    }

    public enum TerrainType
    {
        WATER,
        ROCK,
        GRASS,
        SNOW,
        SAND
    }

    public class TextureValueMap <T>
    {
        private T[,] Values;

        public TextureValueMap(ITextureRef texture, Func<Color, T> converter)
        {
            Values = new T[512, 512];
            
            var image = new Bitmap(texture.GetImage());
            var data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, image.PixelFormat);
            var pixelSize = image.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb ? 4 : 3;
            var padding = data.Stride - (data.Width * pixelSize);
            var bytes = new byte[data.Height * data.Stride];

            // copy the bytes from bitmap to array
            Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);

            var index = 0;

            for(var y=0; y < 512; y++){
                for(var x=0; x < 512; x++){
                    var a = pixelSize == 3 ? 255 : bytes[index + 3];
                    var r = bytes[index + 2];
                    var g = bytes[index + 1];
                    var b = bytes[index];

                    index += pixelSize;

                    //The game actually uses the pixel coordinates as the lot coordinates
                    var color = Color.FromArgb(a, r, g, b);
                    var value = converter(color);
                    Values[y, x] = value;
                }
            }
            
            image.UnlockBits(data);
        }

        public T Get(int x, int y)
        {
            return Values[y, x];
        }
    }
}
