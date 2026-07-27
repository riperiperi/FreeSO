using FSO.Common.Serialization;
using Mina.Core.Buffer;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FSO.Server.Protocol.Electron.Model
{
    public class CityData : ICompressedContainerItem
    {
        public const int Width = 512;
        public const int Height = 512;

        public byte[] Elevation;
        public byte[] ForestDensity;
        public uint[] ForestType;
        public byte[] RoadMap;
        public uint[] TerrainType;

        public void Deserialize(IoBuffer input, ISerializationContext context)
        {
            int pixelCount = Width * Height;

            Elevation = input.GetSlice(pixelCount).GetBytes();
            ForestDensity = input.GetSlice(pixelCount).GetBytes();
            ForestType = GetArray<uint>(input, pixelCount);
            RoadMap = input.GetSlice(pixelCount).GetBytes();
            TerrainType = GetArray<uint>(input, pixelCount);
        }

        private static T[] GetArray<T>(IoBuffer input, int size) where T : unmanaged
        {
            var bytes = input.GetSlice(Unsafe.SizeOf<T>() * size).GetBytes();

            return MemoryMarshal.Cast<byte, T>(bytes).ToArray();
        }

        private static byte[] ToBytes<T>(T[] data) where T : unmanaged
        {
            return MemoryMarshal.Cast<T, byte>(data).ToArray();
        }

        public void Serialize(IoBuffer output, ISerializationContext context)
        {
            int pixelCount = Width * Height;

            if (Elevation.Length != pixelCount || ForestDensity.Length != pixelCount || ForestType.Length != pixelCount || RoadMap.Length != pixelCount || TerrainType.Length != pixelCount)
            {
                throw new Exception($"Invalid pixel count for city map - expected {Width}x{Height}");
            }

            output.Put(Elevation);
            output.Put(ForestDensity);
            output.Put(ToBytes(ForestType));
            output.Put(RoadMap);
            output.Put(ToBytes(TerrainType));
        }
    }
}
