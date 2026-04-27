using FSO.Files.Utils;
using System.IO.Compression;

namespace FSO.Content.Model
{
    public class CityMapMarshal
    {
        private const int MapWidth = 512;
        private const int MapHeight = 512;

        public byte[] TerrainType;
        public byte[] ElevationMap;
        public byte[] RoadMap;

        public byte[] ForestDensity;
        public byte[] ForestType;

        public CityMapMarshal()
        {

        }

        public void Write(Stream str)
        {
            using (var compressed = new GZipStream(str, CompressionMode.Compress))
            {
                using (var io = IoWriter.FromStream(str))
                {
                    io.WriteBytes(TerrainType);
                    io.WriteBytes(ElevationMap);
                    io.WriteBytes(RoadMap);
                    io.WriteBytes(ForestDensity);
                    io.WriteBytes(ForestType);
                }

                compressed.Close();
            }
        }

        public byte[] Write()
        {
            using (var mem = new MemoryStream())
            {
                Write(mem);

                return mem.ToArray();
            }
        }

        public void Read(Stream str)
        {
            using (var io = IoBuffer.FromStream(str))
            {
                int pixelCount = MapWidth * MapHeight;
                TerrainType = io.ReadBytes(pixelCount);
                ElevationMap = io.ReadBytes(pixelCount);
                RoadMap = io.ReadBytes(pixelCount);
                ForestDensity = io.ReadBytes(pixelCount);
                ForestType = io.ReadBytes(pixelCount);
            }
        }

        public void Read(byte[] data)
        {
            using (var mem = new MemoryStream(data))
            {
                Read(mem);
            }
        }
    }
}
