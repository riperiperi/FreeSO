using System;

using System.Net;



namespace DNA
{
    public class Endian
    {
        static Endian()
        {
            _LittleEndian = BitConverter.IsLittleEndian;
        }

        public static short SwapInt16(short v)
        {
            return (short)(((v & 0xff) << 8) | ((v >> 8) & 0xff));
        }

        public static ushort SwapUInt16(ushort v)
        {
            return (ushort)(((v & 0xff) << 8) | ((v >> 8) & 0xff));
        }

        public static int SwapInt32(int v)
        {
            return (int)(((SwapInt16((short)v) & 0xffff) << 0x10) |
                          (SwapInt16((short)(v >> 0x10)) & 0xffff));
        }

        public static uint SwapUInt32(uint v)
        {
            return (uint)(((SwapUInt16((ushort)v) & 0xffff) << 0x10) |
                           (SwapUInt16((ushort)(v >> 0x10)) & 0xffff));
        }

        public static long SwapInt64(long v)
        {
            UInt64 uvalue = ((0x00000000000000FF) & ((ulong)v >> 56)

            | (0x000000000000FF00) & ((ulong)v >> 40)

            | (0x0000000000FF0000) & ((ulong)v >> 24)

            | (0x00000000FF000000) & ((ulong)v >> 8)

            | (0x000000FF00000000) & ((ulong)v << 8)

            | (0x0000FF0000000000) & ((ulong)v << 24)

            | (0x00FF000000000000) & ((ulong)v << 40)

            | (0xFF00000000000000) & ((ulong)v << 56));

            return (Int64)uvalue;
        }

        public static ulong SwapUInt64(ulong v)
        {
            UInt64 uvalue = ( (0x00000000000000FF) & (v >> 56)

            | (0x000000000000FF00) & (v >> 40)

            | (0x0000000000FF0000) & (v >> 24)

            | (0x00000000FF000000) & (v >> 8)

            | (0x000000FF00000000) & (v << 8)

            | (0x0000FF0000000000) & (v << 24)

            | (0x00FF000000000000) & (v << 40)

            | (0xFF00000000000000) & (v << 56));

            return uvalue;
        }

        public static bool IsBigEndian
        {
            get
            {
                return !_LittleEndian;
            }
        }

        public static bool IsLittleEndian
        {
            get
            {
                return _LittleEndian;
            }
        }

        private static readonly bool _LittleEndian;
    }
}