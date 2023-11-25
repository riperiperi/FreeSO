using System;
using System.Text;
using System.IO;

namespace FSO.Files.Utils
{

    /// <summary>
    /// IOBuffer is a very basic wrapper over System.BinaryReader that inherits from IDisposable.
    /// </summary>
    public class IoWriter : IDisposable, BCFWriteProxy
    {
        private Stream Stream;
        private BinaryWriter Writer;
        private bool FloatSwap = false;
        public ByteOrder ByteOrder = ByteOrder.BIG_ENDIAN;

        /// <summary>
        /// Creates a new IOBuffer instance from a stream.
        /// </summary>
        /// <param name="stream"></param>
        public IoWriter(Stream stream)
        {
            this.Stream = stream;
            this.Writer = new BinaryWriter(stream);
        }

        /// <summary>
        /// More to read in this stream?
        /// </summary>
        public bool HasMore
        {
            get
            {
                return Stream.Position < Stream.Length - 1;
            }
        }

        /// <summary>
        /// Skips a number of bytes in the current stream, starting from the current position.
        /// </summary>
        /// <param name="numBytes">Number of bytes to skip.</param>
        public void Skip(long numBytes)
        {
            Writer.BaseStream.Seek(numBytes, SeekOrigin.Current);
        }

        /// <summary>
        /// Seeks in the current stream.
        /// </summary>
        /// <param name="origin">Where to start from.</param>
        /// <param name="offset">The offset to seek to.</param>
        public void Seek(SeekOrigin origin, long offset)
        {
            Writer.BaseStream.Seek(offset, origin);
        }

        /// <summary>
        /// Writes a variable length unsigned integer to the current stream
        /// </summary>
        /// <param name="value">Value to write.</param>
        public void WriteVarLen(uint value)
        {
            bool first = true;
            while (value > 0 || first)
            {
                WriteByte((byte)(((value > 127)?(uint)128:0) | (value & 127)));
                value >>= 7;
                first = false;
            }
        }

        /// <summary>
        /// Writes an unsigned 16bit integer to the current stream. 
        /// </summary>
        /// <returns>A ushort.</returns>
        public void WriteUInt16(ushort value)
        {
            if (ByteOrder == ByteOrder.BIG_ENDIAN)
            {
                value = Endian.SwapUInt16(value);
            }
            Writer.Write(value);
        }

        /// <summary>
        /// Writes a 16bit integer to the current stream. 
        /// </summary>
        /// <returns>A short.</returns>
        public void WriteInt16(short value)
        {
            if (ByteOrder == ByteOrder.BIG_ENDIAN)
            {
                value = Endian.SwapInt16(value);
            }
            Writer.Write(value);
        }

        /// <summary>
        /// Writes a 32bit integer to the current stream. 
        /// </summary>
        /// <returns>An int.</returns>
        public void WriteInt32(int value)
        {
            if (ByteOrder == ByteOrder.BIG_ENDIAN)
            {
                value = Endian.SwapInt32(value);
            }
            Writer.Write(value);
        }

        /// <summary>
        /// Writes a 32bit integer to the current stream. 
        /// </summary>
        /// <returns>An int.</returns>
        public void WriteInt64(long value)
        {
            if (ByteOrder == ByteOrder.BIG_ENDIAN)
            {
                value = Endian.SwapInt64(value);
            }
            Writer.Write(value);
        }


        /// <summary>
        /// Writes an unsigned 32bit integer from to current stream. 
        /// </summary>
        /// <returns>A uint.</returns>
        public void WriteUInt32(uint value)
        {
            if (ByteOrder == ByteOrder.BIG_ENDIAN)
            {
                value = Endian.SwapUInt32(value);
            }
            Writer.Write(value);
        }

        /// <summary>
        /// Writes a number of ASCII characters to the current stream.
        /// </summary>
        /// <param name="value">The string to write.</param>
        /// <param name="num">The number of bytes to write into.</param>
        public void WriteCString(string value, int num)
        {
            value = value.PadRight(num, '\0');
            Writer.Write(ASCIIEncoding.ASCII.GetBytes(value));
        }

        public void WriteCString(string value)
        {
            WriteCString(value, value.Length + 1);
        }


        /// <summary>
        /// Writes a byte to the current stream.
        /// </summary>
        public void WriteByte(byte value)
        {
            Writer.Write(value);
        }

        /// <summary>
        /// Writes a number of bytes to the current stream.
        /// </summary>
        /// <param name="bytes">Bytes to write out.</param>
        public void WriteBytes(byte[] bytes)
        {
            Writer.Write(bytes);
        }

        /// <summary>
        /// Writes a pascal string to the current stream, which is prefixed by a 16bit short.
        /// </summary>
        public void WriteLongPascalString(string value)
        {
            WriteInt16((short)value.Length);
            WriteBytes(Encoding.ASCII.GetBytes(value));
        }

        /// <summary>
        /// Writes a C string to the current stream.
        /// </summary>
        public void WriteNullTerminatedString(string value)
        {
            if (value != null) WriteBytes(Encoding.ASCII.GetBytes(value));
            WriteByte(0);
        }

        /// <summary>
        /// Writes a pascal string to the current stream.
        /// </summary>
        public void WriteVariableLengthPascalString(string value)
        {
            Writer.Write((value == null)?"":value);
        }

        /// <summary>
        /// Writes a pascal string to the current stream, prefixed by a byte.
        /// </summary>
        public void WritePascalString(string value)
        {
            WriteByte((byte)value.Length);
            WriteBytes(Encoding.ASCII.GetBytes(value));
        }

        /// <summary>
        /// Writes a float to the current stream.
        /// </summary>
        public void WriteFloat(float value)
        {
            var bytes = BitConverter.GetBytes(value);

            if (ByteOrder == ByteOrder.BIG_ENDIAN && FloatSwap)
            {
                Array.Reverse(bytes);
            }

            Writer.Write(bytes);
        }

        #region IDisposable Members

        public void Dispose()
        {
        }

        #endregion

        /// <summary>
        /// Creates a new IoWriter instance from a stream.
        /// </summary>
        /// <param name="stream">A stream.</param>
        /// <returns>A new IoWriter instance.</returns>
        public static IoWriter FromStream(Stream stream)
        {
            return new IoWriter(stream);
        }

        /// <summary>
        /// Creates a new IoWriter instance from a stream, using a specified byte order.
        /// </summary>
        /// <param name="stream">A stream.</param>
        /// <param name="order">Byte order to use.</param>
        /// <returns>A new IoWriter instance.</returns>
        public static IoWriter FromStream(Stream stream, ByteOrder order)
        {
            var item = FromStream(stream);
            item.ByteOrder = order;
            return item;
        }

        /// <summary>
        /// Creates a new IOBuffer instance from a byte array.
        /// </summary>
        /// <param name="bytes">The byte array to use.</param>
        /// <returns>A new IOBuffer instance.</returns>
        public static IoWriter FromBytes(byte[] bytes)
        {
            return FromStream(new MemoryStream(bytes));
        }

        /// <summary>
        /// Creates a new IOBuffer instance from a byte array, using a specified byte order.
        /// </summary>
        /// <param name="bytes">The byte array to use.</param>
        /// <param name="order">Byte order to use.</param>
        /// <returns>A new IOBuffer instance.</returns>
        public static IoWriter FromBytes(byte[] bytes, ByteOrder order)
        {
            return FromStream(new MemoryStream(bytes), order);
        }
        
        /// <summary>
        /// Used by BCFWriteProxy's string mode, but does not do anything here.
        /// </summary>
        /// <param name="groupSize">The size of value groups</param>
        public void SetGrouping(int groupSize)
        {

        }
    }
}
