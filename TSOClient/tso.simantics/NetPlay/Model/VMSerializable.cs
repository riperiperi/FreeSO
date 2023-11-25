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
        public static byte[] ToByteArray<T>(T[] input)
        {
            var result = new byte[input.Length * Marshal.SizeOf(typeof(T))];
            Buffer.BlockCopy(input, 0, result, 0, result.Length);
            return result;
        }

        public static T[] ToTArray<T>(byte[] input)
        {
            var result = new T[input.Length / Marshal.SizeOf(typeof(T))];
            Buffer.BlockCopy(input, 0, result, 0, input.Length);
            return result;
        }
    }
}
