using FSO.Files.Utils;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace FSO.Files.Formats.IFF.Chunks
{
    /// <summary>
    /// Used to read values from field encoded stream.
    /// </summary>
    public class IffFieldEncode : IOProxy
    {
        private byte bitPos = 0;
        private byte curByte = 0;
        private bool odd = false;
        public byte[] widths = { 5, 8, 13, 16 };
        public byte[] widths32 = { 6, 11, 21, 32 };
        public byte[] widthsUnknown = { 2, 13, 21, 32 }; // These appear in the binary, but I'm not sure they're used.
        public byte[] widthsByte = { 2, 4, 6, 8 };
        public bool StreamEnd;

        [StructLayout(LayoutKind.Explicit)]
        private struct IntFloatAlias
        {
            [FieldOffset(0)]
            public int Int;
            [FieldOffset(0)]
            public float Float;

            public IntFloatAlias(int data)
            {
                Float = 0;
                Int = data;
            }

            public IntFloatAlias(float data)
            {
                Int = 0;
                Float = data;
            }
        }

        public void setBytePos(int n)
        {
            io.Seek(SeekOrigin.Begin, n);
            curByte = io.ReadByte();
            bitPos = 0;
        }

        public void Interrupt()
        {
            long targetPos = io.Position;

            if (bitPos == 0)
            {
                targetPos--;
            }

            io.Seek(SeekOrigin.Begin, targetPos);
        }

        public byte ReadByte()
        {
            return (byte)ReadField(widthsByte);
        }

        public override ushort ReadUInt16()
        {
            return (ushort)ReadField(widths);
        }

        public override short ReadInt16()
        {
            return (short)ReadField(widths);
        }

        public override int ReadInt32()
        {
            return (int)ReadField(widths32);
        }

        public override uint ReadUInt32()
        {
            return (uint)ReadField(widths32);
        }

        public override float ReadFloat()
        {
            uint data = (uint)ReadField(widths32);

            return new IntFloatAlias((int)data).Float;
        }

        private long ReadField(byte[] widths)
        {
            if (ReadBit() == 0) return 0;

            uint code = ReadBits(2);
            byte width = widths[code];
            long value = ReadBits(width);
            value |= -(value & (1 << (width - 1)));

            if (value == 0)
            {
                // not valid
            }

            return value;
        }

        public string BitDebug(int count)
        {
            string result = "";

            for (int i = 0; i < count; i++)
            {
                var bit = ReadBit();

                result += bit == 1 ? "1" : "0";

                if (bitPos == 0)
                {
                    result += "|";
                }
            }

            return result;
        }

        public string BitDebugTil(long skipPosition)
        {
            long currentPos = bitPos == 0 ? io.Position : io.Position - 1;

            int diff = (int)(skipPosition - currentPos) * 8 - bitPos;

            if (diff < 0)
            {
                return "oob";
            }

            return BitDebug(diff);
        }

        public Tuple<long, int> DebugReadField(bool big)
        {
            if (ReadBit() == 0) return new Tuple<long, int>(0, 0);

            uint code = ReadBits(2);
            byte width = (big) ? widths32[code] : widths[code];
            long value = ReadBits(width);
            value |= -(value & (1 << (width - 1)));

            return new Tuple<long, int>(value, width);
        }
        
        public Tuple<byte, byte, bool, long> MarkStream()
        {
            return new Tuple<byte, byte, bool, long>(bitPos, curByte, odd, io.Position);
        }

        public void RevertToMark(Tuple<byte, byte, bool, long> mark)
        {
            StreamEnd = false;
            bitPos = mark.Item1;
            curByte = mark.Item2;
            odd = mark.Item3;
            io.Seek(SeekOrigin.Begin, mark.Item4);
        }

        public uint ReadBits(int n)
        {
            uint total = 0;
            for (int i = 0; i < n; i++)
            {
                total += (uint)(ReadBit() << ((n - i) - 1));
            }
            return total;
        }

        private byte ReadBit()
        {
            byte result = (byte)((curByte & (1 << (7 - bitPos))) >> (7 - bitPos));
            if (++bitPos > 7)
            {
                bitPos = 0;
                if (io.HasMore)
                {
                    curByte = io.ReadByte();
                    odd = !odd;
                }
                else
                {
                    curByte = 0; //no more data, read 0
                    odd = !odd;
                    StreamEnd = true;
                }
            }
            return result;
        }

        public string ReadString(bool nextField)
        {
            if (bitPos == 0)
            {
                io.Seek(SeekOrigin.Current, -1);
                odd = !odd;
            }
            var str = io.ReadNullTerminatedString();
            if ((str.Length % 2 == 0) == !odd) io.ReadByte(); //2 byte pad

            bitPos = 8;
            if (nextField && io.HasMore)
            {
                curByte = io.ReadByte();
                odd = true;
                bitPos = 0;
            } else
            {
                odd = false;
            }

            return str;
        }

        public IffFieldEncode(IoBuffer io, bool oddOffset = false) : base(io)
        {
            curByte = io.ReadByte();
            odd = !oddOffset;
            bitPos = 0;
        }
    }
}
