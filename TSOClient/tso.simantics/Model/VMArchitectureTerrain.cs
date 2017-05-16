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

        public sbyte[] Heights;
        public byte[] GrassState;

        public TerrainType LightType = TerrainType.GRASS;
        public TerrainType DarkType = TerrainType.GRASS;

        public VMArchitectureTerrain(int width, int height)
        {
            Width = width;
            Height = height;

            Heights = new sbyte[width * height];
            GrassState = new byte[width * height];
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
            writer.Write(Heights.Length);
            writer.Write(VMSerializableUtils.ToByteArray(Heights));
            writer.Write(GrassState.Length);
            writer.Write(GrassState);
        }

        public void Deserialize(BinaryReader reader)
        {
            LightType = (TerrainType)reader.ReadByte();
            DarkType = (TerrainType)reader.ReadByte();
            Heights = (sbyte[])((Array)reader.ReadBytes(reader.ReadInt32()));
            GrassState = reader.ReadBytes(reader.ReadInt32());
        }
    }
}
