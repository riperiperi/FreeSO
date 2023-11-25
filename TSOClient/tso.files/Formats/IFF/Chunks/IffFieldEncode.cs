using FSO.Files.Utils;
using System;
using System.IO;

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
        public byte[] widths2 = { 6, 11, 21, 32 };
        public bool StreamEnd;

        public void setBytePos(int n)
        {
            io.Seek(SeekOrigin.Begin, n);
            curByte = io.ReadByte();
            bitPos = 0;
        }

        public override ushort ReadUInt16()
        {
            return (ushort)ReadField(false);
        }

        public override short ReadInt16()
        {
            return (short)ReadField(false);
        }

        public override int ReadInt32()
        {
            return (int)ReadField(true);
        }

        public override uint ReadUInt32()
        {
            return (uint)ReadField(true);
        }

        public override float ReadFloat()
        {
            return (float)ReadField(true);
            //this is incredibly wrong
        }

        private long ReadField(bool big)
        {
            if (ReadBit() == 0) return 0;

            uint code = ReadBits(2);
            byte width = (big) ? widths2[code] : widths[code];
            long value = ReadBits(width);
            value |= -(value & (1 << (width - 1)));

            return value;
        }

        public Tuple<long, int> DebugReadField(bool big)
        {
            if (ReadBit() == 0) return new Tuple<long, int>(0, 0);

            uint code = ReadBits(2);
            byte width = (big) ? widths2[code] : widths[code];
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
                try
                {
                    curByte = io.ReadByte();
                    odd = !odd;
                }
                catch (Exception)
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

        public IffFieldEncode(IoBuffer io) : base(io)
        {
            curByte = io.ReadByte();
            odd = !odd;
            bitPos = 0;
        }
    }
}
