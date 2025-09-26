using System;
using System.IO;
using System.Runtime.InteropServices;

namespace FSO.SimAntics.NetPlay.Model
{
    public interface VMSerializable
    {
        void SerializeInto(BinaryWriter writer);
        void Deserialize(BinaryReader reader);
    }

    public static class VMSerializableUtils
    {
        public static T[] ReadArray<T>(BinaryReader reader, int size) where T : unmanaged
        {
            var result = new T[size];
            var bytes = MemoryMarshal.Cast<T, byte>(result);

            reader.Read(bytes);

            return result;
        }

        public static void WriteArray<T>(BinaryWriter writer, T[] data) where T : unmanaged
        {
            var bytes = MemoryMarshal.Cast<T, byte>(data);

            writer.Write(bytes);
        }

        public static T[] ToTArray<T>(byte[] input)
        {
            var result = new T[input.Length / Marshal.SizeOf<T>()];
            Buffer.BlockCopy(input, 0, result, 0, input.Length);
            return result;
        }
    }
}
