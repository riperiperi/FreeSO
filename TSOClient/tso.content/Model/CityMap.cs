using Microsoft.Xna.Framework;
using System.Runtime.InteropServices;

namespace FSO.Content.Model
{
    public class CityMap
    {
        private static Color TERRAIN_GRASS = new Color(0, 255, 0);
        private static Color TERRAIN_WATER = new Color(12, 0, 255);
        private static Color TERRAIN_SNOW = new Color(255, 255, 255);
        private static Color TERRAIN_ROCK = new Color(255, 0, 0);
        private static Color TERRAIN_SAND = new Color(255, 255, 0);

        private static Color FOREST_HEAVY = new Color(0, 0x6A, 0x28);
        private static Color FOREST_LIGHT = new Color(0, 0xEB, 0x42);
        private static Color FOREST_CACTI = new Color(255, 0, 0);
        private static Color FOREST_PALM = new Color(255, 0xFC, 0);

        public int Width => 512;
        public int Height => 512;

        private string _Directory;

        public ITextureRef VertexColour { get; internal set; }
        public ITextureRef Thumbnail { get; internal set; }

        private TextureValueMap<TerrainType> _TerrainType;
        private TextureValueMap<byte> _ElevationMap;
        private TextureValueMap<byte> _RoadMap;

        private TextureValueMap<byte> _ForestDensity;
        private TextureValueMap<ForestType> _ForestType;

        public byte[] ElevationData => _ElevationMap.GetRaw();
        public byte[] ForestDensityData => _ForestDensity.GetRaw();
        public byte[] RoadData => _RoadMap.GetRaw();
        public TerrainType[] TerrainType => _TerrainType.GetRaw();
        public ForestType[] ForestTypeData => _ForestType.GetRaw();


        public Color[] ElevationColorData => _ElevationMap.GetColor();
        public Color[] ForestDensityColorData => _ForestDensity.GetColor();
        public Color[] RoadColorData => _RoadMap.GetColor();
        public Color[] TerrainTypeColorData => _TerrainType.GetColor();
        public Color[] ForestTypeColorData => _ForestType.GetColor();

        private CityMapAspects _Dirty = CityMapAspects.All;

        private static byte Red(Color color)
        {
            return color.R;
        }

        private static Color ToGrayscale(byte value)
        {
            return new Color(value, value, value, (byte)255);
        }

        public CityMap(CityMap other)
        {
            _Directory = other._Directory;
            VertexColour = other.VertexColour;

            _TerrainType = new(other._TerrainType);
            _ElevationMap = new(other._ElevationMap);
            _RoadMap = new(other._RoadMap);

            _ForestDensity = new(other._ForestDensity);
            _ForestType = new(other._ForestType);
        }
        
        public CityMap(CityMapMarshal marshal)
        {
            _TerrainType = new TextureValueMap<TerrainType>([.. MemoryMarshal.Cast<byte, TerrainType>(marshal.TerrainType)], TerrainTypeToColor);
            _ElevationMap = new TextureValueMap<byte>(marshal.ElevationMap, ToGrayscale);
            _RoadMap = new TextureValueMap<byte>(marshal.RoadMap, ToGrayscale);

            _ForestDensity = new TextureValueMap<byte> (marshal.ForestDensity, ToGrayscale);
            _ForestType = new TextureValueMap<ForestType>([.. MemoryMarshal.Cast<byte, ForestType>(marshal.TerrainType)], ForestTypeToColor);
        }

        private static Color TerrainTypeToColor(TerrainType type)
        {
            return type switch
            {
                Model.TerrainType.GRASS => TERRAIN_GRASS,
                Model.TerrainType.WATER => TERRAIN_WATER,
                Model.TerrainType.SNOW => TERRAIN_SNOW,
                Model.TerrainType.ROCK => TERRAIN_ROCK,
                Model.TerrainType.SAND => TERRAIN_SAND,
                _ => Color.Black
            };
        }
        private static Color ForestTypeToColor(ForestType type)
        {
            return type switch
            {
                Model.ForestType.HEAVY => FOREST_HEAVY,
                Model.ForestType.LIGHT => FOREST_LIGHT,
                Model.ForestType.CACTI => FOREST_CACTI,
                Model.ForestType.PALM => FOREST_PALM,
                _ => Color.Black
            };
        }

        public CityMap(string directory)
        {
            _Directory = directory;
            string ext = "bmp";
            if (!File.Exists(Path.Combine(directory, "elevation.bmp")))
            {
                ext = "png"; //fso maps use png
            }

            VertexColour = new FileTextureRef(Path.Combine(directory, "vertexcolor." + ext));
            Thumbnail = new FileTextureRef(Path.Combine(directory, "thumbnail." + ext));

            var Elevation = new FileTextureRef(Path.Combine(directory, "elevation." + ext));
            var ForestDensity = new FileTextureRef(Path.Combine(directory, "forestdensity." + ext));
            var ForestType = new FileTextureRef(Path.Combine(directory, "foresttype." + ext));
            var RoadMap = new FileTextureRef(Path.Combine(directory, "roadmap." + ext));
            var TerrainTypeTex = new FileTextureRef(Path.Combine(directory, "terraintype." + ext));

            // Load from the files

            _TerrainType = new TextureValueMap<Model.TerrainType>(TerrainTypeTex, x =>
            {
                if (x == TERRAIN_GRASS)
                {
                    return Model.TerrainType.GRASS;
                }
                else if (x == TERRAIN_WATER)
                {
                    return Model.TerrainType.WATER;
                }
                else if (x == TERRAIN_SNOW)
                {
                    return Model.TerrainType.SNOW;
                }
                else if (x == TERRAIN_ROCK)
                {
                    return Model.TerrainType.ROCK;
                }
                else if (x == TERRAIN_SAND)
                {
                    return Model.TerrainType.SAND;
                }

                return Model.TerrainType.NULL;
            }, TerrainTypeToColor);

            _ElevationMap = new TextureValueMap<byte>(Elevation, Red, ToGrayscale);
            _RoadMap = new TextureValueMap<byte>(RoadMap, Red, ToGrayscale);

            _ForestType = new TextureValueMap<ForestType>(ForestType, x =>
            {
                if (x == FOREST_HEAVY)
                {
                    return Model.ForestType.HEAVY;
                }
                else if (x == FOREST_LIGHT)
                {
                    return Model.ForestType.LIGHT;
                }
                else if (x == FOREST_CACTI)
                {
                    return Model.ForestType.CACTI;
                }
                else if (x == FOREST_PALM)
                {
                    return Model.ForestType.PALM;
                }
                
                return Model.ForestType.NULL;
            }, ForestTypeToColor);

            _ForestDensity = new TextureValueMap<byte>(ForestDensity, x => x.R, ToGrayscale);
        }

        public CityMapAspects ConsumeDirty()
        {
            var toConsume = _Dirty;
            _Dirty = CityMapAspects.None;

            return toConsume;
        }

        public void SetDirty(CityMapAspects flags)
        {
            _Dirty |= flags;
        }

        public void Set(CityMap other)
        {
            // TODO: limit aspects that are copied?
            _TerrainType = new(other._TerrainType);
            _ElevationMap = new(other._ElevationMap);
            _RoadMap = new(other._RoadMap);

            _ForestDensity = new(other._ForestDensity);
            _ForestType = new(other._ForestType);
        }

        public TerrainType GetTerrain(int x, int y)
        {
            var type = _TerrainType.Get(x, y);

            // Compatibility for server terrain type checks (OOB always counts as grass)
            return type == Model.TerrainType.NULL ? Model.TerrainType.GRASS : type;
        }

        public byte GetRoad(int x, int y)
        {
            return _RoadMap.Get(x, y);
        }

        public byte GetElevation(int x, int y)
        {
            return _ElevationMap.Get(x, y);
        }

        public byte[] GetRawElevation()
        {
            return _ElevationMap.GetRaw();
        }

        public byte[] GetRawRoads()
        {
            return _RoadMap.GetRaw();
        }

        public TerrainType[] GetRawTerrain()
        {
            return _TerrainType.GetRaw();
        }

        public ForestType[] GetRawForestType()
        {
            return _ForestType.GetRaw();
        }

        public byte[] GetRawForestDensity()
        {
            return _ForestDensity.GetRaw();
        }

        public TerrainBlend GetBlend(int x, int y)
        {
            TerrainType sample;
            TerrainType t;

            var edges = new TerrainType[] { Model.TerrainType.NULL, Model.TerrainType.NULL, Model.TerrainType.NULL, Model.TerrainType.NULL,
                Model.TerrainType.NULL, Model.TerrainType.NULL, Model.TerrainType.NULL, Model.TerrainType.NULL};
            sample = GetTerrain(x, y);

            t = GetTerrain(x, y - 1);
            if ((y - 1 >= 0) && (t > sample)) edges[0] = t;

            t = GetTerrain(x + 1, y - 1);
            if ((y - 1 >= 0) && (x + 1 < 512) && (t > sample)) edges[1] = t;

            t = GetTerrain(x + 1, y);
            if ((x + 1 < 512) && (t > sample)) edges[2] = t;

            t = GetTerrain(x + 1, y + 1);
            if ((x + 1 < 512) && (y + 1 < 512) && (t > sample)) edges[3] = t;

            t = GetTerrain(x, y + 1);
            if ((y + 1 < 512) && (t > sample)) edges[4] = t;

            t = GetTerrain(x - 1, y + 1);
            if ((y + 1 < 512) && (x - 1 >= 0) && (t > sample)) edges[5] = t;

            t = GetTerrain(x - 1, y);
            if ((x - 1 >= 0) && (t > sample)) edges[6] = t;

            t = GetTerrain(x - 1, y - 1);
            if ((y - 1 >= 0) && (x - 1 >= 0) && (t > sample)) edges[7] = t;

            int binary = 0;
            for (int i = 0; i < 8; i++)
                binary |= ((edges[i] > Model.TerrainType.NULL) ? (1 << i) : 0);

            int waterbinary = 0;
            for (int i = 0; i < 8; i++)
                waterbinary |= ((edges[i] == Model.TerrainType.WATER) ? (1 << i) : 0);

            TerrainType maxEdge = Model.TerrainType.WATER;

            for (int i = 0; i < 8; i++)
                if (edges[i] < maxEdge && edges[i] != Model.TerrainType.NULL) maxEdge = edges[i];

            TerrainBlend ReturnBlend = new TerrainBlend();
            ReturnBlend.Base = sample;
            ReturnBlend.Blend = maxEdge;
            ReturnBlend.AdjFlags = (byte)binary;
            ReturnBlend.WaterFlags = (byte)waterbinary;

            return ReturnBlend;
        }

        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && y >= 0 && x < Width && y < Height;
        }

        public CityMapMarshal Save()
        {
            return new CityMapMarshal()
            {
                TerrainType = [.. MemoryMarshal.Cast<TerrainType, byte>(_TerrainType.GetRaw())],
                ElevationMap = [.. _ElevationMap.GetRaw()],
                RoadMap = [.. _RoadMap.GetRaw()],

                ForestDensity = [.. _ForestDensity.GetRaw()],
                ForestType = [..MemoryMarshal.Cast<ForestType, byte>(_ForestType.GetRaw())],
            };
        }
    }

    public struct TerrainBlend
    {
        public TerrainType Base;
        public TerrainType Blend;
        public byte AdjFlags;
        public byte WaterFlags;
    }

    public enum TerrainType : sbyte
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

    public enum ForestType : sbyte
    {
        HEAVY = 0,
        LIGHT = 1,
        CACTI = 2,
        PALM = 3,

        SNOW = 4, // special internal type

        NULL = -1
    }

    [Flags]
    public enum CityMapAspects
    {
        None = 0,
        Elevation = 1 << 0,
        TerrainType = 1 << 1,
        Forest = 1 << 2,
        Road = 1 << 3,

        All = Elevation | TerrainType | Forest | Road
    }

    public class TextureValueMap<T>
    {
        private const int Width = 512;
        private const int Height = 512;
        private readonly T[] Values;
        private readonly Func<T, Color> ReverseConverter;

        public TextureValueMap(T[] values, Func<T, Color> reverseConverter)
        {
            Values = values;
            ReverseConverter = reverseConverter;
        }

        public TextureValueMap(ITextureRef texture, Func<Color, T> converter, Func<T, Color> reverseConverter)
        {
            Values = new T[Width * Height];
            ReverseConverter = reverseConverter;

            var image = texture.GetImage();
            var bytes = image.Data;
            var pixelSize = image.PixelSize;

            // copy the bytes from bitmap to array

            var index = 0;

            int i = 0;
            for (var y = 0; y < 512; y++)
            {
                for (var x = 0; x < 512; x++)
                {
                    var a = pixelSize == 3 ? 255 : bytes[index + 3];
                    var r = bytes[index + 2];
                    var g = bytes[index + 1];
                    var b = bytes[index];

                    index += pixelSize;

                    //The game actually uses the pixel coordinates as the lot coordinates
                    var color = new Color(r, g, b, a);
                    var value = converter(color);
                    Values[i++] = value;
                }
            }

            //image.UnlockBits(data);
        }

        public T Get(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
            {
                return default(T);
            }
            return Values[y * Width + x];
        }

        public T[] GetRaw()
        {
            return Values;
        }

        public Color[] GetColor()
        {
            return Values.Select(x => ReverseConverter(x)).ToArray();
        }

        public TextureValueMap(TextureValueMap<T> other)
        {
            Values = other.Values.ToArray();
            ReverseConverter = other.ReverseConverter;
        }
    }
}
