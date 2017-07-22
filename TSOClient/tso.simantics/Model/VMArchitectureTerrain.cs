using FSO.Content.Model;
using FSO.SimAntics.NetPlay.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FSO.SimAntics.Model
{
    public class VMArchitectureTerrain : VMSerializable
    {
        public int Width;
        public int Height;

        public short[] Heights;
        public short[] Centers;
        public bool[] Sloped;
        public byte[] GrassState;

        public short[] VisHeights;
        public byte[] VisGrass;
        private short[] OldHeights;
        private byte[] OldGrass;

        public TerrainType LightType = TerrainType.GRASS;
        public TerrainType DarkType = TerrainType.GRASS;
        public int Version;

        public static float[] TerrainXNoise = GeneratePerlinNoise(77, 1);
        public static float[] TerrainYNoise = GeneratePerlinNoise(77, 2);

        public VMArchitectureTerrain(int width, int height)
        {
            Width = width;
            Height = height;

            Heights = new short[width * height];
            GrassState = new byte[width * height];
            Centers = new short[width * height];
            Sloped = new bool[width * height];

            VisHeights = new short[width * height];
            VisGrass = new byte[width * height];
        }

        public void EnterVis()
        {
            OldHeights = Heights;
            OldGrass = GrassState;

            Array.Copy(Heights, VisHeights, Heights.Length);
            Array.Copy(GrassState, VisGrass, GrassState.Length);

            Heights = VisHeights;
            GrassState = VisGrass;
        }

        public void ExitVis()
        {
            Heights = OldHeights;
            GrassState = OldGrass;
        }

        public void RegenerateCenters() //TODO: partial update
        {
            int i = 0;
            for (int y=0; y<Height; y++)
            {
                for (int x = 0; x < Width; x++) {
                    var h1 = Heights[i];
                    var h2 = Heights[y * Width + ((x + 1) % Width)];
                    var h3 = Heights[((y + 1) % Height) * Width + ((x + 1) % Width)];
                    var h4 = Heights[((y + 1) % Height) * Width + x];
                    
                    Centers[i] = (short)((Heights[i] + h2 + h3 + h4) / 4);
                    Sloped[i] = (h1 != h2) || (h1 != h3) || (h1 != h4);
                    i++;
                }
            }
        }

        public static float[] GeneratePerlinNoise(int size, int seed)
        {
            var random = new Random(seed);
            int width = size;
            float[] result = new float[size*size];
            int initial = width / 4; //divide by more for less noisyness!
            float factor = 0.42f / ((int)Math.Log(initial, 2));

            float min = 1;
            float max = 0;
            int offset;

            while (initial > 0)
            {
                var squared = initial * initial;
                var noise = new float[squared];
                for (int i = 0; i < squared; i++) noise[i] = (float)random.NextDouble() * factor;

                offset = 0;
                for (int x = 0; x < width; x++)
                {
                    double xInt = (x / (double)(width - 1)) * (initial - 1);
                    for (int y = 0; y < width; y++)
                    {
                        double yInt = (y / (double)(width - 1)) * (initial - 1);
                        float tl = noise[(int)(Math.Floor(yInt) * initial + Math.Floor(xInt))];
                        float tr = noise[(int)(Math.Floor(yInt) * initial + Math.Ceiling(xInt))];
                        float bl = noise[(int)(Math.Ceiling(yInt) * initial + Math.Floor(xInt))];
                        float br = noise[(int)(Math.Ceiling(yInt) * initial + Math.Ceiling(xInt))];
                        float p = (float)(xInt % 1.0);
                        float q = (float)(yInt % 1.0);
                        result[offset++] += (tl * (1 - p) + tr * (p)) * (1 - q) + (bl * (1 - p) + br * (p)) * q; //don't you love 2 dimensional linear interpolation?? ;)
                        if (initial == 1)
                        {
                            if (result[offset - 1] < min) min = result[offset - 1];
                            if (result[offset - 1] > max) max = result[offset - 1];
                        }
                    }
                }
                factor *= 1.35f;
                initial /= 2;
            }

            //var off = (min * 3 + max) / 4;
            var mul = 1/(max - min);
            offset = 0;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < width; y++)
                {
                    result[offset] -= min;
                    result[offset++] *= mul;
                    if (result[offset - 1] < 0) result[offset - 1] = 0;

                    //if within 8 of edges, gradiate to 0
                    var dist1 = Math.Abs(x);
                    var dist2 = Math.Abs(x - width);
                    if (dist2 < dist1) dist1 = dist2;
                    dist2 = Math.Abs(y);
                    if (dist2 < dist1) dist1 = dist2;
                    dist2 = Math.Abs(y - size);
                    if (dist2 < dist1) dist1 = dist2;

                    dist1 -= 4;
                    if (dist1 < 0) dist1 = 0;

                    if (dist1 < 8)
                    {
                        result[offset - 1] *= dist1 / 8f;
                        result[offset - 1] += 0.5f * (1-(dist1 / 8f));
                    }
                }
            }

            return result;
        }

        public void GenerateGrassStates() //generates a set of grass states for a lot.
        {
            //right now only works for square lots, but that's all tso has!
            var random = new Random();
            int width = Width;
            float[] result = new float[Width * Height];
            int initial = width / 4; //divide by more for less noisyness!
            float factor = 0.42f / ((int)Math.Log(initial, 2));

            float min = 1;
            float max = 0;
            if (LightType != DarkType) factor /= 2.5f;
            int offset;

            while (initial > 0)
            {
                var squared = initial * initial;
                var noise = new float[squared];
                for (int i = 0; i < squared; i++) noise[i] = (float)random.NextDouble() * factor;

                offset = 0;
                for (int x = 0; x < width; x++)
                {
                    double xInt = (x / (double)(width - 1)) * (initial - 1);
                    for (int y = 0; y < width; y++)
                    {
                        double yInt = (y / (double)(width - 1)) * (initial - 1);
                        float tl = noise[(int)(Math.Floor(yInt) * initial + Math.Floor(xInt))];
                        float tr = noise[(int)(Math.Floor(yInt) * initial + Math.Ceiling(xInt))];
                        float bl = noise[(int)(Math.Ceiling(yInt) * initial + Math.Floor(xInt))];
                        float br = noise[(int)(Math.Ceiling(yInt) * initial + Math.Ceiling(xInt))];
                        float p = (float)(xInt % 1.0);
                        float q = (float)(yInt % 1.0);
                        result[offset++] += (tl * (1 - p) + tr * (p)) * (1 - q) + (bl * (1 - p) + br * (p)) * q; //don't you love 2 dimensional linear interpolation?? ;)
                        if (initial == 1)
                        {
                            if (result[offset - 1] < min) min = result[offset - 1];
                            if (result[offset - 1] > max) max = result[offset - 1];
                        }
                    }
                }
                factor *= 1.25f;
                initial /= 2;
            }

            var off = (min * 3 + max) / 4;
            offset = 0;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < width; y++)
                {
                    result[offset++] -= off;
                    if (result[offset - 1] < 0) result[offset - 1] = 0;

                    //if within 8 of edges, gradiate to 0
                    var dist1 = Math.Abs(x);
                    var dist2 = Math.Abs(x - width);
                    if (dist2 < dist1) dist1 = dist2;
                    dist2 = Math.Abs(y);
                    if (dist2 < dist1) dist1 = dist2;
                    dist2 = Math.Abs(y - Height);
                    if (dist2 < dist1) dist1 = dist2;

                    if (dist1 < 8) result[offset-1] *= dist1 / 8f;
                }
            }

            GrassState = new byte[result.Length];
            for (int i = 0; i < result.Length; i++)
            {
                GrassState[i] = (byte)(result[i] * 255);
            }
        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write((byte)LightType);
            writer.Write((byte)DarkType);
            var ba = VMSerializableUtils.ToByteArray(Heights);
            writer.Write(ba.Length);
            writer.Write(ba);
            writer.Write(GrassState.Length);
            writer.Write(GrassState);
        }

        public void Deserialize(BinaryReader reader)
        {
            LightType = (TerrainType)reader.ReadByte();
            DarkType = (TerrainType)reader.ReadByte();
            var dat = reader.ReadBytes(reader.ReadInt32());
            if (Version > 18)
            {
                Heights = VMSerializableUtils.ToTArray<short>(dat);
            } else
            {
                Heights = Array.ConvertAll(dat, x => (short)x);
            }
            
            GrassState = reader.ReadBytes(reader.ReadInt32());
        }
    }
}
