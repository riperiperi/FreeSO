using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Content.Model
{
    public class CityMap
    {
        private static Color TERRAIN_GRASS = new Color(0, 255, 0);
        private static Color TERRAIN_WATER = new Color(12, 0, 255);
        private static Color TERRAIN_SNOW = new Color(255, 255, 255);
        private static Color TERRAIN_ROCK = new Color(255, 0, 0);
        private static Color TERRAIN_SAND = new Color(255, 255, 0);
        
        private string _Directory;

        public ITextureRef Elevation { get; internal set; }
        public ITextureRef ForestDensity { get; internal set; }
        public ITextureRef ForestType { get; internal set; }
        public ITextureRef RoadMap { get; internal set; }
        public ITextureRef TerrainTypeTex { get; internal set; }
        public ITextureRef VertexColour { get; internal set; }
        public ITextureRef Thumbnail { get; internal set; }

        private TextureValueMap<TerrainType> _TerrainType;
        private TextureValueMap<byte> _ElevationMap;
        private TextureValueMap<byte> _RoadMap;

        public CityMap(string directory)
        {
            _Directory = directory;
            string ext = "bmp";
            if (!File.Exists(Path.Combine(directory, "elevation.bmp")))
            {
                ext = "png"; //fso maps use png
            }
            Elevation = new FileTextureRef(Path.Combine(directory, "elevation."+ext));
            ForestDensity = new FileTextureRef(Path.Combine(directory, "forestdensity." + ext));
            ForestType = new FileTextureRef(Path.Combine(directory, "foresttype." + ext));
            RoadMap = new FileTextureRef(Path.Combine(directory, "roadmap." + ext));
            TerrainTypeTex = new FileTextureRef(Path.Combine(directory, "terraintype." + ext));
            VertexColour = new FileTextureRef(Path.Combine(directory, "vertexcolor." + ext));
            Thumbnail = new FileTextureRef(Path.Combine(directory, "thumbnail." + ext));

            _TerrainType = new TextureValueMap<Model.TerrainType>(TerrainTypeTex, x =>
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
            _RoadMap = new TextureValueMap<byte>(RoadMap, x => x.R);
        }

        public TerrainType GetTerrain(int x, int y)
        {
            return _TerrainType.Get(x, y);
        }

        public byte GetRoad(int x, int y)
        {
            return _RoadMap.Get(x, y);  
        }

        public byte GetElevation(int x, int y)
        {
            return _ElevationMap.Get(x, y);
        }

        public TerrainBlend GetBlend(int x, int y)
        {
            TerrainType sample;
            TerrainType t;

            var edges = new TerrainType[] { TerrainType.NULL, TerrainType.NULL, TerrainType.NULL, TerrainType.NULL,
                TerrainType.NULL, TerrainType.NULL, TerrainType.NULL, TerrainType.NULL};
            sample = GetTerrain(x, y);
            
            t = GetTerrain(x, y-1);
            if ((y - 1 >= 0) && (t > sample)) edges[0] = t;

            t = GetTerrain(x + 1, y-1);
            if ((y - 1 >= 0) && (x + 1 < 512) && (t > sample)) edges[1] = t;

            t = GetTerrain(x+1, y);
            if ((x + 1 < 512) && (t > sample)) edges[2] = t;

            t = GetTerrain(x + 1, y + 1);
            if ((x + 1 < 512) && (y + 1 < 512) && (t > sample)) edges[3] = t;

            t = GetTerrain(x, y + 1);
            if ((y + 1 < 512) && (t > sample)) edges[4] = t;

            t = GetTerrain(x-1, y + 1);
            if ((y + 1 < 512) && (x - 1 >= 0) && (t > sample)) edges[5] = t;

            t = GetTerrain(x-1, y);
            if ((x - 1 >= 0) && (t > sample)) edges[6] = t;

            t =  GetTerrain(x - 1, y - 1);
            if ((y - 1 >= 0) && (x - 1 >= 0) && (t > sample)) edges[7] = t;

            int binary = 0;
            for (int i=0; i<8; i++)
                binary |= ((edges[i] > TerrainType.NULL) ? (1 << i) : 0);

            int waterbinary = 0;
            for (int i = 0; i < 8; i++)
                waterbinary |= ((edges[i] == TerrainType.WATER) ? (1 << i) : 0);

            TerrainType maxEdge = TerrainType.WATER;

            for (int i = 0; i < 8; i++)
                if (edges[i] < maxEdge && edges[i] != TerrainType.NULL) maxEdge = edges[i];

            TerrainBlend ReturnBlend = new TerrainBlend();
            ReturnBlend.Base = sample;
            ReturnBlend.Blend = maxEdge;
            ReturnBlend.AdjFlags = (byte)binary;
            ReturnBlend.WaterFlags = (byte)waterbinary;

            return ReturnBlend;
        }
    }

    public struct TerrainBlend
    {
        public TerrainType Base;
        public TerrainType Blend;
        public byte AdjFlags;
        public byte WaterFlags;
    }

    public enum TerrainType
    {
        WATER = 4,
        ROCK = 2,
        GRASS = 0,
        SNOW = 3,
        SAND = 1,
        NULL = -1,

        TS1DarkGrass = 5,
        TS1AutumnGrass = 6,
        TS1Cloud = 7
    }

    public class TextureValueMap <T>
    {
        private T[,] Values;

        public TextureValueMap(ITextureRef texture, Func<Color, T> converter)
        {
            Values = new T[512, 512];

            var image = texture.GetImage();
            var bytes = image.Data;
            var pixelSize = image.PixelSize;

            // copy the bytes from bitmap to array

            var index = 0;

            for(var y=0; y < 512; y++){
                for(var x=0; x < 512; x++){
                    var a = pixelSize == 3 ? 255 : bytes[index + 3];
                    var r = bytes[index + 2];
                    var g = bytes[index + 1];
                    var b = bytes[index];

                    index += pixelSize;

                    //The game actually uses the pixel coordinates as the lot coordinates
                    var color = new Color(r, g, b, a);
                    var value = converter(color);
                    Values[y, x] = value;
                }
            }
            
            //image.UnlockBits(data);
        }

        public T Get(int x, int y)
        {
            if(x < 0 || y < 0 || x >= 512 || y >= 512){
                return default(T);
            }
            return Values[y, x];
        }
    }
}
