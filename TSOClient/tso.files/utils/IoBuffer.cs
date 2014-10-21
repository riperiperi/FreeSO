/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SimsLib;

namespace TSO.Files.utils
{
    /// <summary>
    /// The order to read bytes in.
    /// </summary>
    public enum ByteOrder
    {
        BIG_ENDIAN,
        LITTLE_ENDIAN
    }

    /// <summary>
    /// IOBuffer is a very basic wrapper over System.BinaryReader that inherits from IDisposable.
    /// </summary>
    public class IoBuffer : IDisposable
    {
        private Stream Stream;
        private BinaryReader Reader;
        public ByteOrder ByteOrder = ByteOrder.BIG_ENDIAN;

        /// <summary>
        /// Creates a new IOBuffer instance from a stream.
        /// </summary>
        /// <param name="stream"></param>
        public IoBuffer(Stream stream)
        {
            this.Stream = stream;
            this.Reader = new BinaryReader(stream);
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
            Reader.BaseStream.Seek(numBytes, SeekOrigin.Current);
        }

        /// <summary>
        /// Seeks in the current stream.
        /// </summary>
        /// <param name="origin">Where to start from.</param>
        /// <param name="offset">The offset to seek to.</param>
        public void Seek(SeekOrigin origin, long offset)
        {
            Reader.BaseStream.Seek(offset, origin);
        }

        /// <summary>
        /// Reads an unsigned 16bit integer from the current stream. 
        /// </summary>
        /// <returns>A ushort.</returns>
        public ushort ReadUInt16()
        {
            var value = Reader.ReadUInt16();
            if (ByteOrder == ByteOrder.BIG_ENDIAN)
            {
                value = Endian.SwapUInt16(value);
            }

            return value;
        }

        /// <summary>
        /// Reads a 16bit integer from the current stream. 
        /// </summary>
        /// <returns>A short.</returns>
        public short ReadInt16()
        {
            var value = Reader.ReadInt16();
            if (ByteOrder == ByteOrder.BIG_ENDIAN)
            {
                value = Endian.SwapInt16(value);
            }

            return value;
        }

        /// <summary>
        /// Reads a 32bit integer from the current stream. 
        /// </summary>
        /// <returns>An int.</returns>
        public int ReadInt32()
        {
            var value = Reader.ReadInt32();
            if (ByteOrder == ByteOrder.BIG_ENDIAN)
            {
                value = Endian.SwapInt32(value);
            }
            return value;
        }

        /// <summary>
        /// Reads an unsigned 32bit integer from the current stream. 
        /// </summary>
        /// <returns>A uint.</returns>
        public uint ReadUInt32()
        {
            var value = Reader.ReadUInt32();
            if (ByteOrder == ByteOrder.BIG_ENDIAN)
            {
                value = Endian.SwapUInt32(value);
            }
            return value;
        }

        /// <summary>
        /// Reads a number of ASCII characters from the current stream.
        /// </summary>
        /// <param name="num">The number of characters to read.</param>
        /// <returns>A string, INCLUDING the trailing 0.</returns>
        public string ReadCString(int num)
        {
            return ReadCString(num, false);
        }

        /// <summary>
        /// Reads a number of ASCII characters from the current stream.
        /// </summary>
        /// <param name="num">The number of characters to read.</param>
        /// <param name="trimNull">Trim the trailing 0?</param>
        /// <returns>A string, with or without the trailing 0.</returns>
        public string ReadCString(int num, bool trimNull)
        {
            var result = ASCIIEncoding.ASCII.GetString(Reader.ReadBytes(num));
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

        /// <summary>
        /// Reads a byte from the current stream.
        /// </summary>
        /// <returns>A byte.</returns>
        public byte ReadByte()
        {
            return Reader.ReadByte();
        }

        /// <summary>
        /// Reads a number of bytes from the current stream.
        /// </summary>
        /// <param name="num">Number of bytes to read.</param>
        /// <returns>An byte array.</returns>
        public byte[] ReadBytes(uint num)
        {
            return Reader.ReadBytes((int)num);
        }

        /// <summary>
        /// Reads a number of bytes from the current stream.
        /// </summary>
        /// <param name="num">Number of bytes to read.</param>
        /// <returns>An byte array.</returns>
        public byte[] ReadBytes(int num)
        {
            return Reader.ReadBytes(num);
        }

        /// <summary>
        /// Reads a pascal string from the current stream, which is prefixed by a 16bit short.
        /// </summary>
        /// <returns>A string.</returns>
        public string ReadLongPascalString()
        {
            var length = ReadInt16();
            return Encoding.ASCII.GetString(Reader.ReadBytes(length));
        }

        /// <summary>
        /// Reads a C string from the current stream.
        /// </summary>
        /// <returns>A string.</returns>
        public string ReadNullTerminatedString()
        {
            var sb = new StringBuilder();
            while (true){
                char ch = (char)Reader.ReadByte();
                if (ch == '\0'){
                    break;
                }
                sb.Append(ch);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Reads a pascal string from the current stream.
        /// </summary>
        /// <returns>A string.</returns>
        public string ReadVariableLengthPascalString()
        {
            return Reader.ReadString();
        }

        /// <summary>
        /// Reads a pascal string from the current stream, prefixed by a byte.
        /// </summary>
        /// <returns>A string.</returns>
        public string ReadPascalString()
        {
            var length = ReadByte();
            return Encoding.ASCII.GetString(Reader.ReadBytes(length));
        }

        /// <summary>
        /// Reads a float from the current stream.
        /// </summary>
        /// <returns>A float.</returns>
        [System.Security.SecuritySafeCritical]  // auto-generated
        public virtual unsafe float ReadFloat()
        {
            var m_buffer = Reader.ReadBytes(4);
            uint tmpBuffer = (uint)(m_buffer[0] | m_buffer[1] << 8 | m_buffer[2] << 16 | m_buffer[3] << 24);

            var result = *((float*)&tmpBuffer);
            return result;
        }

        /// <summary>
        /// Sets a mark at the current position in the stream.
        /// </summary>
        private long _Mark;
        public void Mark()
        {
            _Mark = Reader.BaseStream.Position;
        }

        /// <summary>
        /// Seeks in the current stream from the current mark plus the number of bytes.
        /// </summary>
        /// <param name="numBytes">The number of bytes to add to the offset (mark).</param>
        public void SeekFromMark(long numBytes)
        {
            Reader.BaseStream.Seek(_Mark + numBytes, SeekOrigin.Begin);
        }

        #region IDisposable Members

        public void Dispose()
        {
        }

        #endregion

        /// <summary>
        /// Creates a new IOBuffer instance from a stream.
        /// </summary>
        /// <param name="stream">A stream.</param>
        /// <returns>A new IOBuffer instance.</returns>
        public static IoBuffer FromStream(Stream stream)
        {
            return new IoBuffer(stream);
        }

        /// <summary>
        /// Creates a new IOBuffer instance from a stream, using a specified byte order.
        /// </summary>
        /// <param name="stream">A stream.</param>
        /// <param name="order">Byte order to use.</param>
        /// <returns>A new IOBuffer instance.</returns>
        public static IoBuffer FromStream(Stream stream, ByteOrder order)
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
        public static IoBuffer FromBytes(byte[] bytes)
        {
            return FromStream(new MemoryStream(bytes));
        }

        /// <summary>
        /// Creates a new IOBuffer instance from a byte array, using a specified byte order.
        /// </summary>
        /// <param name="bytes">The byte array to use.</param>
        /// <param name="order">Byte order to use.</param>
        /// <returns>A new IOBuffer instance.</returns>
        public static IoBuffer FromBytes(byte[] bytes, ByteOrder order)
        {
            return FromStream(new MemoryStream(bytes), order);
        }
    }
}
