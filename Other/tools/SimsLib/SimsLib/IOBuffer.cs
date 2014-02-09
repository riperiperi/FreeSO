using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SimsLib
{
    public enum ByteOrder
    {
        BIG_ENDIAN,
        LITTLE_ENDIAN
    }

    /// <summary>
    /// IOBuffer is used to read a stream of bytes from a file.
    /// </summary>
    public class IoBuffer : IDisposable
    {
        private Stream Stream;
        private BinaryReader Reader;
        public ByteOrder ByteOrder = ByteOrder.BIG_ENDIAN;

        public IoBuffer(Stream stream)
        {
            this.Stream = stream;
            this.Reader = new BinaryReader(stream);
        }

        public bool HasMore
        {
            get
            {
                return Stream.Position < Stream.Length - 1;
            }
        }

        public void Skip(long numBytes)
        {
            Reader.BaseStream.Seek(numBytes, SeekOrigin.Current);
        }

        public void Seek(SeekOrigin origin, long offset)
        {
            Reader.BaseStream.Seek(offset, origin);
        }

        public ushort ReadUInt16()
        {
            var value = Reader.ReadUInt16();
            if (ByteOrder == ByteOrder.BIG_ENDIAN)
            {
                value = Endian.SwapUInt16(value);
            }
            return value;
        }

        public short ReadInt16()
        {
            var value = Reader.ReadInt16();
            if (ByteOrder == ByteOrder.BIG_ENDIAN)
            {
                value = Endian.SwapInt16(value);
            }
            return value;
        }

        public int ReadInt32()
        {
            var value = Reader.ReadInt32();
            if (ByteOrder == ByteOrder.BIG_ENDIAN)
            {
                value = Endian.SwapInt32(value);
            }
            return value;
        }

        public uint ReadUInt32()
        {
            var value = Reader.ReadUInt32();
            if (ByteOrder == ByteOrder.BIG_ENDIAN)
            {
                value = Endian.SwapUInt32(value);
            }
            return value;
        }

        public string ReadChars(int num)
        {
            return ReadChars(num, false);
        }

        public string ReadChars(int num, bool trimNull)
        {
            var result = new string(Reader.ReadChars(num));
            if (trimNull)
            {
                /** Trim on \0 **/
                var io = result.IndexOf('\0');
                if (io != -1)
                {
                    result = result.Substring(0, io);
                }
            }
            return result;
        }

        public byte ReadByte()
        {
            return Reader.ReadByte();
        }

        public byte[] ReadBytes(uint num)
        {
            return Reader.ReadBytes((int)num);
        }

        public byte[] ReadBytes(int num)
        {
            return Reader.ReadBytes(num);
        }

        public string ReadLongPascalString()
        {
            var length = ReadInt16();
            return Encoding.ASCII.GetString(Reader.ReadBytes(length));
        }

        public string ReadNullTerminatedString()
        {
            var sb = new StringBuilder();
            while (true)
            {
                char ch = (char)Reader.ReadByte();
                if (ch == '\0')
                {
                    break;
                }
                sb.Append(ch);
            }
            return sb.ToString();
        }

        public string ReadVariableLengthPascalString()
        {
            return Reader.ReadString();
        }

        public string ReadPascalString()
        {
            var length = ReadByte();
            return Encoding.ASCII.GetString(Reader.ReadBytes(length));
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public virtual unsafe float ReadFloat()
        {
            var m_buffer = Reader.ReadBytes(4);
            uint tmpBuffer = (uint)(m_buffer[0] | m_buffer[1] << 8 | m_buffer[2] << 16 | m_buffer[3] << 24);

            var result = *((float*)&tmpBuffer);
            return result;
        }

        private long _Mark;
        public void Mark()
        {
            _Mark = Reader.BaseStream.Position;
        }

        public void SeekFromMark(long numBytes)
        {
            Reader.BaseStream.Seek(_Mark + numBytes, SeekOrigin.Begin);
        }

        #region IDisposable Members

        public void Dispose()
        {
        }

        #endregion

        public static IoBuffer FromStream(Stream stream)
        {
            return new IoBuffer(stream);
        }

        public static IoBuffer FromStream(Stream stream, ByteOrder order)
        {
            var item = FromStream(stream);
            item.ByteOrder = order;
            return item;
        }

        public static IoBuffer FromBytes(byte[] bytes)
        {
            return FromStream(new MemoryStream(bytes));
        }

        public static IoBuffer FromBytes(byte[] bytes, ByteOrder order)
        {
            return FromStream(new MemoryStream(bytes), order);
        }
    }
}